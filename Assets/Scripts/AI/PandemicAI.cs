using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static OPEN.PandemicAI.Enums;
using static OPEN.PandemicAI.Plan;

namespace OPEN.PandemicAI
{
    public class PandemicAI
    {
        public static PandemicAI Instance { get; private set; }
        public Plan currentPlan;
        public int currentPlanTargetLocation;
        public VirusName currentPlanTargetColor;
        private const int SAFEGUARDCUBESTOCKCONSTANT = 2;
        public enum AIGameState { Start, PlayerTurn, AITurn, AIDiscard, End }

        // A static list of playable virus colors to avoid magic values and allow easy iteration.
        private static readonly List<VirusName> PlayableColors = new() { VirusName.Red, VirusName.Yellow, VirusName.Blue };

        #region Card Discard Logic

        /// <summary>
        /// Selects the best card for an AI player to discard from their hand.
        /// </summary>
        /// <param name="ai">The AI player who needs to discard.</param>
        /// <param name="partner">The other player in the game.</param>
        /// <returns>The integer ID of the card to discard.</returns>
        public int SelectCardToDiscard(Player ai, Player partner)
        {
            var discardCandidates = new List<int>(ai.Hand);

            // Rule 0: Never discard Geneva
            if (discardCandidates.Contains(GameRoot.Catalog.InitialCityId))
            {
                discardCandidates.Remove(GameRoot.Catalog.InitialCityId);
            }

            // Rule 1: Protect complete 4-card sets for uncured diseases.
            foreach (var color in PlayableColors)
            {
                if (IsUncuredSetOf(ai, color, 4))
                {
                    discardCandidates.RemoveAll(card => ai.GetCardsInHand(color).Contains(card));
                }
            }

            // Rule 2: Protect card needed for the current "Share Knowledge" plan.
            if (ai.Plan?.PlanPriority == PlanPriorities.ShareKnowledge && ai.Hand.Contains(ai.Plan.TargetCity))
            {
                discardCandidates.Remove(ai.Plan.TargetCity);
            }

            // Edge Case: If all cards are protected, we must discard one to meet hand limit.
            if (discardCandidates.Count == 0)
            {
                return ai.Hand.FirstOrDefault(); // Fallback to discarding the first card.
            }

            // Priority 1: Discard a card of a CURED color.
            var curedColorCandidates = discardCandidates.Where(card => IsCuredColor(CityDrawer.CityScripts[card].CityCard.VirusInfo.virusName)).ToList();
            if (curedColorCandidates.Any())
            {
                return GetLeastValuableCard(curedColorCandidates);
            }

            // Priority 2: Discard from an uncured set where the AI has fewer or equal cards than the partner.
            foreach (var color in PlayableColors)
            {
                if (!IsCuredColor(color))
                {
                    var colorCandidates = discardCandidates.Intersect(ai.GetCardsInHand(color)).ToList();
                    if (colorCandidates.Any() && ai.GetCardsInHand(color).Count <= partner.GetCardsInHand(color).Count)
                    {
                        return GetLeastValuableCard(colorCandidates);
                    }
                }
            }

            // Fallback: Discard the least valuable card from all remaining candidates.
            return GetLeastValuableCard(discardCandidates);
        }

        // Helper to check if a player has a specific number of cards for an uncured disease.
        private bool IsUncuredSetOf(Player player, VirusName color, int count)
        {
            return !IsCuredColor(color) && player.GetCardsInHand(color).Count == count;
        }

        // Helper to get the status of a cure.
        private bool IsCuredColor(VirusName color)
        {
            return color switch
            {
                VirusName.Red => GameRoot.State.RedCureFound,
                VirusName.Yellow => GameRoot.State.YellowCureFound,
                VirusName.Blue => GameRoot.State.BlueCureFound,
                _ => false,
            };
        }

        // Helper to select the card corresponding to the city with the fewest disease cubes.
        private int GetLeastValuableCard(List<int> candidates)
        {
            return candidates.OrderBy(cardId => CityDrawer.CityScripts[cardId].GetMaxNumberCubes()).First();
        }

        #endregion

        #region Plan Selection Logic

