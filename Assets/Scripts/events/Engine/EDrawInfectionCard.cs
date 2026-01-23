using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static OPEN.PandemicAI.Enums;

namespace OPEN.PandemicAI
{
    internal class EDrawInfectionCard : EngineEvent
    {
        private readonly int _numberOfCubes;
        private int cityID;
        private CityCard cityCard = null;
        private readonly bool _fromTheTop;
        private Player _quarantineSpecialist = null;
        private bool _gameOver = false;

        public EDrawInfectionCard(int numberOfCubes, bool fromTheTop)
        {
            _fromTheTop = fromTheTop;
            _numberOfCubes = numberOfCubes;
        }

        public override void Do(Timeline tl)
        {
            var s = GameRoot.State;
            var catalog = GameRoot.Catalog;

            if(_numberOfCubes == 1)
                s.InfectionCardsDrawn++; //used for counting the number of infection cards drawn in a turn

            if (_fromTheTop) cityID = s.InfectionDeck.Pop();
            else
            {
                cityID = s.InfectionDeck[0];
                s.InfectionDeck.Remove(cityID);
            }
            s.InfectionDiscard.Add(cityID);

            // If the infection deck is empty, recycle the discard pile.
            if (s.InfectionDeck.Count == 0)
            {
                s.InfectionDeck.AddRange(s.InfectionDiscard);
                s.InfectionDiscard.Clear();
                s.InfectionDeck.Shuffle(s.infectionRngState);
                s.infectionRngState = Random.state;
            }

            cityCard = CityDrawer.CityScripts[cityID].CityCard;

            bool preventedByQuarantine = IsPreventedByQuarantine(s, cityID);

            if (!preventedByQuarantine)
            {
                if (!s.TryTakeCubes(cityCard.VirusInfo.virusName, _numberOfCubes))
                {
                    tl.AddEvent(new EGameOver(GameOverReasons.NoMoreCubesOfAColor));
                    _gameOver = true;
                    return;
                }

                bool outbreak = CityDrawer.AddCubes(cityCard.CityID, cityCard.VirusInfo.virusName, _numberOfCubes);

                if (outbreak && !s.OutbreakTracker.Contains(cityCard.CityID))
                {
                    tl.AddEvent(new EOutbreak(cityCard.CityID));
                    return;
                }
            }
            else
                tl.AddEvent(new PQuarantineSpecialistPrevention(_quarantineSpecialist, cityCard.CityID));

            s.ActionCompleted = true;
        }

        public override float Act()
        {
            if (_numberOfCubes == 3) 
                GameRoot.CurrentEpidemicObject.GetComponent<EpidemicCardDisplay>().ChangeEpidemicStage(EpidemicState.Infect);

            GameObject cardToAddObject = Object.Instantiate(
                GameGUI.Assets.infectionCardPrefab,
                DeckDrawer.InfectionDeck.transform.position,
                DeckDrawer.PlayerDeck.transform.rotation,
                DeckDrawer.InfectionDiscard.transform);

            cardToAddObject.GetComponent<InfectionCardDisplay>().CityCardData = cityCard;
            Sequence sequence = AnimationTemplates.HighlightCardAndMove(cardToAddObject, DeckDrawer.InfectionDiscard.transform, false, false);

            if (!_gameOver && _quarantineSpecialist == null)
            {
                for (int i = 0; i < _numberOfCubes; i++)
                {
                    GameObject cubeToDuplicate = CubeDrawer.GetCubeFromPool(cityCard.VirusInfo, i);
                    GameObject cube = Object.Instantiate(cubeToDuplicate, GameGUI.AnimationCanvasTransform.transform, cubeToDuplicate.transform);
                    cubeToDuplicate.SetActive(false);
                    sequence.Join(AnimationTemplates.MoveToPosition(cube, CityDrawer.CityScripts[cityCard.CityID].CubesGameObject.transform, true, true));
                }
            }

            sequence.Play().OnComplete(() =>
            {
                GUIEvents.RaiseDrawBoard();
                GUIEvents.RaiseDrawCity(cityCard.CityID);
            });
            return sequence.Duration();
        }


        public override Dictionary<string, object> GetLogInfo()
        {
            return new Dictionary<string, object>
            {
                { "condition", _numberOfCubes == 3 ? "epidemic" : "regular"},
                { "city", cityCard != null ? cityCard.CityID : (int?)null },
                { "color", cityCard.VirusInfo.virusName.ToString()},
                { "nCubes", _numberOfCubes },
                { "abort", _gameOver }
            };
        }

        private bool IsPreventedByQuarantine(GameStateData s, int targetCity)
        {
            if (s.CurrentState == GameState.Epidemic) return false;

            foreach (var p in s.Players)
            {
                if (p.Role != Player.Roles.QuarantineSpecialist) continue;
                
                int pc = p.GetCurrentCityId();
                if (pc == targetCity || CityDrawer.CityScripts[pc].CityCard.Neighbors.Contains(targetCity))
                {
                    _quarantineSpecialist = p;
                    return true;
                }
            }
            return false;
        }
    }
}