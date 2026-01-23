using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using static OPEN.PandemicAI.Enums;

namespace OPEN.PandemicAI
{
    public class EIncreaseOutbreak : EngineEvent
    {
        public override void Do(Timeline timeline)
        {
            GameRoot.State.OutbreakCounterIndex++;
            if (GameRoot.State.OutbreakCounterIndex >= 4)
            {
                Timeline.Instance.ClearPendingEvents();
                timeline.AddEvent(new EGameOver(GameOverReasons.TooManyOutbreaks));
            }
        }

        public override float Act()
        {
            Transform moveFrom = MarkerDrawer.OutbreakMarkerTransforms[GameRoot.State.OutbreakCounterIndex - 1];
            Transform moveTo = MarkerDrawer.OutbreakMarkerTransforms[GameRoot.State.OutbreakCounterIndex];
            moveFrom.gameObject.DestroyChildrenImmediate();
            GameObject marker = Object.Instantiate(GameGUI.Assets.outbreakMarkerPrefab, moveFrom.position, moveFrom.rotation, GameGUI.AnimationCanvasTransform.transform);
            Sequence sequence = AnimationTemplates.MoveToPosition(marker, moveTo.transform, true, true);
            return sequence.Play().Duration();
        }

        public override Dictionary<string, object> GetLogInfo()
        {
            return new Dictionary<string, object>
            {
                { "nOutbreaksLeft", GameRoot.State.OutbreakCounterIndex == 1? "2" : GameRoot.State.OutbreakCounterIndex == 2? "1" : "no"},
                { "nOutbreaks", GameRoot.State.OutbreakCounterIndex == 1? "1" : GameRoot.State.OutbreakCounterIndex == 2? "2" : "3"},
                { "abort" , GameRoot.State.OutbreakCounterIndex >= 4 }
            };
        }
    }
}