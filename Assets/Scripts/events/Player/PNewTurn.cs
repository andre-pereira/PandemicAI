using UnityEngine;
using System.Collections.Generic;

namespace OPEN.PandemicAI
{
    public class PNewTurn : PlayerEvent
    {
        string playerName;
        public override void Do(Timeline timeline)
        {
            var s = GameRoot.State;
            GameEvents.RequestNextPlayer();
            playerName = GameRoot.State.CurrentPlayer.Name;
        }

        public override float Act()
        {
            GUIEvents.RaiseDrawAll();
            return 0;
        }

        public override Dictionary<string, object> GetLogInfo()
        {
            return new Dictionary<string, object>
            {
                { "name", playerName}
            };
        }
    }
}