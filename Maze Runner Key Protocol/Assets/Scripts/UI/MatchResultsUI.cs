using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

/// <summary>
/// Match results screen displayed on match end.
/// Shows outcome (Win/Draw), winner name, and match duration.
/// </summary>
public class MatchResultsUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject resultsPanel;
    [SerializeField] private TextMeshProUGUI outcomeText;
    [SerializeField] private TextMeshProUGUI winnerText;
    [SerializeField] private TextMeshProUGUI durationText;

    private void Start()
    {
        if (resultsPanel != null)
            resultsPanel.SetActive(false);

        if (MatchManager.Instance != null)
            MatchManager.Instance.OnMatchEnded += ShowResults;
    }

    private void OnDestroy()
    {
        if (MatchManager.Instance != null)
            MatchManager.Instance.OnMatchEnded -= ShowResults;
    }

    private void ShowResults(MatchManager.MatchOutcome outcome, ulong winnerClientId)
    {
        if (resultsPanel != null)
            resultsPanel.SetActive(true);

        if (outcomeText != null)
        {
            outcomeText.text = outcome == MatchManager.MatchOutcome.Win
                ? "ESCAPE SUCCESSFUL"
                : "NO ESCAPE - DRAW";
            outcomeText.color = outcome == MatchManager.MatchOutcome.Win
                ? new Color(1f, 0.85f, 0.2f) // gold
                : Color.red;
        }

        if (winnerText != null)
        {
            if (outcome == MatchManager.MatchOutcome.Win)
            {
                bool isLocalWinner = NetworkManager.Singleton != null
                    && NetworkManager.Singleton.LocalClientId == winnerClientId;
                winnerText.text = isLocalWinner ? "YOU ESCAPED!" : $"Player {winnerClientId} Escaped";
                winnerText.color = isLocalWinner ? Color.green : Color.white;
            }
            else
            {
                winnerText.text = "All Players Eliminated";
                winnerText.color = Color.gray;
            }
        }

        if (durationText != null && MatchManager.Instance != null)
        {
            float duration = MatchManager.Instance.MatchDuration.Value;
            int minutes = Mathf.FloorToInt(duration / 60f);
            int seconds = Mathf.FloorToInt(duration % 60f);
            durationText.text = $"Match Duration: {minutes}:{seconds:D2}";
        }
    }
}
