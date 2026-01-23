using System.Collections.Generic;

namespace OPEN.PandemicAI
{
    internal class RMoveDragPawn : PlayerEvent
    {
        private int targetCity;
        public RMoveDragPawn(Player player, int targetCity) : base(player)
        {
            this.targetCity = targetCity;
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