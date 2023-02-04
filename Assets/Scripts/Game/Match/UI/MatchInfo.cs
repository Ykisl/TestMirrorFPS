using UnityEngine;
using TMPro;
using System;

namespace Game.Match.UI
{
    public class MatchInfo : MonoBehaviour
    {
        [SerializeField] private MatchService matchService;
        [Space]
        [SerializeField] private TextMeshProUGUI roundTimerText;
        [SerializeField] private TextMeshProUGUI roundText;
        [SerializeField] private TextMeshProUGUI scoreText;

        private void Start()
        {
            matchService.OnClientRoundTimerChanged += RoundTimerChanged;
            matchService.OnClientRoundChanged += RoundChanged;
            matchService.OnClientPlayerScoreChanged += PlayerScoreChanged;

            RoundChanged(matchService.Round, matchService.MaxRounds);
        }

        private void OnDestroy()
        {
            matchService.OnClientRoundTimerChanged -= RoundTimerChanged;
            matchService.OnClientRoundChanged -= RoundChanged;
            matchService.OnClientPlayerScoreChanged -= PlayerScoreChanged;
        }

        private void RoundTimerChanged(float seconds, float maxSeconds)
        {
            var secondsRemaining = maxSeconds - seconds;

            var timeSpan = TimeSpan.FromSeconds(secondsRemaining);
            roundTimerText.text = $"Next round: {timeSpan.ToString(@"mm\:ss")}";
        }

        private void RoundChanged(int round, int maxRound)
        {
            roundText.text = $"Round: {round}/{maxRound}";
        }

        private void PlayerScoreChanged(int playerId, int score)
        {
            scoreText.text = $"Score: {score}";
        }
    }
}
