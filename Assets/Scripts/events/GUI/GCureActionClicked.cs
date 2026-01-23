using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

namespace OPEN.PandemicAI
{
    internal class GCureActionClicked : GuiEvent
    {
        private Vector3 clickCoordinates;
        private string color;

        public GCureActionClicked(Vector3 clickCoordinates) : base(GameRoot.State.CurrentPlayer)
        {
            this.clickCoordinates = clickCoordinates;

            color = GameRoot.State.CurrentPlayer.BlueCardsInHand.Count > 3 ? "blue" :
            GameRoot.State.CurrentPlayer.YellowCardsInHand.Count > 3 ? "yellow" : "red";
        }

        public override Dictionary<string, object> GetLogInfo()
        {
            return new Dictionary<string, object>
            {
                { "cureColor", color }
            };
        }
    }
}