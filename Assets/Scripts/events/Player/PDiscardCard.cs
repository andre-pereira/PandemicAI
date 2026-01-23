using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

namespace OPEN.PandemicAI
{
    internal class PDiscardCard : PlayerEvent
    {
        private readonly int _cardToDiscard;
        private readonly Player _playerToDiscard;
        private readonly bool withAnimation;
        private Vector3 _objectToDiscardPosition;
        private Quaternion _objectToDiscardRotation;

        public PDiscardCard(int cardID, Player player, bool withAnimation)
        {
            _cardToDiscard = cardID;
            _playerToDiscard = player;
            this.withAnimation = withAnimation;
            if (withAnimation)
            {
                GameObject _objectToDiscard = _playerToDiscard.Panel.hand.pool[_cardToDiscard];
                _objectToDiscardPosition = _objectToDiscard.transform.position;
                _objectToDiscardRotation = _objectToDiscard.transform.rotation;
            }
        }
      
        public override void Do(Timeline timeline)
        {
            _playerToDiscard.RemoveCardInHand(_cardToDiscard, true);
            GameRoot.State.ActionCompleted = true;
            //Debug.Log($"Discarding card {_cardToDiscard} from player {_playerToDiscard.Name}");
        }

        public override float Act()
        {
            if (!withAnimation) return 0f;

            _playerToDiscard.Panel.Draw();
            Sequence sequence = DOTween.Sequence();

            GameObject cardToAddObject = Object.Instantiate(GameGUI.Assets.cityCardPrefab, _objectToDiscardPosition, _objectToDiscardRotation, DeckDrawer.PlayerDeckDiscard.transform);
            cardToAddObject.GetComponent<CityCardDisplay>().CityCardData = CityDrawer.CityScripts[_cardToDiscard].CityCard;
            sequence.Append(AnimationTemplates.MoveToPosition(cardToAddObject, DeckDrawer.PlayerDeckDiscard.transform));
            return sequence.Play().Duration();
        }

        public override Dictionary<string, object> GetLogInfo()
        {
            return new Dictionary<string, object>
            {
                { "cityCard", _cardToDiscard },
                { "player", _playerToDiscard != null ? _playerToDiscard.Name : null }
            };
        }
    }
}