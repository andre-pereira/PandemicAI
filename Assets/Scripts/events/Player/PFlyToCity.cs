using UnityEngine.UI;
using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

namespace OPEN.PandemicAI
{
    public class PFlyToCity : PlayerEvent
    {
        private readonly int _flyFrom;
        private readonly int _flyTo;
        private readonly Vector3 _originalCardPosition;
        private readonly Quaternion _originalCardRotation;
        private readonly float _animationDuration = 1f / GameRoot.Config.AnimationTimingMultiplier;

        public PFlyToCity(int flyTo) : base(GameRoot.State.CurrentPlayer)
        {
            _flyTo = flyTo;
            _flyFrom = _player.GetCurrentCityId();

            if (!_player.IsAIControlled)
            {
                GameObject cardInHand = _panel.hand.pool[_flyTo];
                if (cardInHand != null) // Check if the card exists.
                {
                    _originalCardPosition = cardInHand.transform.position;
                    _originalCardRotation = cardInHand.transform.rotation;
                }
            }
        }

        public override void Do(Timeline timeline)
        {
            _player.RemoveCardInHand(_flyTo, true);
            _player.UpdateCurrentCity(_flyTo, true);
            _player.DecreaseActionsRemaining(1);
        }

        public override float Act()
        {
            _panel.ClearSelectedAction(true);
            _panel.Draw();

            DG.Tweening.Sequence sequence = DOTween.Sequence();
            GameObject cardToAddObject = DeckDrawer.SpawnPlayerCard(_flyTo, DeckDrawer.PlayerDeckDiscard.transform);
            if (cardToAddObject != null) //Check if the card object was created successfully
            {
                cardToAddObject.transform.position = _originalCardPosition;
                cardToAddObject.transform.rotation = _originalCardRotation;
                sequence = AnimationTemplates.AnimateCardTransition(cardToAddObject, DeckDrawer.PlayerDeckDiscard.transform);
            }

            City currentCity = CityDrawer.CityScripts[_flyFrom];
            City cityToMoveTo = CityDrawer.CityScripts[_flyTo];
            currentCity.RemovePawn(_player);
            currentCity.Draw();
            GameObject movingPawn = Object.Instantiate(GameGUI.Assets.pawnPrefab, currentCity.transform.position, currentCity.transform.rotation, GameGUI.AnimationCanvasTransform.transform);
            if (movingPawn != null)
            {
                movingPawn.GetComponent<Image>().color = _player.GetRoleCard().roleColor;
                movingPawn.GetComponent<Outline>().enabled = true;
                sequence.Join(movingPawn.transform.DOMove(cityToMoveTo.transform.position, _animationDuration).OnComplete(() =>
                {
                    //cityToMoveTo.Draw();
                    GUIEvents.RaiseDrawBoard();
                    Object.Destroy(movingPawn);
                }));
            }
            return sequence.Duration();
        }

        public override Dictionary<string, object> GetLogInfo()
        {
            return new Dictionary<string, object>
            {
                { "fromCity", _flyFrom },
                { "toCity", _flyTo }
            };
        }
    }
}