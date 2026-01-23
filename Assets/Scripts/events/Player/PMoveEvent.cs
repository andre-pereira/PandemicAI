using System.Collections.Generic;

namespace OPEN.PandemicAI
{
    internal class PMoveEvent : PlayerEvent
    {
        private readonly int _newCityID;
        private readonly int _oldCityID;
        private readonly int _numberOfActionsSpent;

        public PMoveEvent(int cityID, int numberOfActionsSpent) : base(GameRoot.State.CurrentPlayer)
        {
            _oldCityID = _player.GetCurrentCityId();
            _newCityID = cityID;
            _numberOfActionsSpent = numberOfActionsSpent;
        }

        public override void Do(Timeline timeline)
        {
            _player.UpdateCurrentCity(_newCityID, true);
            _player.DecreaseActionsRemaining(_numberOfActionsSpent);
        }

        public override float Act()
        {
            GUIEvents.RaiseDrawCities();
            _panel.ClearSelectedAction(false);
            GUIEvents.RaiseDrawPlayerArea(_panel);
            return 0;
        }

        /// <summary>
        /// Generates a log information dictionary for the move action.
        /// </summary>
        /// <returns>A dictionary containing information about the action.</returns>
        public override Dictionary<string, object> GetLogInfo()
        {
            return new Dictionary<string, object>
            {
                { "toCity", _newCityID },
                { "fromCity", _oldCityID },
                { "nActions", _numberOfActionsSpent.ToString() }
            };
        }
    }
}