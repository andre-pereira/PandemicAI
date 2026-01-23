// GameRoot.cs
using OPEN.PandemicAI;
using UnityEngine;

namespace OPEN.PandemicAI
{
    /// <summary>
    /// Entry-point MonoBehaviour that wires all runtime objects together
    /// and starts a new match.
    /// </summary>
    public class GameRoot : MonoBehaviour
    {
        public static float StartTimestamp { get; private set; }
        [SerializeField] private GameCatalog gameCatalog;
        [SerializeField] private GameConfig gameConfig;

        [SerializeField] private TurnFlow turnFlow;

        private GameStateData state;
        private DeckEngine deck;

        public static GameStateData State { get; private set; }
        public static GameCatalog Catalog { get; private set; }
        public static GameConfig Config { get; private set; }

        public static GameObject CurrentEpidemicObject = null;


        private void Awake()
        {
            Catalog = gameCatalog;
            Config = gameConfig;
        }

        // Drag your GameStartManager object here in the Inspector
        public GameStartManager gameStartManager;

        private void Start()
        {
            // The game should start immediately if:
            // 1. The GameStartManager is not assigned in the inspector (null).
            // 2. The GameObject containing the GameStartManager is disabled.
            // 3. The backend is set to LMStudio.
            if (gameStartManager == null || !gameStartManager.gameObject.activeInHierarchy)
            {
                Debug.Log("GameStartManager not required or is disabled. Starting game immediately.");
                gameStartManager.apiKeyPanel.SetActive(false); // Hide the API key panel if it's not needed
                InitializeGame();
            }
            else
            {
                // If the GameStartManager is active and needs a key, wait for its signal.
                Debug.Log("Waiting for a valid API key before initializing the game...");
                GameStartManager.OnApiKeyReady += InitializeGame;
            }
        }

        // This method contains your original game setup logic
        private void InitializeGame()
        {
            // Unsubscribe to prevent calling this method multiple times
            if (gameStartManager != null)
            {
                GameStartManager.OnApiKeyReady -= InitializeGame;
            }

            Debug.Log("Initializing game systems...");

            StartTimestamp = Time.time;
            state = new GameStateData();
            State = state;

            deck = new DeckEngine();
            deck.Init(state, Catalog);

            turnFlow.Init(state, gameCatalog, gameConfig, deck);
            turnFlow.StartNewGame();
        }

        private void OnDestroy()
        {
            // Ensure we unsubscribe if the object is destroyed
            if (gameStartManager != null)
            {
                GameStartManager.OnApiKeyReady -= InitializeGame;
            }
        }
    }
}