        /// <summary>
        /// Main entry point to determine and execute the AI's turn plan.
        /// </summary>
        public Plan PlanMove(Player ai, Player partner, PlanPriorities currentPlanPriority = PlanPriorities.None)
        {
            var actionQueue = new List<PlayerAction>();
            var aiCanDiscard = FindCardsCanDiscard(ai, partner);
            var partnerCanDiscard = FindCardsCanDiscard(partner, ai);
            int actionsRemaining = ai.ActionsRemaining;

            // 1. Determine the highest priority plan for this turn.
            if (currentPlanPriority == PlanPriorities.None)
            {
                currentPlanPriority = SelectCurrentPlan(ai, partner, aiCanDiscard, partnerCanDiscard, actionsRemaining);
            }
            else // Fixed priority plan selection
            {
                currentPlanPriority = SelectCurrentPlanFixedPriority(ai, partner, aiCanDiscard, partnerCanDiscard, actionsRemaining, currentPlanPriority);
            }

            // 2. Populate the action queue based on the selected plan.
            PopulateActionQueue(ai, partner, currentPlanPriority, actionQueue, ref actionsRemaining, aiCanDiscard);

            // 2.5 Rule-based creation of a human-readable explanation of the plan.
            string planExplanation = CreatePlanExplanation(ai, aiCanDiscard, currentPlanPriority, currentPlanTargetLocation, currentPlanTargetColor, actionQueue);
            //Debug.Log($"[AI] Plan for {ai.Name}:\n{planExplanation}");

            // 3. Finalize and return the plan.
            var plan = new Plan(currentPlanPriority, currentPlanTargetLocation, currentPlanTargetColor, actionQueue, planExplanation);
            ai.Plan = plan;
            return plan;
        }

        /// <summary>
        /// Evaluates the game state to determine the highest priority plan for the AI.
        /// </summary>
        private PlanPriorities SelectCurrentPlan(Player ai, Player partner, List<int> aiCanDiscard, List<int> partnerCanDiscard, int actionsRemaining)
        {
            (bool canCure, VirusName virus) = ai.HasCardSetForUncuredDisease();
            // --- Highest Priority: Find an immediate cure if possible ---
            if (TrySelectFindCurePlan(ai, actionsRemaining, aiCanDiscard, canCure, virus)) return PlanPriorities.FindCure;

            if (GameRoot.State.PlayerDeck.Count > 2) // Skip if it's the last turn
            {
                // --- Safeguard Plans for Critical Situations ---
                if (TrySelectSafeguardCubeSupplyPlan(ai, aiCanDiscard)) return PlanPriorities.SafeguardCubeSupply;

                if (TrySelectSafeguardOutbreakPlan(ai, partner, aiCanDiscard, actionsRemaining)) return PlanPriorities.SafeguardOutbreak;
            }

            // --- Mid-Priority: Proactively move to find a cure later ---
            if (canCure)
            {
                currentPlanTargetLocation = GameRoot.Catalog.InitialCityId;
                currentPlanTargetColor = virus;
                return PlanPriorities.FindCure;
            }

            // --- Default Plans: Share Knowledge or Treat Disease ---
            int shareLocation = SetShareLocation(ai, partner, aiCanDiscard, partnerCanDiscard);
            if (shareLocation > 0)
            {
                currentPlanTargetLocation = shareLocation;
                currentPlanTargetColor = VirusName.None;
                return PlanPriorities.ShareKnowledge;
            }

            var (bauLocation, bauColor) = FindBestManagingDiseaseTarget(ai, aiCanDiscard, actionsRemaining);
            if (bauLocation != -1)
            {
                currentPlanTargetLocation = bauLocation;
                currentPlanTargetColor = bauColor;
                return PlanPriorities.ManagingDisease;
            }

            return PlanPriorities.None; // Fallback if no action is possible.
        }

        // Helper to check for and set up a "Find Cure" plan.
        private bool TrySelectFindCurePlan(Player ai, int actionsRemaining, List<int> aiCanDiscard, bool canCure, VirusName virus)
        {
            int distanceToGeneva = BoardSearch.RouteConsideringCards(ai.CurrentCity.CityCard.CityID, GameRoot.Catalog.InitialCityId, aiCanDiscard, CityDrawer.CityScripts).Count;

            if (distanceToGeneva < actionsRemaining && canCure)
            {
                currentPlanTargetLocation = GameRoot.Catalog.InitialCityId;
                currentPlanTargetColor = virus;
                return true;
            }
            return false;
        }

        // Helper to check for and set up a "Safeguard Cube Supply" plan.
        private bool TrySelectSafeguardCubeSupplyPlan(Player ai, List<int> aiCanDiscard)
        {
            int infectionRate = GameRoot.Catalog.InfectionRateValues[GameRoot.State.InfectionRateIndex];
            foreach (var color in PlayableColors)
            {
                if (GameRoot.State.GetCubeStock(color) < SAFEGUARDCUBESTOCKCONSTANT + infectionRate)
                {
                    currentPlanTargetLocation = GetClosestCityInfectedByColor(ai, color, aiCanDiscard);
                    if (currentPlanTargetLocation == -1) continue;
                    currentPlanTargetColor = color;
                    return true;
                }
            }
            return false;
        }

