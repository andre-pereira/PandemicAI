using System.Collections.Generic;

namespace OPEN.PandemicAI
{
    public class GCityClicked : GuiEvent
    {
        private const string CTREAT = "Treat";
        private const string CNOTDOINGANYTHING = "NotDoingAnything";
        private readonly City _selectedCity;
        private string condition;

        public GCityClicked(City city) : base(GameRoot.State.CurrentPlayer)
        {
            _selectedCity = city;
            condition = _panel.ActionSelected == Enums.ActionType.Treat ? CTREAT : CNOTDOINGANYTHING;
        }

        public override Dictionary<string, object> GetLogInfo()
        {
            return new Dictionary<string, object>
            {
                { "condition", condition },
                { "city", _selectedCity.CityCard.CityID },
                { "cityColor", _selectedCity.CityCard.VirusInfo.virusName.ToString() }
            };
        }
    }
}