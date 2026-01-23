using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace OPEN.PandemicAI
{
    internal class EOutbreak : EngineEvent
    {
        private readonly CityCard _originOfOutbreak;
        private readonly List<int> _infectCities = new List<int>();
        private List<int> _quarantineSpecialistExceptions;
        private bool quarantinePrevented = false;

        public EOutbreak(int origin)
        {
            _originOfOutbreak = CityDrawer.CityScripts[origin].CityCard;
        }

        public override void Do(Timeline timeline)
        {
            GameEvents.RequestStateChange(GameState.Outbreak);

            //add the GameRoot.State.Players[1].CurrentCity.CityCard.Neighbors int[] to the _quarantineSpecialistExceptions list
            _quarantineSpecialistExceptions = GameRoot.State.Players[1].CurrentCity.CityCard.Neighbors.ToList();

            bool recurrentOutbreak = false;

            GameRoot.State.OutbreakTracker.Add(_originOfOutbreak.CityID);
            Timeline.Instance.AddEvent(new EIncreaseOutbreak());

            foreach (int neighbor in _originOfOutbreak.Neighbors)
            {
                City neighborCity = CityDrawer.CityScripts[neighbor];

                if (_quarantineSpecialistExceptions.Contains(neighborCity.CityCard.CityID))
                {
                    quarantinePrevented = true;
                    continue;
                }

                if (!GameRoot.State.OutbreakTracker.Contains(neighborCity.CityCard.CityID))
                {
                    _infectCities.Add(neighbor);
                    if (neighborCity.IncrementNumberOfCubes(_originOfOutbreak.VirusInfo.virusName, 1)) // True when Outbreak happens 
                    {
                        Timeline.Instance.AddEvent(new EOutbreak(neighborCity.CityCard.CityID));
                        recurrentOutbreak = true;
                    }
                  }
            }

            if (!recurrentOutbreak)
                GameRoot.State.ActionCompleted = true;
        }

        public override float Act()
        {
            Sequence sequence = DOTween.Sequence();

            if (_infectCities.Any())
            {
                List<GameObject> listCubes = new List<GameObject>();

                foreach (int neighbor in _infectCities)
                {
                    Transform originOutbreakTransform = CityDrawer.CitiesGO[_originOfOutbreak.CityID].transform;
                    GameObject cube = Object.Instantiate(GameGUI.Assets.cubePrefab, originOutbreakTransform.position, originOutbreakTransform.rotation, GameGUI.AnimationCanvasTransform.transform);
                    cube.transform.localScale = new Vector3(0.096f, 0.096f, 0.096f);
                    cube.GetComponent<Cube>().VirusInfo = _originOfOutbreak.VirusInfo;
                    listCubes.Add(cube);
                    sequence.Join(AnimationTemplates.MoveToPosition(cube, CityDrawer.CitiesGO[neighbor].transform, true, false));
                }

                if (quarantinePrevented)
                {
                    Outline outline = GameRoot.State.Players[1].Panel.pawnAnimator.roleCard.GetOutlineComponent();
                    sequence.Join(AnimationTemplates.FadeOutline(outline));
                }

                sequence.AppendCallback(() =>
                {
                    foreach (int neighbor in _originOfOutbreak.Neighbors)
                    {
                        City neighborCity = CityDrawer.CityScripts[neighbor];
                        neighborCity.Draw();
                    }
                });
            }

            return sequence.Play().Duration();
        }

        public override Dictionary<string, object> GetLogInfo()
        {
            return new Dictionary<string, object>
            {
                { "city", _originOfOutbreak.CityID },
                { "infectCities", _infectCities },
                { "quarantineSpecialistException", _quarantineSpecialistExceptions }
            };
        }
    }
}