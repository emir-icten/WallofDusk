using UnityEngine;

public class DamagePopupSpawner : MonoBehaviour
{
    public static DamagePopupSpawner Instance;

    [Header("Popup Prefab")]
    public DamagePopup popupPrefab;

    [Header("Spawn Offset")]
    public Vector3 offset = new Vector3(0f, 1.5f, 0f);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void Spawn(int damage, Vector3 worldPos)
    {
        if (popupPrefab == null) return;

        DamagePopup popup = Instantiate(
            popupPrefab,
            worldPos + offset,
            Quaternion.identity
        );

        popup.Setup(damage);
    }
}
