using System.Collections.Generic;

namespace OPEN.PandemicAI
{
    internal class RShareAccept : PlayerEvent
    {
        private Plan plan;
        private string color;

        public RShareAccept(Player player, Plan plan):base (player)
        {
            this.plan = plan;
            color = CityDrawer.CityScripts[plan.TargetCity].CityCard.VirusInfo.virusName.ToString();


        }

        public override Dictionary<string, object> GetLogInfo()
        {
            return new Dictionary<string, object>
            {
                { "cityCard", plan.TargetCity },
                { "cityColor", color }
            };
        }
    }
}