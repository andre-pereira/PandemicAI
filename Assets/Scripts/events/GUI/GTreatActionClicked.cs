using System.Collections.Generic;
using UnityEngine;

namespace OPEN.PandemicAI
{
    public class GTreatActionClicked : GuiEvent
    {
        private Vector3 clickCoordinates;

        public GTreatActionClicked(Vector3 clickCoordinates) : base(GameRoot.State.CurrentPlayer)
        {
            this.clickCoordinates = clickCoordinates;
        }

        public override Dictionary<string, object> GetLogInfo()
        {
            var logInfo = new Dictionary<string, object>
            {
                { "city", _player.CurrentCity.CityCard.CityID}
            };
            return logInfo;
        }
    }
}