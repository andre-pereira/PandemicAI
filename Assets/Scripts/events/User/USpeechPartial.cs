using System.Collections.Generic;

namespace OPEN.PandemicAI
{
    internal class USpeechPartial : EngineEvent
    {
        private string s;

        public USpeechPartial(string s)
        {
            this.s = s;
        }

        public override Dictionary<string, object> GetLogInfo()
        {
            //return a dictionary with the speech information
            return new Dictionary<string, object>
            {
                { "speech", s }
            };
        }
    }
}