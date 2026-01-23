using UnityEngine;
using System.Collections.Generic;

namespace OPEN.PandemicAI
{
    public class RCureClickCube : PlayerEvent
    {
        private Plan plan;

        public RCureClickCube(Player player, Plan plan) : base(player)
        {
            this.plan = plan;
        }

        public override Dictionary<string, object> GetLogInfo()
        {
            return new Dictionary<string, object>
            {
                {"color", plan.TargetColor.ToString() }
            };
        }
    }
} 