        // Helper to check for and set up a "Safeguard Outbreak" plan.
        private bool TrySelectSafeguardOutbreakPlan(Player ai, Player partner, List<int> aiCanDiscard, int actionsRemaining)
        {
            var (maxOutbreaks, outbreakCities, outbreakColor) = FindMostCriticalOutbreakChain(ai, aiCanDiscard, actionsRemaining);

            if (GameRoot.State.OutbreakCounterIndex + maxOutbreaks > 2 && !outbreakCities.Contains(partner.CurrentCity.CityCard.CityID))
            {
                var (_, closestCityId) = GetMinimumTimeToOutbreakLocation(outbreakCities, ai.CurrentCity.CityCard.CityID, aiCanDiscard.ToArray(), CityDrawer.CityScripts, actionsRemaining);
                if (closestCityId != -1)
                {
                    currentPlanTargetLocation = closestCityId;
                    currentPlanTargetColor = outbreakColor;
                    return true;
                }
            }
            return false;
        }

        // Helper to find the best target city for a "Business As Usual" treatment plan.
        private (int targetLocation, VirusName targetColor) FindBestManagingDiseaseTarget(Player ai, List<int> aiCanDiscard, int actionsRemaining)
        {
            int bestLocation = -1;
            VirusName bestColor = VirusName.None;
            int bestCubes = 0;
            int bestDistance = int.MaxValue;

            foreach (City city in CityDrawer.CityScripts)
            {
                foreach (var virus in PlayableColors.Where(v => city.cubes[v] > 0))
                {
                    int distance = BoardSearch.RouteConsideringCards(ai.CurrentCity.CityCard.CityID, city.CityCard.CityID, aiCanDiscard, CityDrawer.CityScripts).Count;
                    int maxActionsForMove = ai.Role == Player.Roles.ContainmentSpecialist ? actionsRemaining : actionsRemaining - 1;

                    bool isReachable = distance <= maxActionsForMove;
                    bool isHighPriority = city.cubes[virus] >= 2 && distance <= (actionsRemaining - 1);

                    if (isReachable && (isHighPriority || city.cubes[virus] > 0))
                    {
                        if ((city.cubes[virus] > bestCubes) || (city.cubes[virus] == bestCubes && distance < bestDistance))
                        {
                            bestCubes = city.cubes[virus];
                            bestDistance = distance;
                            bestLocation = city.CityCard.CityID;
                            bestColor = virus;
                        }
                    }
                }
            }
            return (bestLocation, bestColor);
        }
        private PlanPriorities SelectCurrentPlanFixedPriority(Player ai, Player partner, List<int> aiCanDiscard, List<int> partnerCanDiscard, int actionsRemaining, PlanPriorities currentPlanPriority)
        {
            (bool canCure, VirusName virus) = ai.HasCardSetForUncuredDisease();
            // --- Highest Priority: Find an immediate cure if possible ---
            if (currentPlanPriority == PlanPriorities.FindCure && TrySelectFindCurePlan(ai, actionsRemaining, aiCanDiscard, canCure, virus)) return PlanPriorities.FindCure;


                // --- Safeguard Plans for Critical Situations ---
            if (currentPlanPriority == PlanPriorities.SafeguardCubeSupply && TrySelectSafeguardCubeSupplyPlan(ai, aiCanDiscard)) return PlanPriorities.SafeguardCubeSupply;

            if (currentPlanPriority == PlanPriorities.SafeguardOutbreak && TrySelectSafeguardOutbreakPlan(ai, partner, aiCanDiscard, actionsRemaining)) return PlanPriorities.SafeguardOutbreak;
            

            // --- Mid-Priority: Proactively move to find a cure later ---
            if (canCure || currentPlanPriority == PlanPriorities.FindCure)
            {
                currentPlanTargetLocation = GameRoot.Catalog.InitialCityId;
                currentPlanTargetColor = virus;
                return PlanPriorities.FindCure;
            }

            // --- Default Plans: Share Knowledge or Treat Disease ---
            int shareLocation = SetShareLocation(ai, partner, aiCanDiscard, partnerCanDiscard);
            if (currentPlanPriority == PlanPriorities.ShareKnowledge && shareLocation > 0)
            {
                currentPlanTargetLocation = shareLocation;
                currentPlanTargetColor = VirusName.None;
                return PlanPriorities.ShareKnowledge;
            }

            var (bauLocation, bauColor) = FindBestManagingDiseaseTarget(ai, aiCanDiscard, actionsRemaining);
            if (bauLocation != -1)
            {
                currentPlanTargetLocation = bauLocation;
                currentPlanTargetColor = bauColor;
                return PlanPriorities.ManagingDisease;
            }

            return PlanPriorities.None; // Fallback if no action is possible.
        }
        #endregion

