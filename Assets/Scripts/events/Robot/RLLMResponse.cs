using System.Collections.Generic;
using System.Diagnostics;

namespace OPEN.PandemicAI
{
    internal class RLLMResponse : EngineEvent
    {
        private string r;

        public RLLMResponse(string r)
        {
            this.r = r;
        }

        public override Dictionary<string, object> GetLogInfo()
        {
            //return a dictionary with the speech information
            return new Dictionary<string, object>
            {
                { "response", r }
            };
        }
    }
}