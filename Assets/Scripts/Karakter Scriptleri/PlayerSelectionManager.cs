using UnityEngine;

public class PlayerSelectionManager : MonoBehaviour
{
    [Header("Player Prefabları")]
    public GameObject rangerPrefab;
    public GameObject knightPrefab;
    public GameObject magePrefab;

    [Header("Sahne Ayarları")]
    public Transform spawnPoint;             // Karakterin doğacağı nokta
    public GameObject characterSelectPanel;  // Seçim paneli (UI)

    private GameObject currentPlayer;
    private CameraFollow cameraFollow;

    private void Awake()
    {
        // Başta ana kameradan CameraFollow'u almaya çalışalım
        if (Camera.main != null)
        {
            cameraFollow = Camera.main.GetComponent<CameraFollow>();
        }
    }

    // UI butonları buraya bağlanacak:
    public void SelectRanger()
    {
        SpawnPlayer(rangerPrefab);
    }

    public void SelectKnight()
    {
        SpawnPlayer(knightPrefab);
    }

    public void SelectMage()
    {
        SpawnPlayer(magePrefab);
    }

    private void SpawnPlayer(GameObject prefab)
    {
        if (prefab == null || spawnPoint == null)
        {
            Debug.LogWarning("PlayerSelectionManager: Prefab veya SpawnPoint eksik!");
            return;
        }

        // Önceki player varsa sil
        if (currentPlayer != null)
        {
            Destroy(currentPlayer);
        }

        // Yeni oyuncuyu spawn et
        currentPlayer = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);

        // 1) Kamera takibini güncelle
        if (cameraFollow == null && Camera.main != null)
        {
            cameraFollow = Camera.main.GetComponent<CameraFollow>();
        }

        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(currentPlayer.transform);
        }
        else
        {
            Debug.LogWarning("PlayerSelectionManager: CameraFollow bulunamadı!");
        }

        // 2) BuildSystem'e yeni PlayerBuilder'ı bildir
        PlayerBuilder builder = currentPlayer.GetComponent<PlayerBuilder>();
        if (BuildSystem.Instance != null)
        {
            BuildSystem.Instance.SetPlayerBuilder(builder);
        }
        else
        {
            Debug.LogWarning("PlayerSelectionManager: BuildSystem.Instance bulunamadı!");
        }

        // 3) Karakter seçme panelini kapat
        if (characterSelectPanel != null)
            characterSelectPanel.SetActive(false);

        // 4) Oyun akışına haber ver: karakter seçildi, Playing moduna geç
        if (FlowUI.Instance != null)
        {
            FlowUI.Instance.OnCharacterSelected();
        }
        else
        {
            Debug.LogWarning("PlayerSelectionManager: FlowUI.Instance bulunamadı!");
        }
    }
}