        #region Action Queue Population

        /// <summary>
        /// Computes the specific actions for a given plan priority, target, and available actions.
        /// </summary>
        /// <param name="ai">The AI player.</param>
        /// <param name="partner">The other player.</param>
        /// <param name="priority">The priority of the plan to execute.</param>
        /// <param name="targetLocation">The target city ID for the plan.</param>
        /// <param name="targetColor">The target virus color for the plan.</param>
        /// <param name="actionsLeft">The number of actions the AI has available.</param>
        /// <returns>A list of PlayerAction objects representing the computed plan, limited to the available actions.</returns>
        public List<PlayerAction> ComputePlanActions(Player ai, Player partner, PlanPriorities priority, int targetLocation, VirusName targetColor, int actionsLeft)
        {
            // Set instance variables required by helper methods
            this.currentPlanTargetLocation = targetLocation;
            this.currentPlanTargetColor = targetColor;

            var actionQueue = new List<PlayerAction>();
            var aiCanDiscard = FindCardsCanDiscard(ai, partner);
            int actionsRemaining = actionsLeft;

            // Use the existing logic to populate the action queue based on the plan priority
            switch (priority)
            {
                case PlanPriorities.ShareKnowledge:
                    QueueShareKnowledgePlan(ai, partner, ref actionsRemaining, actionQueue, aiCanDiscard);
                    break;
                case PlanPriorities.FindCure:
                    QueueMoveAndAct(ai, ref actionsRemaining, actionQueue, aiCanDiscard, () =>
                        new FindCureAction(currentPlanTargetLocation, currentPlanTargetColor, ai.GetCardsInHand(currentPlanTargetColor).Take(4).ToList()));
                    break;
                case PlanPriorities.SafeguardOutbreak:
                case PlanPriorities.SafeguardCubeSupply:
                case PlanPriorities.ManagingDisease:
                    QueueMoveAndAct(ai, ref actionsRemaining, actionQueue, aiCanDiscard, () =>
                        new TreatCubeAction(currentPlanTargetLocation, currentPlanTargetColor));
                    break;
                case PlanPriorities.None:
                    if (actionsRemaining > 0) actionQueue.Add(new EndTurnAction(ai));
                    break;
            }

            return actionQueue;
        }

        /// <summary>
        /// Generates a human-readable explanation of the AI's current plan and actions.
        /// </summary>
        private string CreatePlanExplanation(Player ai, List<int> aiCanDiscard, PlanPriorities priority, int targetLocation, VirusName targetColor, List<PlayerAction> actions)
        {
            var explanation = new System.Text.StringBuilder();
            string targetCityName = targetLocation != -1 ? CityDrawer.CityScripts[targetLocation].CityCard.CityName : "an unknown location";

            string priorityExplanation;
            if (priority == PlanPriorities.FindCure)
            {
                priorityExplanation = $"Goal: Cure the {targetColor} disease in Geneva.";
            }
            else if (priority == PlanPriorities.SafeguardCubeSupply)
            {
                priorityExplanation = $"Goal: Treat {targetColor} in {targetCityName} to protect low {targetColor} cube supply, \n currently at {GameRoot.State.GetCubeStock(targetColor)}.";
            }
            else if (priority == PlanPriorities.SafeguardOutbreak)
            {
                var (maxOutbreaks, outbreakCities, _) = FindMostCriticalOutbreakChain(ai, aiCanDiscard, ai.ActionsRemaining);
                priorityExplanation = $"Goal: Treat {targetColor} in {targetCityName} to prevent potential {maxOutbreaks} outbreak \n in {string.Join(", ", outbreakCities)}, since current outbreak level is {GameRoot.State.OutbreakCounterIndex} (Game over at 4 outbreaks).";
            }
            else if (priority == PlanPriorities.ShareKnowledge)
            {
                priorityExplanation = $"Goal: Meet teammate at {targetCityName} to share the city card and contribute to {CityDrawer.CityScripts[targetLocation].CityCard.VirusInfo.name} cure.";
            }
            else if (priority == PlanPriorities.ManagingDisease)
            {
                priorityExplanation = $"Goal: Treat {targetColor} disease in {targetCityName} to minimize future outbreak";
            }
            else if (priority == PlanPriorities.None)
            {
                priorityExplanation = "Goal: No specific plan. Ending turn.";
            }
            else
            {
                priorityExplanation = "Goal: Executing a plan.";
            }
            explanation.AppendLine(priorityExplanation);
            explanation.AppendLine("Actions:");

            if (actions.Any())
            {
                for (int i = 0; i < actions.Count; i++)
                {
                    explanation.AppendLine($"{i + 1}. {actions[i].Description}");
                }
            }
            else
            {
                explanation.AppendLine("  - No actions to perform.");
            }

            return explanation.ToString();
        }

