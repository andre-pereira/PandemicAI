using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static OPEN.PandemicAI.Enums;

namespace OPEN.PandemicAI
{
    public class TurnFlow : MonoBehaviour
    {
        //public static event System.Action<GameState> OnStateChanged;

        private GameStateData s;
        private GameCatalog catalog;
        private GameConfig config;
        private DeckEngine deck;
        private bool isInitialized = false;

        public void Init(GameStateData state, GameCatalog cat, GameConfig cfg, DeckEngine dk)
        {
            s = state;
            catalog = cat;
            config = cfg;
            deck = dk;

            GameEvents.StateChanged += ChangeState;
            GameEvents.NextPlayer += NextPlayer;
            GameEvents.GameResetRequested += StartNewGame;

            isInitialized = true;
        }

        public void StartNewGame()
        {
            s.TurnNumber = 1;
            s.InitializeSeeds();
            s.InitializeVariables();
            deck.ResetDecks();

            foreach (var city in CityDrawer.CityScripts) { city.Initialize(); }
            initPlayers();
            deck.AddEpidemicCards();

            int[] infectionInitialization = { 3, 3, 2, 2, 1, 1 };
            foreach (int nCubes in infectionInitialization)
            {
                int cityID = s.InfectionDeck.Pop();
                VirusName virusName = CityDrawer.CityScripts[cityID].CityCard.VirusInfo.virusName;
                s.InfectionDiscard.Add(cityID);
                s.TryTakeCubes(virusName, nCubes);
                CityDrawer.AddCubes(cityID, virusName, nCubes);
            }

            Timeline.Instance.ResetTimeline();
            Timeline.Instance.AddEvent(new EInitializeFirstPlayer());
            AIDrawer.Initialize();
        }

        private void initPlayers()
        {
            s.Players.Clear();
            s.Players.Add(new Player(PlayerAreaDrawer.Panels[0], Player.Roles.ContainmentSpecialist, config.PlayerName));
            s.Players.Add(new Player(PlayerAreaDrawer.Panels[1], Player.Roles.QuarantineSpecialist, config.BotName));

            s.CurrentPlayer = s.Players[(int)GameRoot.Config.FirstToPlay];

            foreach (Player player in s.Players)
            {
                for (int i = 0; i < config.startingNCards; ++i)
                {
                    player.AddCardToHand(deck.DrawPlayerCard());
                }
                player.UpdateCurrentCity(GameRoot.Catalog.InitialCityId, false);
            }

            PlayerAreaDrawer.Init(s.Players);
        }

        private void Update()
        {
            if (!isInitialized)
            {
                return;
            }
            Tick();
        }
        private void Tick()
        {
            if (s.Players.Any(p => p.Hand.Count > 6))
            {
                return;
            }

            switch (s.CurrentState)
            {
                case GameState.DrawPlayerCards: HandleDrawPlayerCards(); break;
                case GameState.Epidemic: HandleEpidemic(); break;
                case GameState.DrawInfectionCards: HandleDrawInfectionCards(); break;
                case GameState.Outbreak: HandleOutbreak(); break;
                case GameState.Discarding: HandleDiscard(); break;
            }
        }

        private void HandleDiscard()
        {
            if (s.ActionCompleted)
            {
                if (s.PreviousState == GameState.PlayerActions && s.CurrentPlayer.ActionsRemaining == 0)
                {
                    ChangeState(GameState.DrawPlayerCards);
                }
                else
                {
                   ChangeState(s.PreviousState); 
                }
                
            }
        }

        private void HandleDrawPlayerCards()
        {
            if (s.PlayerCardsDrawn >= 2 && s.PreviousState == GameState.Discarding)
                s.ActionCompleted = true;
            else
            {
                if (!s.ActionsInitiated)
                {
                    s.ActionsInitiated = true;
                    Timeline.Instance.AddEvent(new PDealCard());
                    s.PlayerCardsDrawn++;
                }
            }

            if (s.ActionCompleted)
            {
                s.ActionCompleted = false;
                if (s.PlayerCardsDrawn < 2)
                    s.ActionsInitiated = false;
                else if (s.CurrentPlayer.Hand.Count < 7 && s.PlayerCardsDrawn == 2)
                {
                    if (s.CurrentState != GameState.Epidemic || s.CurrentState != GameState.Discarding) ChangeState(GameState.DrawInfectionCards);
                }
            }
        }

        private void HandleEpidemic()
        {
            if (s.EpidemicStage == EpidemicState.Increase)
            {
                Timeline.Instance.AddEvent(new EIncreaseInfectionRate());
                s.EpidemicStage = EpidemicState.Infect;
                Timeline.Instance.AddEvent(new EDrawInfectionCard(3, false));
            }
            else if (s.EpidemicStage == EpidemicState.Infect)
            {
                if (s.ActionCompleted)
                {
                    s.EpidemicStage = EpidemicState.Intensify;
                    Timeline.Instance.AddEvent(new EIntensify());
                    ChangeState(s.PreviousState);
                }
            }
        }

        private void HandleDrawInfectionCards()
        {
            if (s.InfectionCardsDrawn < config.InfectionRateValues[s.InfectionRateIndex] && !s.TurnEnded)
            {
                if (!s.ActionsInitiated)
                {
                    s.ActionsInitiated = true;
                    Timeline.Instance.AddEvent(new EDrawInfectionCard(1, true));
                }
                if (s.ActionCompleted)
                {
                    s.ActionsInitiated = false;
                    s.ActionCompleted = false;
                }
            }
            else
            {
                if (!s.TurnEnded && s.ActionCompleted)
                {
                    s.TurnEnded = true;
                    Timeline.Instance.AddEvent(new PNewTurn());
                }
            }
        }

        private void HandleOutbreak()
        {
            if (s.ActionCompleted)
            {
                s.OutbreakTracker.Clear();
                if (s.InfectionCardsDrawn >= config.InfectionRateValues[s.InfectionRateIndex] && !s.TurnEnded)
                {
                    // End turn if outbreak is triggered on the last drawn infection card.
                    s.TurnEnded = true;
                    Timeline.Instance.AddEvent(new PNewTurn());
                }
                else
                {
                    ChangeState(GameState.DrawInfectionCards);
                }
            }
        }

        public void ChangeState(GameState next)
        {
            if (s.CurrentState == GameState.Discarding && next == GameState.PlayerActions)
            { 
                GameRoot.State.UpdatePlayerActions();
                
            }
            if ( s.CurrentState == GameState.PlayerActions && s.Players.Any(p => p.Hand.Count > 6))
            {
                return;
            }

            s.PreviousState = s.CurrentState;
            s.CurrentState = next;
            //Debug.Log($"Changing state from {s.PreviousState} to {s.CurrentState}");
            s.ActionsInitiated = s.ActionCompleted = s.TurnEnded = false;
            if (next == GameState.PlayerActions) s.PlayerCardsDrawn = s.InfectionCardsDrawn = 0;
            else if (next == GameState.Epidemic) s.EpidemicStage = EpidemicState.Increase;
            //OnStateChanged?.Invoke(next);
        }

        private void NextPlayer()
        {
            var list = s.Players;
            s.TurnNumber++;
            int currentIndex = list.IndexOf(s.CurrentPlayer);
            int nextIndex = (currentIndex + 1) % list.Count;
            s.CurrentPlayer = list[nextIndex];
            s.CurrentPlayer.ResetTurn();
        }
    }
}
