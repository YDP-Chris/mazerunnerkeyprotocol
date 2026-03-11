using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Shows escape progress bar when a player is escaping at the exit.
/// Visible to all players.
/// </summary>
public class EscapeProgressUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject progressPanel;
    [SerializeField] private Image progressFill;
    [SerializeField] private TextMeshProUGUI statusText;

    private void Start()
    {
        if (progressPanel != null)
            progressPanel.SetActive(false);

        if (ExitGateway.Instance != null)
        {
            ExitGateway.Instance.OnEscapeProgressChanged += OnProgressChanged;
            ExitGateway.Instance.OnEscapeCancelled += OnCancelled;
            ExitGateway.Instance.OnEscapeComplete += OnComplete;
        }
    }

    private void OnDestroy()
    {
        if (ExitGateway.Instance != null)
        {
            ExitGateway.Instance.OnEscapeProgressChanged -= OnProgressChanged;
            ExitGateway.Instance.OnEscapeCancelled -= OnCancelled;
            ExitGateway.Instance.OnEscapeComplete -= OnComplete;
        }
    }

    private void OnProgressChanged(float progress)
    {
        if (progressPanel != null && !progressPanel.activeSelf)
            progressPanel.SetActive(true);

        if (progressFill != null)
            progressFill.fillAmount = progress;

        if (statusText != null)
            statusText.text = "ESCAPE IN PROGRESS...";
    }

    private void OnCancelled()
    {
        if (progressPanel != null)
            progressPanel.SetActive(false);
    }

    private void OnComplete(ulong winnerClientId)
    {
        if (statusText != null)
            statusText.text = "ESCAPED!";
    }
}
