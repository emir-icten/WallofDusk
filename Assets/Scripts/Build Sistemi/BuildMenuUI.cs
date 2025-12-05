using UnityEngine;

public class BuildMenuUI : MonoBehaviour
{
    public GameObject buildPanel;  // Aşağı açılan panel
    public bool isOpen = false;    // Başlangıçta kapalı olsun

    private void Start()
    {
        if (buildPanel != null)
            buildPanel.SetActive(isOpen);   // isOpen = false → panel gizli
    }

    public void TogglePanel()
    {
        isOpen = !isOpen;

        if (buildPanel != null)
            buildPanel.SetActive(isOpen);
    }
}
