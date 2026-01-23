namespace OPEN.PandemicAI
{
    using UnityEngine;
    [CreateAssetMenu(fileName = "GameConfig", menuName = "PandemicAI/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        public readonly int[] InfectionRateValues = new int[] { 2, 2, 3, 4 };
        public int PlayerCardsSeed = -1;
        public int InfectionCardsSeed = -1;

        public int AnimationTimingMultiplier;
        public float PlayerUIOpacity;
        public string PlayerName;
        public string BotName;
        public int startingNCards;
        public Player.Roles FirstToPlay;

        public bool UseFurhat;
        public bool SpeechRecActivated;
        public int EndSilTimeout;
        public int NoSpeechTimeout;
        public bool SimulationMode;
        public bool StepByStepSimulation;
        public bool UseAIQuarantineSpecialist;
        public bool UseAIContainmentSpecialist;
        public int NumberSimulations;
    }
}