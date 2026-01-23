using DG.Tweening;
using UnityEngine;
using System.Collections.Generic;
using static OPEN.PandemicAI.Enums;

namespace OPEN.PandemicAI
{
    internal class PCharterEvent : PlayerEvent
    {
        private readonly CityCard _flyTo;
        private readonly CityCard _flyFrom;
        private readonly Vector3 _originalCardPosition;
        private readonly Quaternion _originalCardRotation;

        public PCharterEvent(int flyTo) : base(GameRoot.State.CurrentPlayer)
        {
            _flyTo = CityDrawer.CityScripts[flyTo].CityCard;
            _flyFrom = CityDrawer.CityScripts[_player.GetCurrentCityId()].CityCard;

            if (!_player.IsAIControlled)
            {
                GameObject cardInHand = _panel.hand.pool[_flyFrom.CityID];
                if (!GameRoot.Config.SimulationMode)
                {
                    _originalCardPosition = cardInHand.transform.position;
                    _originalCardRotation = cardInHand.transform.rotation;
                }
            }
        }

        public override void Do(Timeline timeline)
        {
            _player.RemoveCardInHand(_flyFrom.CityID, true);
            _player.UpdateCurrentCity(_flyTo.CityID, true);
            _player.DecreaseActionsRemaining(1);
        }

        public override float Act()
        {
            _panel.ClearSelectedAction(true);
            GUIEvents.RaiseDrawPlayerArea(_panel);
            GUIEvents.RaiseDrawCity(_flyTo.CityID);

            Sequence sequence = DOTween.Sequence();
            GameObject cardToAddObject = DeckDrawer.SpawnPlayerCard(_flyFrom.CityID, DeckDrawer.PlayerDeckDiscard.transform);
            cardToAddObject.transform.position = _originalCardPosition;
            cardToAddObject.transform.rotation = _originalCardRotation;
            sequence = AnimationTemplates.AnimateCardTransition(cardToAddObject, DeckDrawer.PlayerDeckDiscard.transform);
            sequence.AppendCallback(() =>
            {
                GUIEvents.RaiseDrawBoard();
            });

            return sequence.Duration();
        }

        public override Dictionary<string, object> GetLogInfo()
        {
            return new Dictionary<string, object>
            {
                { "fromCity", _flyFrom.CityID },
                { "toCity", _flyTo.CityID }
            };
        }
    }
}