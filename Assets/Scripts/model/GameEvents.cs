using System;
using static OPEN.PandemicAI.Enums;

namespace OPEN.PandemicAI
{
    public static class GameEvents
    {
        public static event Action<GameState> StateChanged;
        public static event Action NextPlayer;
        public static event Action GameResetRequested;

        public static void RequestStateChange(GameState next) => StateChanged?.Invoke(next);
        public static void RequestNextPlayer() => NextPlayer?.Invoke();
        public static void RequestGameReset() => GameResetRequested?.Invoke();
    }
}