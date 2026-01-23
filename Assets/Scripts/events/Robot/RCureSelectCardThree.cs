using System.Collections.Generic;

namespace OPEN.PandemicAI
{
    internal class RCureSelectCardThree : PlayerEvent
    {
        private readonly Plan plan;
        private int selectedCity;
        private Enums.VirusName selectedColor;

        public RCureSelectCardThree(Player player, Plan plan) : base(player)
        {
            this.plan = plan;
            if (plan.ActionQueue[0] is FindCureAction findCureAction)
            {
                selectedCity = findCureAction.SelectedCards[2];
                selectedColor = findCureAction.TargetVirus;
            }
        }

        public override Dictionary<string, object> GetLogInfo()
        {
            return new Dictionary<string, object>
            {
                {"cityCard", selectedCity },
                {"chosenCard", selectedCity},
                {"cityColor", selectedColor.ToString() }
            };
        }
    }
}