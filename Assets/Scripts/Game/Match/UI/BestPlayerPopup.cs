using Game.Match;
using TMPro;
using UnityEngine;

namespace Game.Match.UI
{
    public class BestPlayerPopup : MonoBehaviour
    {
        [SerializeField] private MatchService matchService;
        [Space]
        [SerializeField] private TextMeshProUGUI bestPlayerText;
        [SerializeField] private TextMeshProUGUI bestScoreText;

        private void Start()
        {
            matchService.OnClientScoreResult += ScoreResultShow;
            matchService.OnClientScoreResultClosed += ScoreResultClosed;

            Close();
        }

        private void OnDestroy()
        {
            matchService.OnClientScoreResult -= ScoreResultShow;
            matchService.OnClientScoreResultClosed -= ScoreResultClosed;
        }

        private void Show()
        {
            gameObject.SetActive(true);
        }

        private void Close()
        {
            gameObject.SetActive(false);
        }

        private void ScoreResultShow(int bestPlayerId, int bestScore)
        {
            bestPlayerText.text = $"Best player: Игрок {bestPlayerId}";
            bestScoreText.text = $"Best score: {bestScore}";

            Show();
        }

        private void ScoreResultClosed()
        {
            Close();
        }
    }
}
