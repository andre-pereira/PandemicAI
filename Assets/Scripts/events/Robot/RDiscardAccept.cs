using System.Collections.Generic;

namespace OPEN.PandemicAI
{
    internal class RDiscardAccept : PlayerEvent
    {
        private int cityToDiscard;

        public RDiscardAccept(int v )
        {
            this.cityToDiscard = v;
        }

        public override Dictionary<string, object> GetLogInfo()
        {
            return new Dictionary<string, object>
            {
                {"cityCard", cityToDiscard },
                {"chosenCard", cityToDiscard },
                {"cardColor", CityDrawer.CityScripts[cityToDiscard].CityCard.VirusInfo.virusName}
            };
        }
    }
} 