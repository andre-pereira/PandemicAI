using System.Collections.Generic;

namespace OPEN.PandemicAI
{
    internal class RCurePlay : PlayerEvent
    {
        private Plan plan;
        private Enums.VirusName targetColor;

        public RCurePlay(Player player, Plan plan) : base(player)
        {
            this.plan = plan;
            if (plan.ActionQueue[0] is FindCureAction findCureAction)
            {
                targetColor = findCureAction.TargetVirus;
            }
        }

        public override Dictionary<string, object> GetLogInfo()
        {
            return new Dictionary<string, object>
            {
                {"cureColor", plan.TargetColor.ToString() }
            };
        }
    }
}