using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

namespace OPEN.PandemicAI
{
    public class EIncreaseInfectionRate : EngineEvent
    {
        public override void Do(Timeline timeline)
        {
            GameRoot.State.InfectionRateIndex++;
        }

        public override float Act()
        {
            GameObject moveFrom = MarkerDrawer.GetInfectionRateMarker(GameRoot.State.InfectionRateIndex - 1);
            GameObject moveTo = MarkerDrawer.GetInfectionRateMarker(GameRoot.State.InfectionRateIndex);

            moveFrom.DestroyChildrenImmediate();
            GameObject marker = Object.Instantiate(GameGUI.Assets.infectionRateMarkerPrefab, moveFrom.transform.position, moveFrom.transform.rotation, GameGUI.AnimationCanvasTransform.transform);
            Sequence sequence = AnimationTemplates.MoveToPosition(marker, moveTo.transform, true, true);
            return sequence.Play().Duration();
        }

        public override Dictionary<string, object> GetLogInfo()
        {
            return new Dictionary<string, object>
            {
                { "condition", GameRoot.State.InfectionRateIndex == 1? "noChange" : "increased" },
                { "infectionRate" , GameRoot.State.InfectionRateIndex == 1? "2" : GameRoot.State.InfectionRateIndex == 2? "3" : "4"}
            };
        }
    }
}