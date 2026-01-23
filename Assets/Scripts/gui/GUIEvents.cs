using System;
using static OPEN.PandemicAI.Enums;

namespace OPEN.PandemicAI
{
    public static class GUIEvents
    {
        public static event Action<GameOverReasons> GameOver;
        public static event Action<int> UpdatePlayerDeckCount;
        public static event Action<int> UpdateInfectionDeckCount;
        public static event Action DrawGUI;
        public static event Action DrawBoard;
        public static event Action DrawCities;
        public static event Action<int> DrawCity;
        public static event Action DrawMarkers;
        public static event Action DrawCubes;
        public static event Action DrawPlayerAreas;
        public static event Action<PlayerPanel> DrawPlayerArea;
        public static event Action<City, VirusName> CubeClicked;
        public static event Action<EligibilityFlags> EligibilityChanged;

        public static void RaiseDrawPlayerDeckTextCount(int count) => UpdatePlayerDeckCount?.Invoke(count);
        public static void RaiseDrawInfectionDeckTextCount(int count) => UpdateInfectionDeckCount?.Invoke(count);
        public static void RaiseGameOver(GameOverReasons didWin) => GameOver?.Invoke(didWin);
        public static void RaiseDrawAll() => DrawGUI?.Invoke();
        public static void RaiseDrawBoard() => DrawBoard?.Invoke();
        public static void RaiseDrawCities() => DrawCities?.Invoke();
        public static void RaiseDrawCity(int cityId) => DrawCity?.Invoke(cityId);
        public static void RaiseDrawMarkers() => DrawMarkers?.Invoke();
        public static void RaiseDrawCubes() => DrawCubes?.Invoke();
        public static void RaiseDrawPlayerAreas() => DrawPlayerAreas?.Invoke();
        public static void RaiseDrawPlayerArea(PlayerPanel panel) => DrawPlayerArea?.Invoke(panel);
        public static void RaiseCubeClicked(City city, VirusName virus) => CubeClicked?.Invoke(city, virus);
        public static void RaiseEligibilityChanged(Player currentPlayer, EligibilityFlags f) => EligibilityChanged?.Invoke(f);
    }
}