        /// <summary>
        /// Fills the action queue based on the selected plan priority.
        /// </summary>
        private void PopulateActionQueue(Player ai, Player partner, PlanPriorities currentPlanPriority, List<PlayerAction> actionQueue, ref int actionsRemaining, List<int> aiCanDiscard)
        {
            switch (currentPlanPriority)
            {
                case PlanPriorities.ShareKnowledge:
                    QueueShareKnowledgePlan(ai, partner, ref actionsRemaining, actionQueue, aiCanDiscard);
                    break;
                case PlanPriorities.FindCure:
                    QueueMoveAndAct(ai, ref actionsRemaining, actionQueue, aiCanDiscard, () =>
                        new FindCureAction(currentPlanTargetLocation, currentPlanTargetColor, ai.GetCardsInHand(currentPlanTargetColor).Take(4).ToList()));
                    break;
                case PlanPriorities.SafeguardOutbreak:
                case PlanPriorities.SafeguardCubeSupply:
                case PlanPriorities.ManagingDisease:
                    QueueMoveAndAct(ai, ref actionsRemaining, actionQueue, aiCanDiscard, () =>
                        new TreatCubeAction(currentPlanTargetLocation, currentPlanTargetColor));
                    break;
                case PlanPriorities.None:
                    if (actionsRemaining > 0) actionQueue.Add(new EndTurnAction(ai));
                    break;
            }

            if (actionQueue.Count > ai.ActionsRemaining)
            {
                Debug.LogWarning($"Action queue size {actionQueue.Count} exceeds {ai.ActionsRemaining} actions remaining in plan priority {currentPlanPriority}.");
            }
        }

        /// <summary>
        /// Generic helper to queue movement to a target and then perform a final action.
        /// </summary>
        private void QueueMoveAndAct(Player ai, ref int actionsLeft, List<PlayerAction> queue, List<int> aiCanDiscard, Func<PlayerAction> createFinalAction)
        {
            // Move to the target location.
            if (ai.CurrentCity.CityCard.CityID != currentPlanTargetLocation)
            {
                actionsLeft = QueueFastestPathToTarget(ai, actionsLeft, queue, aiCanDiscard);
            }

            // If at the target (or will arrive) with actions left, perform the final action.
            if (WillArriveAtTarget(ai, queue, currentPlanTargetLocation) && actionsLeft > 0)
            {
                queue.Add(createFinalAction());
                actionsLeft--;
            }
        }

