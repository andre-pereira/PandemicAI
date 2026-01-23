using DG.Tweening;
using UnityEngine;
using static UnityEngine.Timeline.TimelineAsset;

namespace OPEN.PandemicAI
{
    internal class EIntensify : EngineEvent
    {
        public override void Do(Timeline timeline)
        {
            GameRoot.State.InfectionDiscard.Shuffle(GameRoot.State.infectionRngState);
            GameRoot.State.infectionRngState = Random.state;
            GameRoot.State.InfectionDeck.AddRange(GameRoot.State.InfectionDiscard);
            GameRoot.State.InfectionDiscard.Clear();
        }

        public override float Act()
        {
            GameRoot.CurrentEpidemicObject.GetComponent<EpidemicCardDisplay>().ChangeEpidemicStage(EpidemicState.Intensify);
            DeckDrawer.InfectionDiscard.DestroyChildrenImmediate();
            GameObject backCard = Object.Instantiate(GameGUI.Assets.infectionCardBackPrefab, DeckDrawer.InfectionDiscard.transform.position, DeckDrawer.InfectionDeck.transform.rotation, DeckDrawer.InfectionDiscard.transform);

            Sequence sequence = DOTween.Sequence();
            //backCard.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            sequence.Append(AnimationTemplates.MoveToPosition(backCard, DeckDrawer.InfectionDeck.transform,true,true));
            sequence.Append(GameRoot.CurrentEpidemicObject.transform.DOScale(Vector3.one, 1));
            GameRoot.CurrentEpidemicObject = null;
            return sequence.Play().Duration();
        }
    }
}