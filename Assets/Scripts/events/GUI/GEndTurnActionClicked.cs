using UnityEngine;

namespace OPEN.PandemicAI
{
    internal class GEndTurnActionClicked : GuiEvent
    {
        private Vector3 clickCoordinates;

        public GEndTurnActionClicked(Vector3 clickCoordinates) : base(GameRoot.State.CurrentPlayer)
        {
            this.clickCoordinates = clickCoordinates;
        }
    }
}