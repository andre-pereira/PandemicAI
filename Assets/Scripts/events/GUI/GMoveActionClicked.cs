using UnityEngine;
using static OPEN.PandemicAI.Enums;

namespace OPEN.PandemicAI
{
    internal class GMoveActionClicked : GuiEvent
    {
        private Vector3 clickCoordinates;

        public GMoveActionClicked(Vector3 clickCoordinates) :base (GameRoot.State.CurrentPlayer)
        {
            this.clickCoordinates = clickCoordinates;
        }
    }
}