using System.Collections.Generic;

namespace OPEN.PandemicAI
{
    internal class RTreatPlay : PlayerEvent
    {
        private Plan plan;
        private int targetCity;
        private Enums.VirusName targetColor;

        public RTreatPlay(Player player, Plan plan) : base(player)
        { 
            if (plan.ActionQueue[0] is TreatCubeAction treatCubeAction)
            {
                targetCity = treatCubeAction.TargetCity;
                targetColor = treatCubeAction.TargetVirus;
            }
            else
            {
                targetCity = player.CurrentCity.CityCard.CityID;
                targetColor = player.CurrentCity.CityCard.VirusInfo.virusName;
            }
        }

        public override Dictionary<string, object> GetLogInfo()
        {
            return new Dictionary<string, object>
            {
                {"city", targetCity },
                {"cubeColor", targetColor.ToString() }
            };
        }
    }
}