        /// <summary>
        /// This plan is unique because it may involve treating cubes *along the path* to the target.
        /// </summary>
        private void QueueShareKnowledgePlan(Player ai, Player partner, ref int actionsLeft, List<PlayerAction> queue, List<int> aiCanDiscard)
        {
            if (partner.CurrentCity.CityCard.CityID != currentPlanTargetLocation)
            {
                // Find all paths to the target location within the remaining actions.
                int maxDepth = ai.Role == Player.Roles.ContainmentSpecialist ? actionsLeft : actionsLeft - 1;
                var possiblePaths = BoardSearch.BFSWithLimitedDepth(maxDepth, ai.CurrentCity.CityCard.CityID, currentPlanTargetLocation, aiCanDiscard, CityDrawer.CityScripts);

                if (possiblePaths.Any())
                {
                    BoardSearch.PathResult bestPath = getBestPathToTarget(possiblePaths, ai);
                    int treatActionsBudget = actionsLeft - bestPath.Actions.Count;
                    var pathAndTreatmentActions = new List<PlayerAction>();

                    if (treatActionsBudget > 0)
                    {
                        // Logic to interleave treatments with moves
                        var citiesOnPath = new List<int> { ai.CurrentCity.CityCard.CityID };
                        citiesOnPath.AddRange(bestPath.Actions.Select(a => a.TargetCity));
                        // Debug.Log($"Cities on path: {string.Join(", ", citiesOnPath)}");
                        var treatActions = GetPrioritizedTreatmentsOnPath(citiesOnPath.Distinct(), ai, treatActionsBudget);
                        var treatmentsByCity = treatActions.ToLookup(t => t.TargetCity);

                        // Build final action sequence
                        var visitedCities = new List<int>();
                        int currentCityId = ai.CurrentCity.CityCard.CityID;
                        if (treatmentsByCity.Contains(currentCityId)) pathAndTreatmentActions.AddRange(treatmentsByCity[currentCityId]);
                        visitedCities.Add(currentCityId);

                        foreach (var moveAction in bestPath.Actions)
                        {
                            pathAndTreatmentActions.Add(moveAction);
                            currentCityId = moveAction.TargetCity;
                            if (treatmentsByCity.Contains(currentCityId) && !visitedCities.Contains(currentCityId)) pathAndTreatmentActions.AddRange(treatmentsByCity[currentCityId]);
                            visitedCities.Add(currentCityId);
                        }
                    }
                    else
                    {
                        pathAndTreatmentActions.AddRange(bestPath.Actions);
                    }

                    queue.AddRange(pathAndTreatmentActions);
                    actionsLeft -= pathAndTreatmentActions.Count;
                }
                else
                {
                    actionsLeft = QueueFastestPathToTarget(ai, actionsLeft, queue, aiCanDiscard);
                }
            }
            else // Partner is already at the location
            {
                actionsLeft = QueueFastestPathToTarget(ai, actionsLeft, queue, aiCanDiscard);
            }

            // After moving, check if the AI can perform the share action.
            if (WillArriveAtTarget(ai, queue, currentPlanTargetLocation) && actionsLeft > 0)
            {
                QueueShareAction(ai, partner, queue);
                actionsLeft--;
            }
        }

        /// <summary>
        /// Queues the appropriate share or end-turn action.
        /// </summary>
        private void QueueShareAction(Player ai, Player partner, List<PlayerAction> queue)
        {
            if (partner.CurrentCity.CityCard.CityID == currentPlanTargetLocation)
            {
                // Give card to partner if they are the one needing it
                if (partner.Hand.Contains(currentPlanTargetLocation))
                    queue.Add(new ShareAction(currentPlanTargetLocation, partner.Panel, ai.Panel, ai.Panel));
                else // Take card from partner
                    queue.Add(new ShareAction(currentPlanTargetLocation, ai.Panel, partner.Panel, ai.Panel));
            }
            else
            {
                // Partner not in position, AI must end its turn.
                queue.Add(new EndTurnAction(ai));
            }
        }

        #endregion

        #region Utility & Helper Functions

        public List<int> FindCardsCanDiscard(Player player, Player other)
        {
            var cardsToDiscard = new List<int>();
            foreach (var color in PlayableColors)
            {
                var cardsOfColor = player.GetCardsInHand(color);
                if (cardsOfColor.Any() && (IsCuredColor(color) || other.GetCardsInHand(color).Count > 3))
                {
                    cardsToDiscard.AddRange(cardsOfColor);
                }
            }
            return cardsToDiscard;
        }

        public int SetShareLocation(Player ai, Player partner, List<int> aiCanDiscard, List<int> partnerCanDiscard)
        {
            double bestOption1Distance = double.PositiveInfinity, bestOption2Distance = double.PositiveInfinity;
            int bestOption1Location = -1, bestOption2Location = -1;

            var players = new[] { ai, partner };
            foreach (var giver in players)
            {
                Player receiver = (giver == ai) ? partner : ai;
                foreach (var color in PlayableColors)
                {
                    if (IsCuredColor(color)) continue;

                    int numGiver = giver.GetCardsInHand(color).Count;
                    if (numGiver == 0) continue;

                    int numReceiver = receiver.GetCardsInHand(color).Count;

                    foreach (int cityId in giver.GetCardsInHand(color))
                    {
                        int totalDist = BoardSearch.RouteConsideringCards(ai.CurrentCity.CityCard.CityID, cityId, aiCanDiscard, CityDrawer.CityScripts).Count +
                                        BoardSearch.RouteConsideringCards(partner.CurrentCity.CityCard.CityID, cityId, partnerCanDiscard, CityDrawer.CityScripts).Count;

                        if (((numReceiver == 3 && numGiver >= 1 && numGiver <= 3) || (numReceiver == 2 && (numGiver == 1 || numGiver == 2))) && partner.CurrentCity.CityCard.CityID == cityId)
                        {
                            return cityId; // Immediate best option
                        }

                        if (numReceiver == 3 && numGiver >= 1 && numGiver <= 3)
                        {
                            if (totalDist < bestOption1Distance)
                            {
                                bestOption1Distance = totalDist;
                                bestOption1Location = cityId;
                            }
                        }
                        else if (numReceiver == 2 && numGiver >= 1 && numGiver <= 2)
                        {
                            if (totalDist < bestOption2Distance)
                            {
                                bestOption2Distance = totalDist;
                                bestOption2Location = cityId;
                            }
                        }
                    }
                }
            }
            return bestOption1Location != -1 ? bestOption1Location : bestOption2Location;
        }

