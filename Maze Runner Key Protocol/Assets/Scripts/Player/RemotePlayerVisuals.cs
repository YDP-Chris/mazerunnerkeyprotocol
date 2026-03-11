using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;

/// <summary>
/// Manages visual representation for remote players: body mesh visibility,
/// color assignment, nameplate, and floating health bar. Hides visuals on the owner instance.
/// </summary>
public class RemotePlayerVisuals : NetworkBehaviour
{
    [Header("Body")]
    [SerializeField] private MeshRenderer bodyRenderer;

    [Header("Nameplate")]
    [SerializeField] private Canvas nameplateCanvas;
    [SerializeField] private TextMeshProUGUI nameplateText;

    [Header("Health Bar (auto-created)")]
    private Image healthBarFill;
    private Image healthBarBg;

    public static readonly Color[] PlayerColors = new Color[]
    {
        new Color(0.2f, 0.5f, 0.9f),  // Blue
        new Color(0.9f, 0.2f, 0.2f),  // Red
        new Color(0.2f, 0.8f, 0.3f),  // Green
        new Color(0.9f, 0.8f, 0.1f),  // Yellow
        new Color(0.8f, 0.3f, 0.8f),  // Purple
        new Color(0.9f, 0.5f, 0.1f),  // Orange
        new Color(0.1f, 0.8f, 0.8f),  // Cyan
        new Color(0.9f, 0.4f, 0.6f),  // Pink
    };

    private PlayerHealth playerHealth;

    public override void OnNetworkSpawn()
    {
        playerHealth = GetComponent<PlayerHealth>();

        if (IsOwner)
        {
            // Hide body and nameplate on owner (first-person view)
            if (bodyRenderer != null)
                bodyRenderer.enabled = false;
            if (nameplateCanvas != null)
                nameplateCanvas.gameObject.SetActive(false);
        }
        else
        {
            // Assign color based on owner client ID
            int playerIndex = GetPlayerIndex();
            if (bodyRenderer != null)
            {
                var mat = new Material(bodyRenderer.sharedMaterial);
                mat.SetColor("_BaseColor", PlayerColors[playerIndex % PlayerColors.Length]);
                bodyRenderer.material = mat;
            }

            // Set nameplate text
            if (nameplateText != null)
            {
                nameplateText.text = $"Player {playerIndex + 1}";
            }

            // Create floating health bar under nameplate
            CreateFloatingHealthBar(playerIndex);

            // Subscribe to health changes
            if (playerHealth != null)
            {
                playerHealth.CurrentHealth.OnValueChanged += OnHealthChanged;
                UpdateHealthBar(playerHealth.CurrentHealth.Value, playerHealth.MaxHealth);
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner && playerHealth != null)
            playerHealth.CurrentHealth.OnValueChanged -= OnHealthChanged;
    }

    private void CreateFloatingHealthBar(int playerIndex)
    {
        if (nameplateCanvas == null) return;

        // Health bar background
        var bgGo = new GameObject("HealthBarBg");
        bgGo.transform.SetParent(nameplateCanvas.transform, false);
        var bgRect = bgGo.AddComponent<RectTransform>();
        bgRect.anchoredPosition = new Vector2(0, -30f);
        bgRect.sizeDelta = new Vector2(150, 12);
        healthBarBg = bgGo.AddComponent<Image>();
        healthBarBg.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);

        // Health bar fill
        var fillGo = new GameObject("HealthBarFill");
        fillGo.transform.SetParent(bgGo.transform, false);
        var fillRect = fillGo.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(1, 1);
        fillRect.offsetMax = new Vector2(-1, -1);
        healthBarFill = fillGo.AddComponent<Image>();
        healthBarFill.type = Image.Type.Filled;
        healthBarFill.fillMethod = Image.FillMethod.Horizontal;
        healthBarFill.color = PlayerColors[playerIndex % PlayerColors.Length];
    }

    private void OnHealthChanged(int oldValue, int newValue)
    {
        if (playerHealth != null)
            UpdateHealthBar(newValue, playerHealth.MaxHealth);
    }

    private void UpdateHealthBar(int current, int max)
    {
        if (healthBarFill == null) return;
        float ratio = (float)current / max;
        healthBarFill.fillAmount = ratio;

        if (ratio > 0.6f)
            healthBarFill.color = Color.green;
        else if (ratio > 0.3f)
            healthBarFill.color = Color.yellow;
        else
            healthBarFill.color = Color.red;
    }

    public int GetPlayerIndex()
    {
        // Determine player index from connected clients order
        if (ConnectionManager.Instance != null)
        {
            for (int i = 0; i < ConnectionManager.Instance.Players.Count; i++)
            {
                if (ConnectionManager.Instance.Players[i].ClientId == OwnerClientId)
                    return i;
            }
        }
        // Fallback: use clientId directly
        return (int)(OwnerClientId % (ulong)PlayerColors.Length);
    }
}
