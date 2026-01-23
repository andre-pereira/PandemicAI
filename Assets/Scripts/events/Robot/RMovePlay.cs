using System.Collections.Generic;

namespace OPEN.PandemicAI
{
    internal class RMovePlay : PlayerEvent
    {
        private int targetCity;
        public RMovePlay(Player player, int targetCity) : base(player)
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