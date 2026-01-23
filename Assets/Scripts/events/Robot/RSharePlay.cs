using System.Collections.Generic;

namespace OPEN.PandemicAI
{
    internal class RSharePlay : PlayerEvent
    {
        private Plan plan;

        public RSharePlay(Player player, Plan plan) : base(player)
        {
            this.plan = plan;
        }

        public override Dictionary<string, object> GetLogInfo()
        {
            return new Dictionary<string, object>
            {
                { "cityCard", plan.TargetCity },
                { "cityColor", plan.TargetColor.ToString() }
            };
        }
    }
}