using DG.Tweening;
using UnityEngine;
using System.Collections.Generic;
using static OPEN.PandemicAI.AnimationTemplates;

namespace OPEN.PandemicAI
{
    public class PDealCard : PlayerEvent
    {
        private int _cardToAdd = -1;
        private bool _epidemicPopped = false;
        private bool possibleToFindCure = false;

        public override void Do(Timeline timeline)
        {
            var s = GameRoot.State;
            if (s.PlayerDeck.Count == 0)
            {
                Timeline.Instance.ClearPendingEvents();
                Timeline.Instance.AddEvent(new EGameOver(Enums.GameOverReasons.NoMorePlayerCards));
                return;
            }

            _cardToAdd = s.PlayerDeck.Pop();

            if (_cardToAdd == GameRoot.Catalog.EpidemicCardIndex)
            {
                Timeline.Instance.AddEvent(new EEpidemicInitiate());
                _epidemicPopped = true;
            }
            else
            {
                _player.AddCardToHand(_cardToAdd);
            }

            if(s.CurrentState != GameState.Discarding)
                s.ActionCompleted = true;

            if (_player.BlueCardsInHand.Count == 4 || _player.YellowCardsInHand.Count == 4 || _player.RedCardsInHand.Count == 4)
            {
                possibleToFindCure = true;
            }
        }

        public override float Act()
        {
            if (_cardToAdd == -1 || _epidemicPopped)
            {
                return 0f;
            }

            GameObject card = DeckDrawer.SpawnPlayerCard(
                _cardToAdd,
                GameGUI.AnimationCanvasTransform.transform,
                DeckDrawer.PlayerDeck.transform);

            if (card != null) // Check if the card was instantiated successfully
            {
                GUIEvents.RaiseDrawBoard();

                GameObject lastCard = _panel.hand.GetLastActiveCard();

                // Create an animation sequence that highlights the card and moves it.
                Sequence sequence = HighlightCardAndMove(card, lastCard == null ?  _panel.pawnAnimator.roleCard.transform : lastCard.transform);
                sequence.onComplete += () =>
                {
                    Object.Destroy(card);
                    _panel.Draw();
                };
                sequence.Play();
                return sequence.Duration();
            }
            else
            {
                Debug.LogError($"Card with ID {_cardToAdd} could not be instantiated.");
                return 0f;
            }
        }

        public override Dictionary<string, object> GetLogInfo()
        {
            if (_cardToAdd == -1)
            {
                return new Dictionary<string, object>
                {
                    { "condition", "noCard" },
                    { "cityCard", null },
                    { "cityColor", null },
                    { "abort", false }
                };
            }

            return new Dictionary<string, object>
            {
                { "condition" , possibleToFindCure? "possibleToFindCure" : "regular" },
                { "cityCard", _cardToAdd },
                { "cityColor" , _epidemicPopped ? null : CityDrawer.CityScripts[_cardToAdd].CityCard.VirusInfo.virusName.ToString()},
                { "abort" , _epidemicPopped }
            };
        }
    }
}