        public (int maxOutbreaks, List<int> outbreakCities) CountPotentialOutbreaks(Player ai, int[] aiCanDiscard, int[] cityIDs, int outbreakCounter, VirusName virus, HashSet<int> visited = null, List<int> path = null)
        {
            visited ??= new HashSet<int>();
            path ??= new List<int>();

            int maxOutbreaks = outbreakCounter;
            List<int> bestPath = new List<int>(path);

            foreach (int cityID in cityIDs)
            {
                if (visited.Contains(cityID)) continue;

                City city = CityDrawer.CityScripts[cityID];
                if (city.cubes[virus] == 3)
                {
                    var newVisited = new HashSet<int>(visited) { cityID };
                    var newPath = new List<int>(path) { cityID };

                    var (newCount, newBestPath) = CountPotentialOutbreaks(ai, aiCanDiscard, city.CityCard.Neighbors, outbreakCounter + 1, virus, newVisited, newPath);

                    if (newCount > maxOutbreaks)
                    {
                        maxOutbreaks = newCount;
                        bestPath = newBestPath;
                    }
                    else if (newCount == maxOutbreaks && newCount > 0)
                    {
                        (int timeToNewPath, _) = GetMinimumTimeToOutbreakLocation(newBestPath, ai.CurrentCity.CityCard.CityID, aiCanDiscard, CityDrawer.CityScripts, ai.ActionsRemaining);
                        (int timeToCurrentBestPath, _) = GetMinimumTimeToOutbreakLocation(bestPath, ai.CurrentCity.CityCard.CityID, aiCanDiscard, CityDrawer.CityScripts, ai.ActionsRemaining);

                        if (timeToNewPath < timeToCurrentBestPath)
                        {
                            bestPath = newBestPath;
                        }
                    }
                }
            }
            return (maxOutbreaks, bestPath);
        }

        private (int count, List<int> cities, VirusName color) FindMostCriticalOutbreakChain(Player ai, List<int> aiCanDiscard, int actionsRemaining)
        {
            return PlayableColors
                .Select(color =>
                {
                    var (count, cities) = CountPotentialOutbreaks(ai, aiCanDiscard.ToArray(), Enumerable.Range(0, 24).ToArray(), 0, color);
                    var (minTime, _) = GetMinimumTimeToOutbreakLocation(cities, ai.CurrentCity.CityCard.CityID, aiCanDiscard.ToArray(), CityDrawer.CityScripts, actionsRemaining);
                    return new { count, cities, color, minTime };
                })
                .OrderByDescending(x => x.count)
                .ThenBy(x => x.minTime)
                .Select(x => (x.count, x.cities, x.color))
                .First();
        }

        private (int time, int cityId) GetMinimumTimeToOutbreakLocation(List<int> path, int currentCityId, int[] usableCards, City[] cities, int maxDepth)
        {
            if (path == null || path.Count == 0) return (int.MaxValue, -1);

            return path.Select(destinationCityId => (time: BoardSearch.RouteConsideringCards(currentCityId, destinationCityId, usableCards, cities).Count, cityId: destinationCityId))
                       .OrderBy(t => t.time)
                       .FirstOrDefault();
        }

