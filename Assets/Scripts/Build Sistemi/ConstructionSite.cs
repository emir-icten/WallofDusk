using UnityEngine;

public class ConstructionSite : MonoBehaviour
{
    private BuildSystem.BuildingConfig config;
    private float buildTimer;
    private bool isConstructing = false;
    private PlayerBuilder builder;

    [Header("Görsel Ayar (isteğe bağlı)")]
    public GameObject constructionVisualPrefab;  // temel/kazı/iskelet model
    private GameObject spawnedVisual;

    public void Setup(BuildSystem.BuildingConfig cfg)
    {
        config = cfg;
        buildTimer = cfg.buildTime;

        // İnşa alanı için görsel istersen:
        if (constructionVisualPrefab != null)
        {
            spawnedVisual = Instantiate(constructionVisualPrefab, transform.position, Quaternion.identity);
        }
    }

    public void BeginConstruction(PlayerBuilder b)
    {
        if (isConstructing || config == null) return;

        builder = b;
        isConstructing = true;
        Debug.Log("ConstructionSite: İnşa başladı -> " + config.displayName);
    }

    private void Update()
    {
        if (!isConstructing || config == null) return;

        // Builder halen yakın mı kontrol (isteğe bağlı)
        if (builder != null)
        {
            float dist = Vector3.Distance(builder.transform.position, transform.position);
            if (dist > 5f)
            {
                // Uzaklaştıysa istersen inşayı durdurabilirsin
                // burada şimdilik devam ettiriyoruz
            }
        }

        buildTimer -= Time.deltaTime;
        if (buildTimer <= 0f)
        {
            FinishConstruction();
        }
    }

    private void FinishConstruction()
    {
        isConstructing = false;

        // İnşa görselini sil
        if (spawnedVisual != null)
            Destroy(spawnedVisual);

        // Asıl kuleyi oluştur
        if (config.finalPrefab != null)
        {
            Instantiate(config.finalPrefab, transform.position, Quaternion.identity);
        }

        Debug.Log("ConstructionSite: İnşa tamamlandı -> " + config.displayName);

        // Bu inşa alanını sil
        Destroy(gameObject);
    }
}
