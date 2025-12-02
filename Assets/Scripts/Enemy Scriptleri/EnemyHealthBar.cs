using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [Header("Referanslar")]
    public Image fillImage;         // Kırmızı bar
    public Image backgroundImage;   // Gri arka plan

    [Header("Görünürlük")]
    public bool hideUntilDamaged = true;

    private Health health;

    private void Awake()
    {
        health = GetComponentInParent<Health>();

        if (health == null)
        {
            Debug.LogWarning("EnemyHealthBar: Parent'ta Health bulunamadı!", this);
            enabled = false;
            return;
        }

        // Oyun başlarken görünürlüğü ayarla
        UpdateVisibility();
        UpdateFill();
    }

    private void LateUpdate()
    {
        if (health == null || fillImage == null) return;

        UpdateFill();
        UpdateVisibility();
    }

    private void UpdateFill()
    {
        float t = (float)health.currentHealth / health.maxHealth;
        fillImage.fillAmount = t;
    }

    private void UpdateVisibility()
    {
        bool isDamaged = health.currentHealth < health.maxHealth;
        bool visible = !hideUntilDamaged || isDamaged;

        if (fillImage != null)
            fillImage.enabled = visible;

        if (backgroundImage != null)
            backgroundImage.enabled = visible;
    }
}
