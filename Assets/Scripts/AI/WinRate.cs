using OPEN.PandemicAI;
using UnityEngine;
using static OPEN.PandemicAI.Enums;
using TMPro;
using System.Collections.Generic;

public class WinRate : MonoBehaviour
{
    public TextMeshProUGUI textbox;

    private int num_wins = 0;
    private int games_completed = 0;
    private int gameId;
    private List<int> gamesWon = new List<int>();
    private int lastProcessedGameId = -1;

    private void Start()
    {
        gameId = GameRoot.Config.PlayerCardsSeed;
    }

    private void Update()
    {
        if (GameRoot.Config.SimulationMode)
        {
            foreach (var evt in Timeline.Instance.ProcessedEvents)
            {
                if (evt is EGameOver gameOverEvent && gameId != lastProcessedGameId)
                {
                    lastProcessedGameId = gameId;

                    if (gameOverEvent.Reason == GameOverReasons.PlayersWon)
                    {
                        num_wins++;
                        gamesWon.Add(gameId);
                    }

                    games_completed++;

                    if (games_completed < GameRoot.Config.NumberSimulations)
                    {
                        StartNextGame();
                    }
                    else
                    {
                        textbox.text = $"Number of wins: {num_wins} | Win Rate: {(float)num_wins / games_completed * 100f:F1}%";
                        Debug.Log($"Won games: {string.Join(", ", gamesWon)}");
                    }
                    break;
                }
            }
        }
    }

    private void StartNextGame()
    {
        gameId++;
        GameRoot.Config.PlayerCardsSeed = gameId;
        GameRoot.Config.InfectionCardsSeed = gameId;
        textbox.text = $"Game {gameId}";

        GameEvents.RequestGameReset();
    }
}