using UnityEngine;
using static OPEN.PandemicAI.Enums;

namespace OPEN.PandemicAI
{
    internal class GFlyActionClicked : GuiEvent
    {
        private Vector3 clickCoordinates;

        public GFlyActionClicked(Vector3 clickCoordinates) : base(GameRoot.State.CurrentPlayer)
        {
            this.clickCoordinates = clickCoordinates;
        }
    }
}