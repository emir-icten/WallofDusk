using System;
using UnityEngine;

public class BuildSystem : MonoBehaviour
{
    public static BuildSystem Instance { get; private set; }

    [Serializable]
    public class BuildingConfig
    {
        public string id;
        public string displayName;

        [Header("Prefablar")]
        public GameObject finalPrefab;     // İnşa bitince oluşacak kule / yapı
        public GameObject ghostPrefab;     // Sürüklerken gözüken hayalet

        [Header("Maliyet")]
        public int woodCost;
        public int stoneCost;

        [Header("İnşa Süresi")]
        public float buildTime = 3f;       // saniye
    }

    [Header("Yapılar")]
    public BuildingConfig[] buildings;

    [Header("Raycast Ayarları")]
    public LayerMask groundMask;          // Zemin layer'ı
    public float verticalOffset = 0.1f;

    [Header("Base Mesafe Sınırı")]
    public Transform baseTransform;
    public float minDistanceFromBase = 3f;

    [Header("Bina Mesafe Sınırı")]
    [Tooltip("Kuleler / binalar birbirine ne kadar yakın olamaz?")]
    public float minDistanceBetweenBuildings = 2f;

    [Tooltip("Binaların bulunduğu layer mask (ArcherTower, MageTower, inşa alanı vs)")]
    public LayerMask buildingMask;

    [Header("Referanslar")]
    public PlayerBuilder playerBuilder;

    [Header("Ghost & Zemin Uyarı Renkleri")]
    public Color validColor = new Color(0.5f, 1f, 0.5f, 1f);   // geçerli yer
    public Color invalidColor = new Color(1f, 0.4f, 0.4f, 1f); // geçersiz yer

    [Header("Zemin Dairesi (opsiyonel)")]
    public GameObject groundIndicatorPrefab;  // Yuvarlak daire prefab'ı

    // ---- internal state ----
    private Camera mainCam;
    private int selectedIndex = -1;
    private GameObject currentGhost;
    private Renderer[] ghostRenderers;

    private GameObject groundIndicatorInstance;
    private Renderer[] groundIndicatorRenderers;

    private Vector3 currentPlacementPos;
    private bool isPlacing = false;
    private bool lastValidState = true;

