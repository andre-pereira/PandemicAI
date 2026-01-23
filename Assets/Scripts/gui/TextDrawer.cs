using System;
using TMPro;
using UnityEngine;
using static OPEN.PandemicAI.Enums;

namespace OPEN.PandemicAI
{    
    public class TextDrawer : MonoBehaviour
    {
        [SerializeField] public TextMeshProUGUI PlayerDeckCount;
        [SerializeField] public TextMeshProUGUI InfectionDeckCount;
        [SerializeField] public TextMeshProUGUI BigTextMessage;
        [SerializeField] public GameObject GameEndWin;
        [SerializeField] public GameObject GameEndLose;

        public void Awake()
        {
            // Disable raycast targets for all TextMeshProUGUI elements to prevent them from blocking raycasts.
            TextMeshProUGUI[] allTMPTexts = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
            foreach (TextMeshProUGUI tmpText in allTMPTexts) tmpText.raycastTarget = false;
        }

        private void OnEnable()
        {
            GameEvents.StateChanged += drawBigContextText;
            GUIEvents.GameOver += ShowEndScreen;
            GUIEvents.UpdatePlayerDeckCount += (count) => PlayerDeckCount.text = count.ToString();
            //GUIEvents.UpdateInfectionDeckCount += (count) => InfectionDeckCount.text = count.ToString();
            GUIEvents.UpdateInfectionDeckCount += updateInfectionDeckText;
        }

        private void updateInfectionDeckText(int count)
        {
            InfectionDeckCount.text = count.ToString();
        }

        private void ShowEndScreen(GameOverReasons reason)
        {
            if(reason == GameOverReasons.PlayersWon)
            {
                GameEndWin.SetActive(true);
            }
            else
            {
                GameEndLose.SetActive(true);
            }
        }

        public void drawBigContextText(GameState state)
        {
            switch (state)
            {
                case GameState.Initializing:
                    BigTextMessage.text = "Set up phase";
                    break;
                case GameState.PlayerActions:
                    BigTextMessage.text = $"{GameRoot.State.CurrentPlayer.Name}'s turn";
                    break;
                case GameState.DrawPlayerCards:
                    BigTextMessage.text = $"Drawing Player Cards: {GameRoot.State.PlayerCardsDrawn}";
                    break;
                case GameState.Epidemic:
                    switch (GameRoot.State.EpidemicStage)
                    {
                        case EpidemicState.Increase:
                            BigTextMessage.text = "Epidemic: Increase";
                            break;
                        case EpidemicState.Infect:
                            BigTextMessage.text = "Epidemic: Infect";
                            break;
                        case EpidemicState.Intensify:
                            BigTextMessage.text = "Epidemic: Intensify";
                            break;
                    }
                    break;
                case GameState.DrawInfectionCards:
                    BigTextMessage.text = "Drawing Infection Cards";
                    break;
                case GameState.Discarding:
                    BigTextMessage.text = "Discarding Cards";
                    break;
                case GameState.Outbreak:
                    BigTextMessage.text = "Outbreak!";
                    break;
                case GameState.GameOver:
                    BigTextMessage.text = "Game Over!";
                    break;
                default:
                    break;
            }
        }

    }
}