using System.Collections;
using System.Text;
using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using System;
using System.Linq;
using NUnit.Framework.Constraints;

namespace OPEN.PandemicAI
{

    public class GetEngineInfo
    {

        public static PandemicAI engine = new PandemicAI();
        private static readonly List<string> recentEvents = new List<string>();

        public static string GetGameInfo()
        {
            var gameInfo = new Dictionary<string, Dictionary<string, string>>
            {
                {
                    "curesFound",
                    new Dictionary<string, string>
                {
                    { "redCure", GameRoot.State.RedCureFound.ToString()},
                    { "yellowCure", GameRoot.State.YellowCureFound.ToString() },
                    { "blueCure", GameRoot.State.BlueCureFound.ToString() }
                }
                },
                {
                    "gameState",
                    new Dictionary<string, string>
                {
                    { "infectionRate", GameRoot.State.InfectionRateIndex.ToString()},
                    { "outbreakCount", GameRoot.State.OutbreakCounterIndex.ToString() },
                    { "yellowCubes", (16 - GameRoot.State.YellowCubes).ToString() },
                    { "blueCubes", (16 - GameRoot.State.BlueCubes).ToString() },
                    { "redCubes", (16 - GameRoot.State.RedCubes).ToString() },
                    { "playerDeck", GameRoot.State.PlayerDeck.Count.ToString() },
                        //{ "infectionDeck", GameRoot.State.InfectionDeck.Count.ToString() },
                        //{ "infectionDiscard",  string.Join(",", GameRoot.State.InfectionDiscard) },
                    }
                }
            };
            return InfoToString(gameInfo);
        }

