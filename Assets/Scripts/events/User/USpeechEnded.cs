using System.Collections.Generic;

namespace OPEN.PandemicAI
{
    internal class USpeechEnded : EngineEvent
    {
        public readonly string SpeechText;

        public USpeechEnded(string SpeechText)
        {
            this.SpeechText = SpeechText;
        }

        public override Dictionary<string, object> GetLogInfo()
        {
            //return a dictionary with the speech information
            return new Dictionary<string, object>
            {
                { "speech", SpeechText }
            };
        }
    }
}