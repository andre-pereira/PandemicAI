using System.Linq;
using UnityEngine;

namespace OPEN.PandemicAI
{
    public class ActionRules : MonoBehaviour
    {
        public static EligibilityFlags Compute(Player player, GameStateData s)
        {
            var f = EligibilityFlags.None;

            if (player.ActionsRemaining == 0 || GameRoot.State.CurrentPlayer != player) return EligibilityFlags.None;

            // Always can end turn or move if actions remain
            f |= EligibilityFlags.EndTurn;
            f |= EligibilityFlags.Move;

            foreach (int card in player.Hand)
            {
                if (card == player.GetCurrentCityId())
                    f |= EligibilityFlags.CharterFlight;
                else f |= EligibilityFlags.DirectFlight;
            }

            if (player.CurrentCity.HasCubes())
                f |= EligibilityFlags.Treat;

            bool shareable = player.CurrentCity.PlayersInCity
                             .Any(p => p != player &&
                                       (p.Hand.Contains(p.GetCurrentCityId()) ||
                                        player.Hand.Contains(p.GetCurrentCityId())));
            if (shareable) f |= EligibilityFlags.Share;

            bool canCure = (!s.RedCureFound && player.RedCardsInHand.Count > 3) ||
                           (!s.YellowCureFound && player.YellowCardsInHand.Count > 3) ||
                           (!s.BlueCureFound && player.BlueCardsInHand.Count > 3);
            if (canCure && player.GetCurrentCityId() == GameRoot.Catalog.InitialCityId) f |= EligibilityFlags.Cure;

            return f;
        }
    }
}