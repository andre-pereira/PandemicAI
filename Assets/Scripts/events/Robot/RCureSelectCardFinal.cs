using UnityEngine;
using System.Collections.Generic;

namespace OPEN.PandemicAI
{
    internal class RCureSelectCardFinal : PlayerEvent
    {
        private readonly Plan plan;
        private int selectedCity;
        private Enums.VirusName selectedColor;

        public RCureSelectCardFinal(Player player, Plan plan) : base(player)
        {
            if (plan.ActionQueue[0] is FindCureAction findCureAction)
            {
                selectedCity = findCureAction.SelectedCards[3];
                selectedColor = findCureAction.TargetVirus;
            }
        }

        public override Dictionary<string, object> GetLogInfo()
        {
            return new Dictionary<string, object>
            {
                {"cityCard", selectedCity },
                {"chosenCard", selectedCity },
                {"cityColor", selectedColor }
            };
        }
    }
} 