using UnityEngine;

namespace OPEN.PandemicAI
{
    //scriptable object to hold game data
    [CreateAssetMenu(fileName = "GameCatalog", menuName = "Game Data/Game Catalog")]
    public class GameCatalog : ScriptableObject
    {
        [SerializeField] public VirusInfo[] VirusInfos;
        [SerializeField] public RoleCard[] RoleCards;

        [Header("Cube & pawn layout")]
        public Vector2[] cubeOffsetRed = { new(0.61f, 0.00f), new(0.61f, -0.30f), new(0.61f, -0.60f) };
        public Vector2[] cubeOffsetYellow = { new(0.38f, 0.00f), new(0.38f, -0.30f), new(0.38f, -0.60f) };
        public Vector2[] cubeOffsetBlue = { new(0.15f, 0.00f), new(0.15f, -0.30f), new(0.15f, -0.60f) };
        public Vector2[] pawnOffsets = { new(-0.10f, 0.55f), new(0.10f, 0.55f), new(0.30f, 0.45f), new(-0.30f, 0.45f) };

        public static readonly int NumberOfCities = 24;
        public readonly int EpidemicCardIndex = 24;
        public readonly int NumberOfEpidemicCards = 24;
        public readonly int InitialCityId = 13;

        public readonly int[] InfectionRateValues = { 2, 2, 3, 4 };
        public readonly int CubesPerColor = 16;   
        public readonly int HandLimit = 6;
        public readonly int ActionsPerTurn = 4;

        public const int PLAYER_COUNT = 2;
    }
}