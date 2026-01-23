using System;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using static OPEN.PandemicAI.Enums;

namespace OPEN.PandemicAI
{
    public class GameStateData
    {
        // ----- high-level state -----------------------------------------
        public GameState CurrentState = GameState.Initializing;
        public GameState PreviousState = GameState.Initializing;
        public EpidemicState EpidemicStage = EpidemicState.Increase;

        // ----- decks -----------------------------------------------------
        public readonly List<int> PlayerDeck = new();
        public readonly List<int> PlayerDiscard = new();
        public readonly List<int> InfectionDeck = new();
        public readonly List<int> InfectionDiscard = new();

        // ----- board counters -------------------------------------------
        public int RedCubes = 16;
        public int YellowCubes = 16;
        public int BlueCubes = 16;

        public bool RedCureFound = false;
        public bool YellowCureFound = false;
        public bool BlueCureFound = false;

        public int InfectionRateIndex = 0;
        public int OutbreakCounterIndex = 0;
        public int PlayerCardsDrawn = 0;
        public int InfectionCardsDrawn = 0;

        // ----- players ---------------------------------------------------
        public Player CurrentPlayer;
        public readonly List<Player> Players = new();

        // ----- flags used by TurnFlow -----------------------------------
        public bool ActionsInitiated;
        public bool ActionCompleted;
        public bool TurnEnded;
        public int TurnNumber = 0;

        public List<int> OutbreakTracker = new List<int>();

        public EligibilityFlags PossibleActions = EligibilityFlags.None;

        public UnityEngine.Random.State playerRngState;
        public UnityEngine.Random.State infectionRngState;

        public void InitializeVariables()
        {
            CurrentState = GameState.Initializing;
            PreviousState = GameState.Initializing;
            EpidemicStage = EpidemicState.Increase;
            RedCubes = 16;
            YellowCubes = 16;
            BlueCubes = 16;
            RedCureFound = false;
            YellowCureFound = false;
            BlueCureFound = false;
            InfectionRateIndex = 0;
            OutbreakCounterIndex = 0;
            PlayerCardsDrawn = 0;
            InfectionCardsDrawn = 0;
        }

        public void AddCubesToBoard(VirusName virus, int delta)
        {
            switch (virus)
            {
                case VirusName.Red: RedCubes += delta; break;
                case VirusName.Yellow: YellowCubes += delta; break;
                case VirusName.Blue: BlueCubes += delta; break;
            }
        }

        public bool TryTakeCubes(VirusName virus, int delta)
        {
            switch (virus)
            {
                case VirusName.Red:
                    if (RedCubes >= delta)
                    {
                        RedCubes -= delta;
                        return true;
                    }
                    break;
                case VirusName.Yellow:
                    if (YellowCubes >= delta)
                    {
                        YellowCubes -= delta;
                        return true;
                    }
                    break;
                case VirusName.Blue:
                    if (BlueCubes >= delta)
                    {
                        BlueCubes -= delta;
                        return true;
                    }
                    break;
            }
            return false;
        }

        public void UpdatePlayerActions()
        {
            PossibleActions = ActionRules.Compute(CurrentPlayer, this);
            GUIEvents.RaiseEligibilityChanged(CurrentPlayer, PossibleActions);
        }

        internal bool IsCuredColor(VirusName color)
        {
            switch (color)
            {
                case VirusName.Red: return RedCureFound;
                case VirusName.Yellow: return YellowCureFound;
                case VirusName.Blue: return BlueCureFound;
                default: return false; // No cure for None or other invalid colors
            }
        }

        internal int GetCubeStock(VirusName color)
        {
            switch (color)
            {
                case VirusName.Red: return RedCubes;
                case VirusName.Yellow: return YellowCubes;
                case VirusName.Blue: return BlueCubes;
                default: return 0; // No cubes for None or other invalid colors
            }
        }
        public void InitializeSeeds()
        {
            int randomSeed = Mathf.Abs(DateTime.UtcNow.Ticks.GetHashCode());

            var _playerCardsSeed = GameRoot.Config.PlayerCardsSeed == -1 ? randomSeed : GameRoot.Config.PlayerCardsSeed;
            var _infectionCardsSeed = GameRoot.Config.InfectionCardsSeed == -1 ? randomSeed : GameRoot.Config.InfectionCardsSeed;

            UnityEngine.Random.InitState(_playerCardsSeed);
            playerRngState = UnityEngine.Random.state;

            UnityEngine.Random.InitState(_infectionCardsSeed);
            infectionRngState = UnityEngine.Random.state;
        }
    }

    public enum GameState
    {
        Initializing,
        PlayerActions,
        DrawPlayerCards,
        Epidemic,
        DrawInfectionCards,
        Discarding,
        Outbreak,
        GameOver
    }

    public enum EpidemicState { Increase, Infect, Intensify }

    [System.Flags]
    public enum EligibilityFlags
    {
        None = 0,
        Move = 1 << 0,
        Treat = 1 << 1,
        Share = 1 << 2,
        Cure = 1 << 3,
        DirectFlight = 1 << 4,
        CharterFlight = 1 << 5,
        EndTurn = 1 << 6
    }
}