using System.Collections.Generic;

namespace OPEN.PandemicAI
{
    internal class RIntentHumor : PlayerEvent
    {
        private string city; 
      public RIntentHumor(int playerCity)
        {
            city = GetEngineInfo.GetCityNameByIndex(playerCity);
        }

        public override Dictionary<string, object> GetLogInfo()
        {
            return new Dictionary<string, object> { 
                { "city", city },
                { "condition", city }
            };
        }
    }
}