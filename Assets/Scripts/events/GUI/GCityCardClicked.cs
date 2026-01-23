using UnityEngine;
using System.Collections.Generic;

namespace OPEN.PandemicAI
{
    public class GCityCardClicked : GuiEvent
    {
        private const string CDISCARDING = "Discarding";
        private const string CFINDINGCURE = "FindingCure";
        private const string CFLYING = "Flying";
        private const string CLOOKINGATCARDS = "LookingAtCards";

        private readonly CityCard _cardClicked;
        private string condition;

        public GCityCardClicked(int cityCard, Vector3 clickCoordinates): base(GameRoot.State.CurrentPlayer)
        {
            _cardClicked = CityDrawer.CityScripts[cityCard].CityCard;
            if (_panel.CardState == Enums.CardGUIStates.CardsDiscarding)
                condition = CDISCARDING;
            else if (_panel.CardState == Enums.CardGUIStates.CardsExpandedCureActionSelected)
                condition = CFINDINGCURE;
            else if (_panel.CardState == Enums.CardGUIStates.CardsExpandedFlyActionSelected)
                condition = CFLYING;
            else if (_panel.CardState == Enums.CardGUIStates.CardsExpanded)
                condition = CLOOKINGATCARDS;
            else
                condition = CLOOKINGATCARDS;
        }

        public override Dictionary<string, object> GetLogInfo()
        {
            return new Dictionary<string, object>
            {
                { "condition", condition },
                { "cityCard", _cardClicked.CityID }
            };
        }
    }
}