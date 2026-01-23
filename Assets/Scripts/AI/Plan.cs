using System;
using System.Collections.Generic;
using System.Linq;
using static OPEN.PandemicAI.Enums;

namespace OPEN.PandemicAI
{
    public class Plan
    {
        public enum PlanPriorities { None, ShareKnowledge, FindCure, SafeguardOutbreak, SafeguardCubeSupply, ManagingDisease }

        public PlanPriorities PlanPriority;

        public int TargetCity { get; }
        public VirusName TargetColor { get; }

        public List<PlayerAction> ActionQueue { get; }

        public string PlanExplanation { get; }

        public Plan(PlanPriorities currentPlanPriority, int currentPlanTargetLocation, VirusName currentPlanTargetColor, List<PlayerAction> actionQueue, string planExplanation = "")
        {
            if (actionQueue == null || actionQueue.Count == 0)
            {
                throw new ArgumentException("Action queue cannot be null or empty.", nameof(actionQueue));
            }

            // Ensure the first action in the queue has a valid target city
            if (actionQueue[0].TargetCity == -1)
            {
                actionQueue[0].TargetCity = currentPlanTargetLocation;
            }

            // Set properties
            {
                PlanPriority = currentPlanPriority;
                TargetCity = currentPlanTargetLocation;
                TargetColor = currentPlanTargetColor;
                ActionQueue = actionQueue;
                PlanExplanation = planExplanation;
            }
        }

        //create a toString method that returns the name of the plan
        public override string ToString()
        {
            return PlanExplanation;
        }

        public void ExecuteFirstAction()
        {
            ActionQueue[0].Execute();
            ActionQueue.RemoveAt(0);
        }
    }
}