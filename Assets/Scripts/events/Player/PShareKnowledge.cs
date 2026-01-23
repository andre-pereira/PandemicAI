using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using static OPEN.PandemicAI.Enums;
using System.Linq;

namespace OPEN.PandemicAI
{
    public class PShareKnowledge : PlayerEvent
    {
        private const string CTAKINGCARDFROMME = "TakingCardFromMe";
        private const string CGIVINGMECARD = "GivingMeCard";
        private string condition = "";

        private readonly Player _playerFrom;
        private readonly Player _playerTo;
        private readonly bool withoutConfirmation;

        private readonly float _animationDuration = 1f / GameRoot.Config.AnimationTimingMultiplier;


        private readonly int _cityID;
        private readonly Vector3 _initialPosition;
        private readonly Quaternion _initialRotation;
        private readonly CityCard _cardData;

        public PShareKnowledge(Player playerFrom, Player playerTo, bool withoutConfirmation)
            : base(GameRoot.State.CurrentPlayer)
        {
            _playerFrom = playerFrom;
            _playerTo = playerTo;
            this.withoutConfirmation = withoutConfirmation;
            _cityID = _playerFrom.GetCurrentCityId();

            // Retrieve the card display from the source player's hand.
            if (!withoutConfirmation)
            {
                CityCardDisplay cityCardDisplay = _playerFrom.Panel.hand.pool[_cityID].GetComponent<CityCardDisplay>();
                _initialPosition = cityCardDisplay.transform.position;
                _initialRotation = cityCardDisplay.transform.rotation;
                _cardData = cityCardDisplay.CityCardData;
            }
            if (_player.Hand.Contains(_player.CurrentCity.CityCard.CityID))
            {
                condition = CGIVINGMECARD;
            }
            else
            {
                condition = CTAKINGCARDFROMME;
            }
        }

        public override void Do(Timeline timeline)
        {
            _player.DecreaseActionsRemaining(1);
            _playerFrom.RemoveCardInHand(_cityID);
            _playerTo.AddCardToHand(_cityID);
            if (!withoutConfirmation)
            {
                _playerTo.Panel.ClearSelectedAction(false);
                _playerFrom.Panel.ClearSelectedAction(false);
            }
        }

        public override float Act()
        {
            if (withoutConfirmation)
                return 0f;

            // Instantiate a copy of the city card to animate.
            GameObject cityCardCopy = Object.Instantiate(
                GameGUI.Assets.cityCardPrefab,
                _initialPosition,
                _initialRotation,
                GameGUI.AnimationCanvasTransform.transform);

            CityCardDisplay cityCardCopyDisplay = cityCardCopy.GetComponent<CityCardDisplay>();
            cityCardCopyDisplay.CityCardData = _cardData;

            // Determine the target for the animation.
            GameObject target = GetAnimationTarget();

            // Create the animation sequence.
            Sequence sequence = DOTween.Sequence();
            sequence.Append(cityCardCopy.transform.DOMove(target.transform.position, _animationDuration));
            sequence.Join(cityCardCopy.transform.DORotate(target.transform.rotation.eulerAngles, _animationDuration));
            sequence.AppendCallback(() =>
            {
                Object.Destroy(cityCardCopy);
                _playerTo.Panel.ClearSelectedAction(false);
                _playerTo.Panel.Draw();
            });

            _playerFrom.Panel.ClearSelectedAction(false);
            _playerFrom.Panel.Draw();
            
            return sequence.Duration();
        }

        public override Dictionary<string, object> GetLogInfo()
        {
            return new Dictionary<string, object>
            {
                { "condition", condition},
                { "fromPlayer", _playerFrom != null ? _playerFrom.Role : null },
                { "toPlayer", _playerTo != null ? _playerTo.Role : null },
                { "cityCard", _cityID },
                { "cityColor" , CityDrawer.CityScripts[_cityID].CityCard.VirusInfo.virusName.ToString()}
            };
        }

        private GameObject GetAnimationTarget()
        {
            // Use the first card in hand if available; otherwise, default to the player's card container.
            GameObject target = _playerTo.Panel.hand.GetLastActiveCard();
            return target ?? _playerTo.Panel.hand.container.gameObject;
        }
    }
}
