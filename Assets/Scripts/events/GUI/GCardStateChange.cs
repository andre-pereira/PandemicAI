using System.Collections.Generic;

namespace OPEN.PandemicAI
{
    internal class GCardStateChange : GuiEvent
    {
        private Enums.CardGUIStates cardsState;
        private PlayerPanel playerGUI;
        private Player.Roles Role;

        public GCardStateChange(Enums.CardGUIStates cardsState, PlayerPanel playerGUI) : base(playerGUI.Model)
        {
            this.cardsState = cardsState;
            this.playerGUI = playerGUI;

            Role = playerGUI.Model.Role;
        }

        public override Dictionary<string, object> GetLogInfo()
        {
            return new Dictionary<string, object>
                {
                { "cardsState", cardsState.ToString() },
                { "role", Role.ToString()}
            };
        }
    }
}