using TMPro;
using UnityEngine;

namespace OPEN.PandemicAI
{
    public class EpidemicCardDisplay : MonoBehaviour
    {
        public TextMeshProUGUI NumberStage;
        public GameObject Stage1GO;
        public GameObject Stage2GO;
        public GameObject Stage3GO;

        public void ChangeEpidemicStage(EpidemicState state)
        {
            switch (state)
            {
                case EpidemicState.Increase:
                    Stage1GO.SetActive(true);
                    Stage2GO.SetActive(false);
                    Stage3GO.SetActive(false);
                    NumberStage.text = "1";
                    break;
                case EpidemicState.Infect:
                    Stage2GO.SetActive(true);
                    NumberStage.text = "2";
                    break;
                case EpidemicState.Intensify:
                    Stage3GO.SetActive(true);
                    NumberStage.text = "3";
                    break;
            }
        }    
    }
}