        public static int FindCityIndexByName(string cityName)
        {
            for (int i = 0; i < GameCatalog.NumberOfCities; i++)
            {
                if (CityDrawer.CityScripts[i].CityCard.CityName.Equals(cityName, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            return -1;
        }

        public static string GetCityNameByIndex(int index)
        {
            string name = CityDrawer.CityScripts[index].CityCard.CityName;
            return name;
        }

        public static string GetCityInfo(string name)
        {
            int index = FindCityIndexByName(name);
            if (index == -1)
            {
                return $"City with name '{name}' not found.";
            }
            var cityInfo = new Dictionary<string, Dictionary<string, string>>();

            City city = CityDrawer.CityScripts[index];
            cityInfo.Add(city.CityCard.CityName, new Dictionary<string, string>
                   {
                       { "color", city.CityCard.VirusInfo.virusName.ToString() },
                       { "redCubes", city.cubes[Enums.VirusName.Red].ToString() },
                       { "yellowCubes", city.cubes[Enums.VirusName.Yellow].ToString() },
                       { "blueCubes", city.cubes[Enums.VirusName.Blue].ToString() },
                       { "neighbors", string.Join(",", city.CityCard.Neighbors.Select(neighbor => CityDrawer.CityScripts[neighbor].CityCard.CityName)) }
                   });
            return InfoToString(cityInfo);
        }

        public static string GetPlayerInfo(int playerID)
        {
            var PlayerInfo = new Dictionary<string, Dictionary<string, string>>();

            Player player = GameRoot.State.Players[playerID];

            PlayerInfo.Add(player.Name, new Dictionary<string, string>
            {
                { "name", player.Name },
                { "actionsRemaining", player.ActionsRemaining.ToString() },
                { "currentCity", player.CurrentCity.CityCard.CityName.ToString() },
                { "yellowCards", string.Join(",", player.YellowCardsInHand.Select(city => CityDrawer.CityScripts[city].CityCard.CityName)) },
                { "blueCards", string.Join(",", player.BlueCardsInHand.Select(city => CityDrawer.CityScripts[city].CityCard.CityName)) },
                { "redCards", string.Join(",", player.RedCardsInHand.Select(city => CityDrawer.CityScripts[city].CityCard.CityName)) }
            });

            return InfoToString(PlayerInfo);
        }

        public static string GetShortestPathFly(int playerID, string cityDest)
        {
            int cityDestIndex = FindCityIndexByName(cityDest);

            if (cityDestIndex == -1)
            {
                return $"Starting city '{cityDest}' not found.";
            }

            var cardsCanDiscard = engine.FindCardsCanDiscard(GameRoot.State.Players[playerID], GameRoot.State.Players[1 - playerID]);
            List<PlayerAction> result = BoardSearch.RouteConsideringCards(GameRoot.State.Players[playerID].CurrentCity.CityCard.CityID, cityDestIndex, cardsCanDiscard, CityDrawer.CityScripts);
            // Join all actions in string by using action.name, action.targetCity
            if (result == null || result.Count == 0)
            {
                return "No path found";
            }
            return string.Join(", ", result.Select(action => $"{action.Type} to {CityDrawer.CityScripts[action.TargetCity].CityCard.CityName}"));
        }

        public static string GetPlan(int playerID)
        {
            var PlanInfo = new Dictionary<string, Dictionary<string, string>>();

            var currentPlan = engine.PlanMove(GameRoot.State.Players[playerID], GameRoot.State.Players[1 - playerID]);
            if (currentPlan == null)
            {
                return "No current plan";
            }

            PlanInfo.Add("CurrentPlan", new Dictionary<string, string>
            {
                { "priority", currentPlan.PlanPriority.ToString() },
                { "targetLocation", CityDrawer.CityScripts[currentPlan.TargetCity].CityCard.CityName },
                { "targetColor", currentPlan.TargetColor.ToString() },
                { "actions", string.Join(", ", currentPlan.ActionQueue.Select(action =>
                    (action.Type == Enums.ActionType.Fly || action.Type == Enums.ActionType.Charter || action.Type == Enums.ActionType.Move)
                        ? $"{action.Type} to {CityDrawer.CityScripts[action.TargetCity].CityCard.CityName}"
                        : $"{action.Type} {action.TargetVirus}"
                )) },
                {
                    "planExplanation", currentPlan.PlanExplanation
                }
            });

            return InfoToString(PlanInfo);
        }

        public static string GetValidActions(int playerID)
        {
            var validActions = new List<string>();

            // Share actions
            if (GameRoot.State.Players[playerID].CurrentCity.PlayersInCity.Count > 1)
            {
                int currentCityId = GameRoot.State.Players[playerID].GetCurrentCityId();
                bool playerHasCard = GameRoot.State.Players[playerID].Hand.Contains(currentCityId);
                bool otherPlayerHasCard = GameRoot.State.Players[1 - playerID].Hand.Contains(currentCityId);

                if (playerHasCard)
                {
                    validActions.Add("Give current city card to another player in the same city");
                }
                if (otherPlayerHasCard)
                {
                    validActions.Add("Receive current city card from another player in the same city");
                }
            }

            // Move actions
            foreach (int neighborIndex in GameRoot.State.Players[playerID].CurrentCity.CityCard.Neighbors)
            {
                validActions.Add($"Move to {CityDrawer.CityScripts[neighborIndex].CityCard.CityName}");
            }

            // Direct Flight actions
            foreach (int city in GameRoot.State.Players[playerID].Hand)
            {
                validActions.Add($"Direct Flight to {CityDrawer.CityScripts[city].CityCard.CityName}");
            }

            // Charter Flight
            if (GameRoot.State.Players[playerID].Hand.Contains(GameRoot.State.Players[playerID].CurrentCity.CityCard.CityID))
            {
                validActions.Add("Charter Flight anywhere on the board");
            }

            // Treat actions per virus color
            var cubes = GameRoot.State.Players[playerID].CurrentCity.cubes;
            foreach (Enums.VirusName virus in Enum.GetValues(typeof(Enums.VirusName)))
            {
                if (virus == Enums.VirusName.None) continue;
                if (cubes[virus] > 0)
                {
                    validActions.Add($"Treat {virus} cubes in current city");
                }
            }

            // Cure actions per virus color
            if (GameRoot.State.Players[playerID].GetCurrentCityId() == GameRoot.Catalog.InitialCityId)
            {
                if (!GameRoot.State.RedCureFound && GameRoot.State.Players[playerID].RedCardsInHand.Count > 3)
                {
                    validActions.Add("Cure Red virus");
                }
                if (!GameRoot.State.YellowCureFound && GameRoot.State.Players[playerID].YellowCardsInHand.Count > 3)
                {
                    validActions.Add("Cure Yellow virus");
                }
                if (!GameRoot.State.BlueCureFound && GameRoot.State.Players[playerID].BlueCardsInHand.Count > 3)
                {
                    validActions.Add("Cure Blue virus");
                }
            }

            return string.Join(", ", validActions);
        }

        public static void AddEngineEvent(string type, string eventDescription)
        {
            //Debug.Log($"Engine Event: {type} - {eventDescription}");
            recentEvents.Add(eventDescription);
            // Make sure the list does not exceed 20 items
            if (recentEvents.Count > 10)
            {
                recentEvents.RemoveAt(0);
            }
        }

        public static string GetEngineEvents()
        {
            return string.Join(", ", recentEvents);
        }

        private static string InfoToString(Dictionary<string, Dictionary<string, string>> gameInfo)
        {
            var result = new StringBuilder();

            foreach (var category in gameInfo)
            {
                result.AppendLine($"[{category.Key}]");
                foreach (var item in category.Value)
                {
                    result.AppendLine($"  {item.Key}: {item.Value}");
                }
                result.AppendLine();
            }

            return result.ToString();
        }

        internal static bool LLMTurn()
        {
            if (GameRoot.State.CurrentPlayer.Role == Player.Roles.QuarantineSpecialist)
            {
                return true;
            }
            else return false;
        }
    }
}
