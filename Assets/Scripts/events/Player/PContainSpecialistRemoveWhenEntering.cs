using DG.Tweening;
using System.Collections.Generic;
using UnityEngine.UI;
using static OPEN.PandemicAI.Enums;

namespace OPEN.PandemicAI
{
    internal class PContainSpecialistRemoveWhenEntering : PlayerEvent
    {
        private readonly City _city;
        private readonly int _redCount;
        private readonly int _yellowCount;
        private readonly int _blueCount;
        private VirusName _cubeRemoved;

        public PContainSpecialistRemoveWhenEntering(City city, int redCount, int yellowCount, int blueCount) : base(GameRoot.State.CurrentPlayer)
        {
            _city = city;
            _redCount = redCount;
            _yellowCount = yellowCount;
            _blueCount = blueCount;
        }

        public override void Do(Timeline timeline)
        {
            var s = GameRoot.State;
            if (_redCount >= 2)
            {
                _city.IncrementNumberOfCubes(VirusName.Red, -1);
                s.AddCubesToBoard(VirusName.Red, 1);
                _cubeRemoved = VirusName.Red;
            }

            if (_yellowCount >= 2)
            {
                _city.IncrementNumberOfCubes(VirusName.Yellow, -1);
                s.AddCubesToBoard(VirusName.Yellow, 1);
                _cubeRemoved = VirusName.Yellow;
            }

            if (_blueCount >= 2)
            {
                _city.IncrementNumberOfCubes(VirusName.Blue, -1);
                s.AddCubesToBoard(VirusName.Blue, 1);
                _cubeRemoved = VirusName.Blue;
            }
        }

        public override float Act()
        {
            Outline outline = _panel.pawnAnimator.roleCard.GetOutlineComponent();
            Sequence sequence = AnimationTemplates.FadeOutline(outline);
            _city.Draw();
            _panel.Draw();
            GUIEvents.RaiseDrawBoard();
            return sequence.Play().Duration();
        }

        public override Dictionary<string, object> GetLogInfo()
        {
            return new Dictionary<string, object>
            {
                { "city", _city.CityCard.CityID },
                { "cubeColor", _cubeRemoved.ToString() }
            };
        }
    }
}