        public int GetClosestCityInfectedByColor(Player ai, VirusName virus, List<int> aiCanDiscard)
        {
            City closest = null;
            int minDist = int.MaxValue;
            int maxNeighborCubes = -1;

            foreach (City city in CityDrawer.CityScripts.Where(c => c.cubes[virus] > 0))
            {
                int d = BoardSearch.RouteConsideringCards(ai.CurrentCity.CityCard.CityID, city.CityCard.CityID, aiCanDiscard, CityDrawer.CityScripts).Count;
                if (d < minDist)
                {
                    minDist = d;
                    closest = city;
                    maxNeighborCubes = city.CityCard.Neighbors.Sum(n => CityDrawer.CityScripts[n].cubes[virus]);
                }
                else if (d == minDist)
                {
                    if (city.cubes[virus] > closest.cubes[virus])
                    {
                        closest = city;
                        maxNeighborCubes = city.CityCard.Neighbors.Sum(n => CityDrawer.CityScripts[n].cubes[virus]);
                    }
                    else if (city.cubes[virus] == closest.cubes[virus])
                    {
                        int currentNeighborCubes = city.CityCard.Neighbors.Sum(n => CityDrawer.CityScripts[n].cubes[virus]);
                        if (currentNeighborCubes > maxNeighborCubes)
                        {
                            closest = city;
                            maxNeighborCubes = currentNeighborCubes;
                        }
                    }
                }
            }
            return closest?.CityCard.CityID ?? -1;
        }

        private BoardSearch.PathResult getBestPathToTarget(List<BoardSearch.PathResult> possiblePathsToTarget, Player player)
        {
            var validPaths = possiblePathsToTarget;
            if (possiblePathsToTarget.Any(p => p.Num3CubeCitiesOnPath > 0))
            {
                if (player.Role == Player.Roles.ContainmentSpecialist && possiblePathsToTarget.Any(p => p.Actions.Count < player.ActionsRemaining) && player.CurrentCity.GetMaxNumberCubes() == 3)
                {
                    validPaths = possiblePathsToTarget.Where(p => p.Actions.Count < player.ActionsRemaining).ToList();
                    return validPaths.OrderByDescending(p => p.Num3CubeCitiesOnPath).OrderByDescending(p => p.Num2CubeCitiesOnPath).First();
                }
                return possiblePathsToTarget.Where(p => p.Num3CubeCitiesOnPath > 0).OrderByDescending(p => p.Num3CubeCitiesOnPath).OrderByDescending(p => p.Num2CubeCitiesOnPath).First();
            }
            else
            {
                if (player.Role == Player.Roles.ContainmentSpecialist && possiblePathsToTarget.Any(p => p.Actions.Count < player.ActionsRemaining))
                {
                    validPaths = possiblePathsToTarget.Where(p => p.Actions.Count < player.ActionsRemaining).ToList();
                }
                return validPaths.OrderByDescending(p => p.Num2CubeCitiesOnPath).OrderByDescending(p => p.SumCubesOnPathCities).First();
            }
        }

        private int QueueFastestPathToTarget(Player ai, int actionsLeft, List<PlayerAction> queue, List<int> aiCanDiscard)
        {
            var pathToTarget = BoardSearch.RouteConsideringCards(ai.CurrentCity.CityCard.CityID, currentPlanTargetLocation, aiCanDiscard, CityDrawer.CityScripts);
            foreach (var action in pathToTarget)
            {
                if (actionsLeft <= 0) break;
                queue.Add(action);
                actionsLeft--;
            }
            return actionsLeft;
        }

        private bool WillArriveAtTarget(Player ai, IReadOnlyCollection<PlayerAction> queue, int targetLocation)
        {
            if (ai.CurrentCity.CityCard.CityID == targetLocation) return true;
            return queue.Any() && queue.Last().TargetCity == targetLocation;
        }

        private List<PlayerAction> GetPrioritizedTreatmentsOnPath(IEnumerable<int> cityIds, Player ai, int budget)
        {
            var allTreatments = new List<Tuple<int, PlayerAction>>(); // Priority, Action

            foreach (int cityId in cityIds)
            {
                City city = CityDrawer.CityScripts[cityId];
                int cubeCount = city.GetMaxNumberCubes();
                if (cubeCount == 0) continue;

                VirusName virus = city.GetMaxVirusName();
                int treatableCount = (cubeCount >= 2 && ai.Role == Player.Roles.ContainmentSpecialist) ? cubeCount - 1 : cubeCount;

                for (int i = 0; i < treatableCount; i++)
                {
                    int priority = (cubeCount - i == 3) ? 3 : (cubeCount - i == 2) ? 2 : 1;
                    allTreatments.Add(new Tuple<int, PlayerAction>(priority, new TreatCubeAction(cityId, virus)));
                }
            }

            return allTreatments.OrderByDescending(t => t.Item1).Select(t => t.Item2).Take(budget).ToList();
        }

        #endregion
    }
}