using System.Collections.Generic;
using static OPEN.PandemicAI.Enums;

namespace OPEN.PandemicAI
{
    public class GActionButtonClicked : GuiEvent
    {
        private readonly ActionType _actionSelected;

        public GActionButtonClicked(ActionType actionSelected) : base(GameRoot.State.CurrentPlayer)
        {
            _actionSelected = actionSelected;
        }

        public override Dictionary<string, object> GetLogInfo()
        {
            return new Dictionary<string, object>
            {
                { "actionSelected", _actionSelected.ToString() }
            };
        }
    }
}