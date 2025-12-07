using UnityEngine;

public class ConstructionSite : MonoBehaviour
{
    private BuildSystem.BuildingConfig config;
    private float buildTimer;
    private bool isBuilding = false;
    private bool isInitialized = false;

    /// <summary>
    /// BuildSystem tarafından çağrılır.
    /// Hangi yapı inşa edilecek, ne kadar sürecek vs. burada atanır.
    /// </summary>
    public void Setup(BuildSystem.BuildingConfig cfg)
    {
        config = cfg;

        if (config != null)
            buildTimer = Mathf.Max(0f, config.buildTime);
        else
            buildTimer = 0f;

        isInitialized = true;
        isBuilding = false;   // Oyuncu gelene kadar bekle
    }

    /// <summary>
    /// PlayerBuilder oyuncu inşa noktasına vardığında çağırır.
    /// Eski kodda site.BeginConstruction() vardı, onu destekliyoruz.
    /// </summary>
    public void BeginConstruction()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("ConstructionSite: BeginConstruction çağrıldı ama Setup henüz yapılmamış!");
            return;
        }

        if (isBuilding) return;  // Zaten başlamışsa tekrar başlatma

        isBuilding = true;
    }

    /// <summary>
    /// Olası eski tasarımlar için: BeginConstruction(cfg) kullandıysan bozulmasın diye overload.
    /// </summary>
    public void BeginConstruction(BuildSystem.BuildingConfig cfg)
    {
        Setup(cfg);
        BeginConstruction();
    }

    private void Update()
    {
        if (!isInitialized || !isBuilding || config == null)
            return;

        if (buildTimer > 0f)
        {
            buildTimer -= Time.deltaTime;
            if (buildTimer <= 0f)
            {
                CompleteConstruction();
            }
        }
    }

    private void CompleteConstruction()
    {
        // İnşa bittiğinde final prefab'ı spawn et
        if (config != null && config.finalPrefab != null)
        {
            // 🔥 ROTASYON BURADA KORUNUYOR
            Instantiate(config.finalPrefab, transform.position, transform.rotation);
        }

        Destroy(gameObject);
    }
}
