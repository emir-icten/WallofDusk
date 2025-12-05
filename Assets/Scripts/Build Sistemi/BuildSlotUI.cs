using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BuildSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Referanslar")]
    public BuildSystem buildSystem;
    public int buildingIndex = 0;

    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;

    private void Start()
    {
        RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (buildSystem == null) return;
        if (buildSystem.buildings == null) return;
        if (buildingIndex < 0 || buildingIndex >= buildSystem.buildings.Length) return;

        var cfg = buildSystem.buildings[buildingIndex];

        if (nameText != null)
            nameText.text = cfg.displayName;

        if (costText != null)
            costText.text = $"{cfg.woodCost} Wood / {cfg.stoneCost} Stone";
    }

    // Parmağı / fareyi buton üzerinde basılı tutup sürüklemeye başlayınca
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (buildSystem != null)
        {
            buildSystem.StartPlacement(buildingIndex);
        }
    }

    // Sürükleme sırasında BuildSystem zaten Input.mousePosition ile takip ediyor,
    // burada ekstra bir şey yapmamıza gerek yok ama interface'i implement etmek için boş bırakıyoruz.
    public void OnDrag(PointerEventData eventData)
    {
    }

    // Parmağı / fareyi bıraktığın frame
    public void OnEndDrag(PointerEventData eventData)
    {
        if (buildSystem != null && buildSystem.IsPlacing)
        {
            buildSystem.ConfirmPlacement();
        }
    }
}
