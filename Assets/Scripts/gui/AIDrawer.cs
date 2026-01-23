using TMPro;
using UnityEngine;

namespace OPEN.PandemicAI
{
    public class AIDrawer : MonoBehaviour
    {
        public GameObject AIQuarantineSpecialist;
        public GameObject AIContainmentSpecialist;
        public GameObject AIFurhat;

        public static GameObject AIQuarantineSpecialistStatic;
        public static GameObject AIContainmentSpecialistStatic;

        public void Awake()
        {
            AIQuarantineSpecialistStatic = AIQuarantineSpecialist;
            AIContainmentSpecialistStatic = AIContainmentSpecialist;
        
            if (GameRoot.Config.UseFurhat) 
            {
                GameRoot.Config.SimulationMode = false;
                GameRoot.Config.StepByStepSimulation = false;
                GameRoot.Config.UseAIQuarantineSpecialist = false;
                AIFurhat.SetActive(true);
            }
            else
            {
                GameRoot.Config.SpeechRecActivated = false;
                AIFurhat.SetActive(false);
                if (GameRoot.Config.SimulationMode)
                {
                    GameRoot.Config.UseAIQuarantineSpecialist = true;
                    GameRoot.Config.UseAIContainmentSpecialist = true;
                }
            }

            if (GameRoot.Config.UseAIQuarantineSpecialist)
                AIQuarantineSpecialist.SetActive(true);
            else
                AIQuarantineSpecialist.SetActive(false);

            if (GameRoot.Config.UseAIContainmentSpecialist)
                AIContainmentSpecialist.SetActive(true);
            else
                AIContainmentSpecialist.SetActive(false);
        }

        public static void Initialize()
        {
            if (GameRoot.Config.UseAIQuarantineSpecialist)
            {
                var aiQuarantine = AIQuarantineSpecialistStatic.GetComponent<AIPlayer>();
                aiQuarantine.Initialize(GameRoot.State.Players[1], GameRoot.State.Players[0]);
            }    
            if (GameRoot.Config.UseAIContainmentSpecialist)
            {
                var aiContainment = AIContainmentSpecialistStatic.GetComponent<AIPlayer>();
                aiContainment.Initialize(GameRoot.State.Players[0], GameRoot.State.Players[1]);
                
            }
            GUIEvents.RaiseDrawAll();
        }
    }
}