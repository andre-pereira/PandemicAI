using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OPEN.PandemicAI
{
    public class EInitializeFirstPlayer : EngineEvent
    {
        public override void Do(Timeline timeline)
        {
            var s = GameRoot.State;
            s.CurrentPlayer.ResetTurn();
        }

        public override float Act()
        {
            foreach (var item in GameRoot.State.InfectionDiscard)
            {
                GameObject cardToAddObject = Object.Instantiate(GameGUI.Assets.infectionCardPrefab, DeckDrawer.InfectionDiscard.transform);
                cardToAddObject.GetComponent<InfectionCardDisplay>().CityCardData = CityDrawer.CityScripts[item].CityCard;
            }
            GUIEvents.RaiseDrawAll();
            return 0f;
        }

        public override Dictionary<string, object> GetLogInfo()
        {
            return new Dictionary<string, object>
            {
                { "role", GameRoot.State.CurrentPlayer?.Role.ToString() }
            };
        }

    }
}