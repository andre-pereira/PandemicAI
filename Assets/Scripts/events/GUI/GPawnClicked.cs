using System.Collections.Generic;

namespace OPEN.PandemicAI
{
    /// <summary>
    /// Represents a GUI event triggered when a player pawn is clicked.
    /// </summary>
    public class GPawnClicked : GuiEvent
    {
        private readonly Pawn _playerSelected;

        public GPawnClicked(Pawn pawn) : base(GameRoot.State.CurrentPlayer)
        {
            _playerSelected = pawn;
        }

        public override Dictionary<string, object> GetLogInfo()
        {
            return new Dictionary<string, object>
            {
                { "role", _playerSelected.PlayerModel.Role.ToString() }
            };
        }
    }
}