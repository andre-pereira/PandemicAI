using System.Collections.Generic;

namespace OPEN.PandemicAI
{
    internal class RDiscardClickCard : PlayerEvent
    {
        private int cityToDiscard;

        public RDiscardClickCard(int v)
        {
            this.cityToDiscard = v;
        }

        public override Dictionary<string, object> GetLogInfo()
        {
            return new Dictionary<string, object>
            {
                {"chosenCard", cityToDiscard },
                {"cityCard", cityToDiscard },
                {"cardColor", CityDrawer.CityScripts[cityToDiscard].CityCard.VirusInfo.virusName.ToString()}
            };
        }
    }
} 