    public bool IsPlacing => isPlacing;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogWarning("BuildSystem: Main Camera bulunamadı!");
        }
    }

    private void Update()
    {
        if (!isPlacing || mainCam == null) return;

        // Ghost ve yer uyarı dairesini mouse'la beraber güncelle
        UpdateGhostPositionFromMouse();

        // İptal: sağ tık veya ESC
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPlacement();
        }
    }

    // =========================================================
    //  BuildSlotUI → drag başlayınca bunu çağırıyor
    // =========================================================
    public void StartPlacement(int index)
    {
        if (index < 0 || buildings == null || index >= buildings.Length)
        {
            Debug.LogWarning("BuildSystem: Geçersiz building index: " + index);
            return;
        }

        selectedIndex = index;
        BuildingConfig cfg = buildings[selectedIndex];

        // Eski ghost'u temizle
        if (currentGhost != null)
        {
            Destroy(currentGhost);
            currentGhost = null;
        }

        // Eski ground indicator'ı temizle
        if (groundIndicatorInstance != null)
        {
            Destroy(groundIndicatorInstance);
            groundIndicatorInstance = null;
        }

        // Yeni ghost oluştur
        if (cfg.ghostPrefab != null)
        {
            currentGhost = Instantiate(cfg.ghostPrefab);
        }
        else if (cfg.finalPrefab != null)
        {
            currentGhost = Instantiate(cfg.finalPrefab);
        }
        else
        {
            Debug.LogWarning("BuildSystem: Seçili yapı için prefab atanmadı.");
            return;
        }

        // Ghost’u tam anlamıyla “fiziksiz” yap (her ihtimale karşı)
        MakeGhostNonInteractable(currentGhost);

        ghostRenderers = currentGhost.GetComponentsInChildren<Renderer>();
        lastValidState = true;

        // Zemin dairesi varsa instantiate et
        if (groundIndicatorPrefab != null)
        {
            groundIndicatorInstance = Instantiate(groundIndicatorPrefab);
            groundIndicatorRenderers = groundIndicatorInstance.GetComponentsInChildren<Renderer>();
        }

        SetPlacementColor(validColor);

        isPlacing = true;
        Debug.Log("BuildSystem: Placement moduna girildi -> " + cfg.displayName);
    }

    // =========================================================
    //  Ghost pozisyonunu mouse’a göre güncelle
    // =========================================================
    private void UpdateGhostPositionFromMouse()
    {
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        bool hasHit;

        if (groundMask.value != 0)
            hasHit = Physics.Raycast(ray, out hit, 200f, groundMask);
        else
            hasHit = Physics.Raycast(ray, out hit, 200f);

        if (!hasHit) return;

        Vector3 pos = hit.point + Vector3.up * verticalOffset;
        currentPlacementPos = pos;

        if (currentGhost != null)
            currentGhost.transform.position = pos;

        // Zemin dairesi pozisyonu
        if (groundIndicatorInstance != null)
        {
            Vector3 p = hit.point;
            p.y += 0.02f;
            groundIndicatorInstance.transform.position = p;
        }

        bool isValid = IsValidPlacementPosition(pos);
        UpdatePlacementVisual(isValid);
    }

    // =========================================================
    //  BuildSlotUI → drag bitince (mouse bırakılınca) bunu çağırıyor
    // =========================================================
    public void ConfirmPlacement()
    {
        if (!isPlacing || selectedIndex < 0 || buildings == null)
            return;

        if (mainCam == null) return;

        BuildingConfig cfg = buildings[selectedIndex];

        // En güncel pozisyonu tekrar al
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        bool hasHit;

        if (groundMask.value != 0)
            hasHit = Physics.Raycast(ray, out hit, 200f, groundMask);
        else
            hasHit = Physics.Raycast(ray, out hit, 200f);

        if (!hasHit)
        {
            Debug.Log("<BuildSystem> Raycast zemin bulamadı, inşa iptal.");
            return;
        }

        Vector3 placementPos = hit.point + Vector3.up * verticalOffset;
        currentPlacementPos = placementPos;

        // 1) Pozisyon geçerli mi? (base + diğer kulelerden uzaklık kontrolü)
        if (!IsValidPlacementPosition(placementPos))
        {
            Debug.Log("<BuildSystem> Geçersiz yer! (base'e veya başka kuleye çok yakın)");
            return; // ghost sahnede kalır, oyuncu tekrar sürükleyebilir
        }

        // 2) Kaynak yetiyor mu?
        if (!HasEnoughResources(cfg))
        {
            Debug.Log("<BuildSystem> Yeterli kaynak yok. Gerekli -> Wood: " +
                      cfg.woodCost + " | Stone: " + cfg.stoneCost +
                      " | Mevcut -> Wood: " + ResourceManager.WoodCount +
                      " | Stone: " + ResourceManager.StoneCount);

            // Kaynak yoksa placement modundan çık
            CancelPlacement();
            return;
        }

        // 3) İnşa alanı oluştur
        GameObject siteObj = new GameObject("ConstructionSite_" + cfg.displayName);
        siteObj.transform.position = placementPos;

        ConstructionSite site = siteObj.AddComponent<ConstructionSite>();
        site.Setup(cfg);

        // 4) Kaynakları harca
        SpendResources(cfg);

        // 5) Player'ı oraya gönder
        if (playerBuilder != null)
        {
            playerBuilder.GoBuild(site);
        }

        // 6) Placement'i kapat
        CancelPlacement(false);
    }

    private void CancelPlacement(bool keepGhost = false)
    {
        isPlacing = false;
        selectedIndex = -1;

        if (!keepGhost && currentGhost != null)
        {
            Destroy(currentGhost);
        }

        if (groundIndicatorInstance != null)
        {
            Destroy(groundIndicatorInstance);
        }

        currentGhost = null;
        ghostRenderers = null;
        groundIndicatorInstance = null;
        groundIndicatorRenderers = null;
    }

    public void CancelCurrentPlacement()
    {
        if (isPlacing)
        {
            CancelPlacement();
        }
    }

    // ---------------------------
    //  Pozisyon geçerli mi?
    //  (Base + diğer kulelerden mesafe kontrolü)
    // ---------------------------
    private bool IsValidPlacementPosition(Vector3 pos)
    {
        // 1) Base'e çok yakın mı?
        if (baseTransform != null && minDistanceFromBase > 0.1f)
        {
            Vector3 basePos = baseTransform.position;
            basePos.y = pos.y;

            float distBase = Vector3.Distance(basePos, pos);
            if (distBase < minDistanceFromBase)
            {
                return false; // base koruma alanı
            }
        }

        // 2) Diğer kulelere / binalara çok yakın mı?
        if (minDistanceBetweenBuildings > 0.01f)
        {
            Collider[] hits;

            if (buildingMask.value != 0)
                hits = Physics.OverlapSphere(pos, minDistanceBetweenBuildings, buildingMask);
            else
                hits = Physics.OverlapSphere(pos, minDistanceBetweenBuildings);

            if (hits.Length > 0)
            {
                // etrafta başka bina var → buraya yeni bina koyma
                return false;
            }
        }

        return true;
    }

    // ---------------------------
    //  Ghost + zemin dairesi görseli
    // ---------------------------
    private void UpdatePlacementVisual(bool isValid)
    {
        if (lastValidState == isValid) return;

        SetPlacementColor(isValid ? validColor : invalidColor);
        lastValidState = isValid;
    }

    private void SetPlacementColor(Color c)
    {
        // Ghost
        if (ghostRenderers != null)
        {
            foreach (var r in ghostRenderers)
            {
                if (r == null) continue;

                if (r.material.HasProperty("_Color"))
                {
                    Color orig = r.material.color;
                    c.a = orig.a;
                    r.material.color = c;
                }
            }
        }

        // Zemin dairesi
        if (groundIndicatorRenderers != null)
        {
            foreach (var r in groundIndicatorRenderers)
            {
                if (r == null) continue;

                if (r.material.HasProperty("_Color"))
                {
                    Color orig = r.material.color;
                    c.a = orig.a;
                    r.material.color = c;
                }
            }
        }
    }

    // ---------------------------
    //  Kaynak kontrolü / harcama
    // ---------------------------
    private bool HasEnoughResources(BuildingConfig cfg)
    {
        int currentWood = ResourceManager.WoodCount;
        int currentStone = ResourceManager.StoneCount;

        return currentWood >= cfg.woodCost && currentStone >= cfg.stoneCost;
    }

    private void SpendResources(BuildingConfig cfg)
    {
        // Odun
        if (cfg.woodCost > 0)
        {
            ResourceManager.WoodCount = Mathf.Max(0, ResourceManager.WoodCount - cfg.woodCost);

            if (UIManager.instance != null)
                UIManager.instance.UpdateWoodUI(ResourceManager.WoodCount);
        }

        // Taş
        if (cfg.stoneCost > 0)
        {
            ResourceManager.StoneCount = Mathf.Max(0, ResourceManager.StoneCount - cfg.stoneCost);

            if (UIManager.instance != null)
                UIManager.instance.UpdateStoneUI(ResourceManager.StoneCount);
        }
    }

    // ---------------------------
    //  Ghost’u tamamen “fiziksiz” yapmak için
    // ---------------------------
    private void MakeGhostNonInteractable(GameObject ghost)
    {
        if (ghost == null) return;

        var colliders = ghost.GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
            col.enabled = false;

        var bodies = ghost.GetComponentsInChildren<Rigidbody>();
        foreach (var rb in bodies)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        int ignoreLayer = LayerMask.NameToLayer("Ignore Raycast");
        if (ignoreLayer >= 0)
            SetLayerRecursively(ghost.transform, ignoreLayer);
    }

    private void SetLayerRecursively(Transform t, int layer)
    {
        t.gameObject.layer = layer;
        for (int i = 0; i < t.childCount; i++)
        {
            SetLayerRecursively(t.GetChild(i), layer);
        }
    }
}
