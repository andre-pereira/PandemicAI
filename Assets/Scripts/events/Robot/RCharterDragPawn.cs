using System.Collections.Generic;

namespace OPEN.PandemicAI
{
    internal class RCharterDragPawn : PlayerEvent
    {
        private int targetCity;

        public RCharterDragPawn(Player player, Plan plan) : base(player) 
        {
            if (plan.ActionQueue[0] is CharterAction charterAction)
            {
                targetCity = charterAction.TargetCity;
            }
        }

        public override Dictionary<string, object> GetLogInfo()
        {
            return new Dictionary<string, object>
            {
                { "fromCity" , _player.CurrentCity.CityCard.CityID},
                { "toCity" , targetCity }
            };
        }
    }
}