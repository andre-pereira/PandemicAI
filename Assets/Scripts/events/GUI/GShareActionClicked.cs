using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OPEN.PandemicAI
{
    internal class GShareActionClicked : GuiEvent
    {
        private Vector3 clickCoordinates;
        private const string CTAKINGCARDFROMME = "TakingCardFromMe";
        private const string CGIVINGMECARD = "GivingMeCard";
        private string condition = "";

        public GShareActionClicked(Vector3 clickCoordinates) : base(GameRoot.State.CurrentPlayer)
        {
            this.clickCoordinates = clickCoordinates;

            if(_player.Hand.Contains(_player.CurrentCity.CityCard.CityID))
            {
                condition = CGIVINGMECARD;
            }
            else
            {
                condition = CTAKINGCARDFROMME;
            }
        }

        public override Dictionary<string, object> GetLogInfo()
        {
            return new Dictionary<string, object>
            {
                { "condition", condition},
                { "cityCard", _player.CurrentCity.CityCard.CityID.ToString() }
            };
        }
    }
}