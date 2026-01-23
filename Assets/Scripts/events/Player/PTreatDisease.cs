using System.Collections.Generic;
using static OPEN.PandemicAI.Enums;

namespace OPEN.PandemicAI
{
    public class PTreatDisease : PlayerEvent
    {
        private readonly City city;
        private readonly VirusName virusName;
        private readonly bool defaultClick;

        public PTreatDisease(int city, VirusName virusName)
            : base(GameRoot.State.CurrentPlayer)
        {
            this.city = CityDrawer.CityScripts[city];
            this.virusName = virusName;
            defaultClick = true;
        }

        public PTreatDisease(City city)
            : base(GameRoot.State.CurrentPlayer)
        {
            this.city = city;
            virusName = city.CityCard.VirusInfo.virusName;
            defaultClick = false;
        }

        public override void Do(Timeline timeline)
        {
            var s = GameRoot.State;
            VirusName? virus = defaultClick ? virusName : city.FirstVirusFoundInCity();

            if (!virus.HasValue)
            {
                _player.DecreaseActionsRemaining(1);
                return;
            }

            if (IsCureFound(virus.Value))
            {
                int cubesInCity = city.GetNumberOfCubes(virus.Value);
                s.AddCubesToBoard(virus.Value, cubesInCity);
                city.ResetCubesOfColor(virus.Value);
            }
            else
            {
                city.IncrementNumberOfCubes(virus.Value, -1);
                s.AddCubesToBoard(virus.Value, 1);
            }
            _player.DecreaseActionsRemaining(1);
        }

        public override float Act()
        {
            city.Draw();
            _panel.Draw();
            GUIEvents.RaiseDrawBoard();
            return 0;
        }

        public override Dictionary<string, object> GetLogInfo()
        {
            return new Dictionary<string, object>
            {
                { "city", city.CityCard.CityID },
                { "cubeColor", virusName.ToString() }
            };
        }

        private bool IsCureFound(VirusName virus)
        {
            return (virus == VirusName.Red && GameRoot.State.RedCureFound)
                || (virus == VirusName.Blue && GameRoot.State.BlueCureFound)
                || (virus == VirusName.Yellow && GameRoot.State.YellowCureFound);
        }
    }
}
