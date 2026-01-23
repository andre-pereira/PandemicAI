using UnityEngine;
using UnityEngine.UI;
using static OPEN.PandemicAI.Enums;

namespace OPEN.PandemicAI
{

    public class MarkerDrawer : MonoBehaviour
    {
        [SerializeField] public Transform[] outbreakMarkerTransforms;
        [SerializeField] public Transform[] infectionRateMarkerTransforms;
        [SerializeField] public GameObject[] vialTokens;
        [SerializeField] public Transform[] vialTokensTransforms;

        public static Transform[] OutbreakMarkerTransforms { get; private set; }
        public static Transform[] InfectionRateMarkerTransforms { get; private set; }
        public static GameObject[] VialTokens { get; private set; }
        public static Transform[] VialTokensTransforms { get; private set; }

        public void Awake()
        {
            OutbreakMarkerTransforms = outbreakMarkerTransforms;
            InfectionRateMarkerTransforms = infectionRateMarkerTransforms;
            VialTokens = vialTokens;
            VialTokensTransforms = vialTokensTransforms;
        }

        public void OnEnable()
        {
            GUIEvents.DrawMarkers += Draw;
        }

        public void Draw()
        {
            var s = GameRoot.State;
            foreach (Transform item in MarkerDrawer.OutbreakMarkerTransforms)
            {
                item.gameObject.DestroyChildrenImmediate();
            }

            foreach (Transform item in MarkerDrawer.InfectionRateMarkerTransforms)
            {
                item.gameObject.DestroyChildrenImmediate();
            }

            Instantiate(GameGUI.Assets.infectionRateMarkerPrefab, infectionRateMarkerTransforms[s.InfectionRateIndex].position, infectionRateMarkerTransforms[s.InfectionRateIndex].rotation, infectionRateMarkerTransforms[s.InfectionRateIndex]);
            Instantiate(GameGUI.Assets.outbreakMarkerPrefab, outbreakMarkerTransforms[s.OutbreakCounterIndex].position, outbreakMarkerTransforms[s.OutbreakCounterIndex].rotation, outbreakMarkerTransforms[s.OutbreakCounterIndex]);

            drawCureVialsOnBoard();
        }

        private void drawCureVialsOnBoard()
        {
            var s = GameRoot.State;
            for (int i = 0; i < vialTokensTransforms.Length; i++)
            {
                vialTokensTransforms[i].gameObject.DestroyChildrenImmediate();
                if ((i == (int)VirusName.Red && s.RedCureFound) ||
                    (i == (int)VirusName.Yellow && s.YellowCureFound) ||
                    (i == (int)VirusName.Blue && s.BlueCureFound))
                {
                    GameObject vial = Instantiate(GameGUI.Assets.cureVialPrefab, vialTokensTransforms[i].position, VialTokensTransforms[i].rotation, vialTokensTransforms[i]);
                    vial.GetComponent<Image>().color = GameRoot.Catalog.VirusInfos[i].virusColor;
                }
            }
        }

        public static GameObject GetInfectionRateMarker(int targetInfectionRate) => InfectionRateMarkerTransforms[targetInfectionRate].gameObject;
        public static GameObject GetOutbreakMarker(int targetOutbreak) => OutbreakMarkerTransforms[targetOutbreak].gameObject;
    }
}