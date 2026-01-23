using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;


namespace OPEN.PandemicAI
{
    internal class RChangingPlan : PlayerEvent
    {
        private string condition;
        private string color;
        private const string CSAFEGUARDCUBESUPPLY = "SafeguardCubeSupply";
        private const string CSAFEGUARDOUTBREAK = "SafeguardOutbreak";
        private const string CSHAREKNOWLEDGE = "ShareKnowledge";
        private const string CFINDCURE = "FindCure";
        private const string CMANAGINGDISEASE = "ManagingDisease";

        public RChangingPlan(Plan plan)
        {
            color = plan.TargetColor.ToString();
            switch (plan.PlanPriority)
            {
                case Plan.PlanPriorities.ShareKnowledge:
                    condition = CSHAREKNOWLEDGE;
                    break;
                case Plan.PlanPriorities.FindCure:
                    condition = CFINDCURE;
                    break;
                case Plan.PlanPriorities.SafeguardOutbreak:
                    condition = CSAFEGUARDOUTBREAK;
                    break;
                case Plan.PlanPriorities.SafeguardCubeSupply:
                    condition = CSAFEGUARDCUBESUPPLY;
                    break;
                case Plan.PlanPriorities.ManagingDisease:
                    condition = CMANAGINGDISEASE;
                    break;
            }
        }

        public override Dictionary<string, object> GetLogInfo()
        {
            return new Dictionary<string, object>
            {
                { "condition" , condition},
                {"color", color }
            };
        }

    }
    
}