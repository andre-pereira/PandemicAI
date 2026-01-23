using UnityEngine;
using System.Collections.Generic;
using static OPEN.PandemicAI.Enums;

namespace OPEN.PandemicAI
{
    public class GContextButtonClicked : GuiEvent
    {
        private ContextButtonStates buttonType;
        private Vector3 mousePosition;

        public GContextButtonClicked(ContextButtonStates buttonType, Vector3 mousePosition) : base (GameRoot.State.CurrentPlayer)
        {
            this.buttonType = buttonType;
            this.mousePosition = mousePosition;
        }

        public override Dictionary<string, object> GetLogInfo()
        {
            return new Dictionary<string, object>
            {
                { "contextButton", buttonType.ToString() },
                { "mousePosition", mousePosition.ToString() }
            };
        }
    }
}