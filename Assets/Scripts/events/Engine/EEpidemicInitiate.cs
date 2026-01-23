using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

namespace OPEN.PandemicAI
{

    public class EEpidemicInitiate : EngineEvent
    {
        private int epidemicCardDrawCount;

        public EEpidemicInitiate()
        {
            epidemicCardDrawCount = GameRoot.State.InfectionRateIndex + 1;
        }

        public override void Do(Timeline timeline)
        {
            GameEvents.RequestStateChange(GameState.Epidemic);
        }

        public override float Act()
        {
            // Instantiate the epidemic card.
            GameRoot.CurrentEpidemicObject = Object.Instantiate(GameGUI.Assets.epidemicCardPrefab, DeckDrawer.PlayerDeck.transform.position, DeckDrawer.PlayerDeck.transform.rotation, DeckDrawer.PlayerDeckDiscard.transform);
            GameRoot.CurrentEpidemicObject.GetComponent<EpidemicCardDisplay>().ChangeEpidemicStage(EpidemicState.Increase);
            GUIEvents.RaiseDrawBoard();
            Sequence sequence = AnimationTemplates.HighlightCardAndMove(GameRoot.CurrentEpidemicObject, DeckDrawer.PlayerDeckDiscard.transform,true, true, false);
            return sequence.Play().Duration();
        }

        public override Dictionary<string, object> GetLogInfo()
        {
            return new Dictionary<string, object>
            {
                { "condition", epidemicCardDrawCount == 1? "first" : epidemicCardDrawCount == 2? "second" : "third" }
            };
        }

    }
}