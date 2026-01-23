using UnityEngine;
using static OPEN.PandemicAI.Enums;

namespace OPEN.PandemicAI
{
    internal class GCharterActionClicked : GuiEvent
    {
        private Vector3 clickCoordinates;

        public GCharterActionClicked(Vector3 clickCoordinates): base(GameRoot.State.CurrentPlayer)
        {
            this.clickCoordinates = clickCoordinates;
        }

    }
}