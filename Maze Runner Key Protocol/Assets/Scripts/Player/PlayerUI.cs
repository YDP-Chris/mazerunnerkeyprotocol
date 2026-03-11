using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections;
using TMPro;

public class PlayerUI : NetworkBehaviour
{
    [Header("Health Bar")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private Image healthBarBackground;
    [SerializeField] private TextMeshProUGUI healthText;

    [Header("Crosshair")]
    [SerializeField] private Image crosshairImage;

    [Header("Damage Flash")]
    [SerializeField] private Image damageFlashImage;
    [SerializeField] private float flashDuration = 0.3f;

    [Header("Elimination")]
    [SerializeField] private GameObject eliminationPanel;

    [Header("Canvas")]
    [SerializeField] private Canvas hudCanvas;

    private PlayerHealth playerHealth;
    private Coroutine flashCoroutine;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            if (hudCanvas != null)
                hudCanvas.gameObject.SetActive(false);
            enabled = false;
            return;
        }

        playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.OnDamaged += HandleDamaged;
            playerHealth.OnHealed += HandleHealed;
            playerHealth.OnDied += HandleDied;
            playerHealth.CurrentHealth.OnValueChanged += HandleHealthChanged;
        }

        if (eliminationPanel != null)
            eliminationPanel.SetActive(false);

        if (damageFlashImage != null)
        {
            var c = damageFlashImage.color;
            c.a = 0f;
            damageFlashImage.color = c;
        }

        UpdateHealthBar(playerHealth != null ? playerHealth.MaxHealth : 100,
                        playerHealth != null ? playerHealth.MaxHealth : 100);
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner || playerHealth == null) return;

        playerHealth.OnDamaged -= HandleDamaged;
        playerHealth.OnHealed -= HandleHealed;
        playerHealth.OnDied -= HandleDied;
        playerHealth.CurrentHealth.OnValueChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(int oldValue, int newValue)
    {
        UpdateHealthBar(newValue, playerHealth.MaxHealth);
    }

    private void HandleDamaged(int amount, int currentHealth)
    {
        UpdateHealthBar(currentHealth, playerHealth.MaxHealth);
        ShowDamageFlash();
    }

    private void HandleHealed(int amount, int currentHealth)
    {
        UpdateHealthBar(currentHealth, playerHealth.MaxHealth);
    }

    private void HandleDied()
    {
        if (crosshairImage != null)
            crosshairImage.gameObject.SetActive(false);

        StartCoroutine(ShowEliminationAfterDelay(0.5f));
    }

    private void UpdateHealthBar(int current, int max)
    {
        float ratio = (float)current / max;

        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = ratio;

            // Color coding
            if (ratio > 0.6f)
                healthBarFill.color = Color.green;
            else if (ratio > 0.3f)
                healthBarFill.color = Color.yellow;
            else
                healthBarFill.color = Color.red;
        }

        if (healthText != null)
            healthText.text = $"{current} / {max}";
    }

    private void ShowDamageFlash()
    {
        if (damageFlashImage == null) return;

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(DamageFlashRoutine());
    }

    private IEnumerator DamageFlashRoutine()
    {
        var c = damageFlashImage.color;
        c.a = 0.3f;
        damageFlashImage.color = c;

        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(0.3f, 0f, elapsed / flashDuration);
            damageFlashImage.color = c;
            yield return null;
        }

        c.a = 0f;
        damageFlashImage.color = c;
    }

    private IEnumerator ShowEliminationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (eliminationPanel != null)
            eliminationPanel.SetActive(true);

        if (healthBarFill != null)
            healthBarFill.transform.parent.gameObject.SetActive(false);

        if (healthText != null)
            healthText.gameObject.SetActive(false);

        if (crosshairImage != null)
            crosshairImage.gameObject.SetActive(false);
    }
}
