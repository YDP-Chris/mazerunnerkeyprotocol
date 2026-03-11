using UnityEngine;
using Unity.Netcode;
using TMPro;

/// <summary>
/// Manages visual representation for remote players: body mesh visibility,
/// color assignment, and nameplate. Hides visuals on the owner instance.
/// </summary>
public class RemotePlayerVisuals : NetworkBehaviour
{
    [Header("Body")]
    [SerializeField] private MeshRenderer bodyRenderer;

    [Header("Nameplate")]
    [SerializeField] private Canvas nameplateCanvas;
    [SerializeField] private TextMeshProUGUI nameplateText;

    private static readonly Color[] PlayerColors = new Color[]
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

    public override void OnNetworkSpawn()
    {
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
        }
    }

    private int GetPlayerIndex()
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
