using static OPEN.PandemicAI.Enums;
using System.Collections.Generic;

namespace OPEN.PandemicAI
{
    public class EGameOver : EngineEvent
    {
        public GameOverReasons Reason;

        public EGameOver(GameOverReasons reason)
        {
            Reason = reason;   
        }

        public override void Do(Timeline timeline)
        {
            timeline.ClearPendingEvents();
            GameEvents.RequestStateChange(GameState.GameOver);
        }

        public override float Act()
        {
            GUIEvents.RaiseGameOver(Reason);
            return 0;
        }

        public override Dictionary<string, object> GetLogInfo()
        {
            return new Dictionary<string, object>
            {
                { "condition", Reason.ToString() }
            };
        }
    }
}