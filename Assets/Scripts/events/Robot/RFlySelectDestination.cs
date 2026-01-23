using System.Collections.Generic;

namespace OPEN.PandemicAI
{
    internal class RFlySelectDestination : PlayerEvent
    {
        private int targetCity;

        public RFlySelectDestination(Player player, Plan plan) : base(player)
        {

            if (plan.ActionQueue[0] is FlyAction flyAction)
            {
                targetCity = flyAction.TargetCity;
            }
        }

        public override Dictionary<string, object> GetLogInfo()
        {
            return new Dictionary<string, object>
                {
                    { "fromCity" , _player.CurrentCity.CityCard.CityID},
                    { "toCity" , targetCity },
                    { "chosenCard", targetCity}
                };
        }
    }
}