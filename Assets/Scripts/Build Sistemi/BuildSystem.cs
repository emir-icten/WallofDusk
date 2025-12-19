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

        [Header("Duvar Ayarları")]
        public bool isWall = false;           // Bu yapı duvar mı?
        [Tooltip("Duvar diğer duvarlara bu mesafeye kadar yaklaşıyorsa snap yapsın")]
        public float wallSnapDistance = 1.5f;
        [Tooltip("Duvar segmentlerinin merkezden merkeze uzunluğu")]
        public float wallSegmentLength = 2f;
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
    [Tooltip("Kuleler / binalar birbirine ne kadar yakın olamaz? (Duvarlar için ayrıca kontrol var)")]
    public float minDistanceBetweenBuildings = 2f;

    [Tooltip("Binaların bulunduğu layer mask (ArcherTower, MageTower, Wall, vs)")]
    public LayerMask buildingMask;

    [Header("Duvar Global Ayarları")]
    [Tooltip("Duvar prefablarının tag'i")]
    public string wallTag = "Wall";

    [Header("Referanslar")]
    public PlayerBuilder playerBuilder;

    /// <summary>
    /// Aktif player değiştiğinde çağırılır.
    /// </summary>
    public void SetPlayerBuilder(PlayerBuilder newBuilder)
    {
        playerBuilder = newBuilder;
        if (playerBuilder != null)
        {
            Debug.Log("BuildSystem: PlayerBuilder güncellendi -> " + playerBuilder.name);
        }
        else
        {
            Debug.Log("BuildSystem: PlayerBuilder NULL oldu.");
        }
    }

    [Header("Ghost & Zemin Uyarı Renkleri")]
    public Color validColor = new Color(0.5f, 1f, 0.5f, 1f);   // geçerli yer
    public Color invalidColor = new Color(1f, 0.4f, 0.4f, 1f); // geçersiz yer

    [Header("Zemin Dairesi (opsiyonel)")]
    public GameObject groundIndicatorPrefab;  // Yuvarlak daire prefab'ı

    [Header("Rotation Ayarları")]
    [Tooltip("R tuşuna her bastığında kaç derece döndürsün?")]
    public float rotationStep = 90f;
    public KeyCode rotateKey = KeyCode.R;

    // ============================
    // ✅ Performans (NonAlloc)
    // ============================
    [Header("GC/Performans (NonAlloc)")]
    [SerializeField] private int placementOverlapBufferSize = 64;
    [SerializeField] private int wallSnapOverlapBufferSize = 32;

    private Collider[] _placementOverlapHits;
    private Collider[] _wallSnapOverlapHits;

    // ---- internal state ----
    private Camera mainCam;
    private int selectedIndex = -1;
    private BuildingConfig currentConfig;

    private GameObject currentGhost;
    private Renderer[] ghostRenderers;

    private GameObject groundIndicatorInstance;
    private Renderer[] groundIndicatorRenderers;

    private Vector3 currentPlacementPos;
    private bool isPlacing = false;
    private bool lastValidState = true;

    private float currentRotationY = 0f;
    private Quaternion ghostBaseRotation;

    public bool IsPlacing => isPlacing;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // ✅ NonAlloc buffer init (tek sefer)
        _placementOverlapHits = new Collider[Mathf.Max(8, placementOverlapBufferSize)];
        _wallSnapOverlapHits = new Collider[Mathf.Max(8, wallSnapOverlapBufferSize)];
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

        // Döndür: R tuşu
        if (Input.GetKeyDown(rotateKey))
        {
            currentRotationY += rotationStep;
            if (currentRotationY >= 360f || currentRotationY <= -360f)
                currentRotationY = Mathf.Repeat(currentRotationY, 360f);

            UpdateGhostRotation();
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
        currentConfig = buildings[selectedIndex];

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
        if (currentConfig.ghostPrefab != null)
        {
            currentGhost = Instantiate(currentConfig.ghostPrefab);
        }
        else if (currentConfig.finalPrefab != null)
        {
            currentGhost = Instantiate(currentConfig.finalPrefab);
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

        // Rotation başlangıcı
        ghostBaseRotation = currentGhost.transform.rotation;
        currentRotationY = 0f;
        UpdateGhostRotation();

        // Zemin dairesi varsa instantiate et
        if (groundIndicatorPrefab != null)
        {
            groundIndicatorInstance = Instantiate(groundIndicatorPrefab);
            groundIndicatorRenderers = groundIndicatorInstance.GetComponentsInChildren<Renderer>();
        }

        SetPlacementColor(validColor);

        isPlacing = true;
        Debug.Log("BuildSystem: Placement moduna girildi -> " + currentConfig.displayName);
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
            hasHit = Physics.Raycast(ray, out hit, 200f, groundMask, QueryTriggerInteraction.Ignore);
        else
            hasHit = Physics.Raycast(ray, out hit, 200f, ~0, QueryTriggerInteraction.Ignore);

        if (!hasHit) return;

        Vector3 pos = hit.point + Vector3.up * verticalOffset;

        // Eğer seçili yapı duvar ise, yakın duvarlara göre snap etmeye çalış
        if (currentConfig != null && currentConfig.isWall)
        {
            pos = GetSnappedWallPosition(pos, currentConfig);
        }

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
            hasHit = Physics.Raycast(ray, out hit, 200f, groundMask, QueryTriggerInteraction.Ignore);
        else
            hasHit = Physics.Raycast(ray, out hit, 200f, ~0, QueryTriggerInteraction.Ignore);

        if (!hasHit)
        {
            Debug.Log("<BuildSystem> Raycast zemin bulamadı, inşa iptal.");
            return;
        }

        Vector3 placementPos = hit.point + Vector3.up * verticalOffset;

        // Duvar için snap tekrar
        if (currentConfig != null && currentConfig.isWall)
        {
            placementPos = GetSnappedWallPosition(placementPos, currentConfig);
        }

        currentPlacementPos = placementPos;

        // 1) Pozisyon geçerli mi? (base + diğer binalardan uzaklık kontrolü)
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

        // Rotasyonu ghost'tan al (özellikle duvarlar için önemli)
        if (currentGhost != null)
            siteObj.transform.rotation = currentGhost.transform.rotation;

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
        currentConfig = null;

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
    //  Duvar snap fonksiyonu (köşelerde tam otursun)
    // ---------------------------
    private Vector3 GetSnappedWallPosition(Vector3 pos, BuildingConfig cfg)
    {
        float radius = Mathf.Max(0.01f, cfg.wallSnapDistance);

        // ✅ 1) Yakındaki duvarları bul (NonAlloc)
        int hitCount;
        if (buildingMask.value != 0)
        {
            hitCount = Physics.OverlapSphereNonAlloc(
                pos, radius, _wallSnapOverlapHits, buildingMask, QueryTriggerInteraction.Ignore
            );
        }
        else
        {
            hitCount = Physics.OverlapSphereNonAlloc(
                pos, radius, _wallSnapOverlapHits, ~0, QueryTriggerInteraction.Ignore
            );
        }

        Transform nearestWall = null;
        float bestSqr = Mathf.Infinity;

        for (int i = 0; i < hitCount; i++)
        {
            var hit = _wallSnapOverlapHits[i];
            if (hit == null) continue;
            if (!hit.CompareTag(wallTag)) continue;

            float sqr = (hit.transform.position - pos).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                nearestWall = hit.transform;
            }
        }

        // Yakında hiç duvar yoksa, olduğu gibi bırak
        if (nearestWall == null)
            return pos;

        Vector3 wallPos = nearestWall.position;

        // Duvarın ileri yönü (uzun eksen)
        Vector3 nF = nearestWall.forward;
        nF.y = 0f;
        if (nF.sqrMagnitude < 0.001f) nF = Vector3.forward;
        nF.Normalize();

        // Ghost duvarın yönü
        if (currentGhost == null)
            return pos;

        Vector3 gF = currentGhost.transform.forward;
        gF.y = 0f;
        if (gF.sqrMagnitude < 0.001f) gF = Vector3.forward;
        gF.Normalize();

        // Mouse pozisyonuna göre hangi tarafa gittiğimizi bul
        Vector3 toPos = pos - wallPos;
        toPos.y = 0f;

        float halfL = cfg.wallSegmentLength * 0.5f;

        // İki duvar paralel mi, dik mi?
        float dot = Mathf.Abs(Vector3.Dot(nF, gF));

        Vector3 snappedPos;

        if (dot > 0.9f)
        {
            // -----------------------------
            // 1) PARALLEL: zincir gibi ard arda
            // -----------------------------
            float signAlong = Mathf.Sign(Vector3.Dot(toPos, nF));
            if (Mathf.Approximately(signAlong, 0f)) signAlong = 1f;

            snappedPos = wallPos + nF * signAlong * cfg.wallSegmentLength;
            snappedPos.y = pos.y;
        }
        else
        {
            // -----------------------------
            // 2) DİK: köşede tam birleşsin
            // -----------------------------
            float signN = Mathf.Sign(Vector3.Dot(toPos, nF));
            if (Mathf.Approximately(signN, 0f)) signN = 1f;

            float signG = Mathf.Sign(Vector3.Dot(toPos, gF));
            if (Mathf.Approximately(signG, 0f)) signG = 1f;

            // Eski duvarın ucundaki köşe
            Vector3 corner = wallPos + nF * signN * halfL;

            // Yeni duvarın merkezi = köşe + kendi yönünde yarım uzunluk
            snappedPos = corner + gF * signG * halfL;
            snappedPos.y = pos.y;
        }

        return snappedPos;
    }

    // ---------------------------
    //  Pozisyon geçerli mi?
    //  (Base + diğer binalardan mesafe kontrolü)
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

        // 2) Diğer kule / binalara çok yakın mı?
        // ⚠ Bu kuralı SADECE duvar olmayan yapılar için uygula.
        // Duvar inşa ederken kulelere yaklaşabilsin.
        if (minDistanceBetweenBuildings > 0.01f && (currentConfig == null || !currentConfig.isWall))
        {
            // ✅ NonAlloc
            int hitCount;
            if (buildingMask.value != 0)
            {
                hitCount = Physics.OverlapSphereNonAlloc(
                    pos, minDistanceBetweenBuildings, _placementOverlapHits, buildingMask, QueryTriggerInteraction.Ignore
                );
            }
            else
            {
                hitCount = Physics.OverlapSphereNonAlloc(
                    pos, minDistanceBetweenBuildings, _placementOverlapHits, ~0, QueryTriggerInteraction.Ignore
                );
            }

            for (int i = 0; i < hitCount; i++)
            {
                var hit = _placementOverlapHits[i];
                if (hit == null) continue;

                // ✅ Ghost’un kendi collider’ına takılma (nadiren olur ama olursa placement hep invalid görünür)
                if (currentGhost != null && hit.transform.IsChildOf(currentGhost.transform))
                    continue;

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

    private void UpdateGhostRotation()
    {
        if (currentGhost == null) return;

        currentGhost.transform.rotation =
            ghostBaseRotation * Quaternion.Euler(0f, currentRotationY, 0f);
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
