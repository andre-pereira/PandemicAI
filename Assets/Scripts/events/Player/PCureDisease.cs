using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using static OPEN.PandemicAI.Enums;

namespace OPEN.PandemicAI
{
    internal class PCureDisease : PlayerEvent
    {
        private readonly VirusName _virusName;
        private readonly List<int> _selectedCards;
        private readonly Vector3[] _originalCardPositions;
        private readonly Quaternion[] _originalCardRotations;
        private readonly float _animationDuration = 5f / GameRoot.Config.AnimationTimingMultiplier;

        public PCureDisease(List<int> selectedCards) : base(GameRoot.State.CurrentPlayer)
        {
            _selectedCards = new List<int>(selectedCards);
            _originalCardPositions = new Vector3[selectedCards.Count];
            _originalCardRotations = new Quaternion[selectedCards.Count];

            int numRed = 0;
            int numYellow = 0;
            int numBlue = 0;

            if (!_player.IsAIControlled)
            {

                for (int i = 0; i < selectedCards.Count; i++)
                {
                    GameObject cardInHand = _panel.hand.pool[selectedCards[i]];
                    if (cardInHand != null) // Check if the card exists.
                    {
                        _originalCardPositions[i] = cardInHand.transform.position;
                        _originalCardRotations[i] = cardInHand.transform.rotation;
                        switch (CityDrawer.CityScripts[selectedCards[i]].CityCard.VirusInfo.virusName)
                        {
                            case VirusName.Blue:
                                numBlue++;
                                if (numBlue > 2)
                                    _virusName = VirusName.Blue;
                                break;
                            case VirusName.Red:
                                numRed++;
                                if (numRed > 2)
                                    _virusName = VirusName.Red;
                                break;
                            case VirusName.Yellow:
                                numYellow++;
                                if (numYellow > 2)
                                    _virusName = VirusName.Yellow;
                                break;
                        }
                    }
                }
            }
            else _virusName = CityDrawer.CityScripts[selectedCards[0]].CityCard.VirusInfo.virusName;
        }

        public override void Do(Timeline timeline)
        {
            for (int i = 0; i < _selectedCards.Count; i++)
            {
                _player.RemoveCardInHand(_selectedCards[i], true);
            }

            switch (_virusName)
            {
                case VirusName.Blue:
                    GameRoot.State.BlueCureFound = true;
                    break;
                case VirusName.Red:
                    GameRoot.State.RedCureFound = true;
                    break;
                case VirusName.Yellow:
                    GameRoot.State.YellowCureFound = true;
                    break;
            }

            _player.DecreaseActionsRemaining(1);

            // Check if all diseases are cured and if so, end the game
            if (GameRoot.State.BlueCureFound && GameRoot.State.RedCureFound && GameRoot.State.YellowCureFound)
            {
                Timeline.Instance.ClearPendingEvents();
                timeline.AddEvent(new EGameOver(GameOverReasons.PlayersWon));
            }
        }

        public override float Act()
        {
            Sequence sequence = DOTween.Sequence();
            _panel.Draw();

            for (int i = 0; i < _selectedCards.Count; i++)
            {
                GameObject cardToAddObject = DeckDrawer.SpawnPlayerCard(_selectedCards[i], DeckDrawer.PlayerDeckDiscard.transform);
                cardToAddObject.transform.position = _originalCardPositions[i];
                cardToAddObject.transform.rotation = _originalCardRotations[i];
                sequence = AnimationTemplates.AnimateCardTransition(cardToAddObject, DeckDrawer.PlayerDeckDiscard.transform);
            }

            sequence.Append(MarkerDrawer.VialTokens[(int)_virusName].transform.DOMove(MarkerDrawer.VialTokensTransforms[(int)_virusName].transform.position, _animationDuration)
                .OnComplete(() =>
            {
                Object.Destroy(MarkerDrawer.VialTokens[(int)_virusName]);
                GUIEvents.RaiseDrawBoard();
            })
                .OnUpdate(() =>
                {
                    if (Furhat.Instance != null)
                        Furhat.Instance.NotifyObjectOnScreenMoved(MarkerDrawer.VialTokens[(int)_virusName].transform);
                })
                );

            _panel.ClearSelectedAction(false);
            _panel.Draw();
            return sequence.Duration();
        }

        public override Dictionary<string, object> GetLogInfo()
        {
            return new Dictionary<string, object>
            {
                { "cureColor", _virusName.ToString() }
            };
        }
    }
}