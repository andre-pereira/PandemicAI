using System.Linq;
using UnityEditor;
using UnityEngine;
using static OPEN.PandemicAI.Enums;

namespace OPEN.PandemicAI
{

    public sealed class ActionController
    {
        public readonly Player model;
        readonly Player partnerModel;
        readonly PawnAnimator animator;
        readonly CardSelectionLogic selection;

        public CardGUIStates CardState = CardGUIStates.None;
        public ActionType ActionSelected = ActionType.None;

        public ActionController(Player m, Player partner, PawnAnimator a, CardSelectionLogic s)
        {
            model = m;
            partnerModel = partner;
            animator = a;
            selection = s;
        }

        public void Execute(ActionType a)
        {
            ActionSelected = a;
            switch (a)
            {
                case ActionType.Move: Move(); break;
                case ActionType.Fly: Fly(); break;
                case ActionType.Charter: Charter(); break;
                case ActionType.Treat: Treat(); break;
                case ActionType.Share: Share(); break;
                case ActionType.FindCure: Cure(); break;
                case ActionType.EndTurn: EndTurn(); break;
            }
        }

        private void Cure()
        {
            CardState = CardGUIStates.CardsExpandedCureActionToSelect;
            Timeline.Instance.AddEvent(new GCureActionClicked(Input.mousePosition));
        }

        private void Share()
        {
            CardState = CardGUIStates.CardsExpandedShareAction;
            // add the correct card to the selection of the player that has the city card, could be the partner or the current player
            if (model.Hand.Contains(model.GetCurrentCityId()))
                selection.AddSingleCardToSelection(model.GetCurrentCityId());
            else
            {
                partnerModel.Panel.AddShareCardToSelection(model.GetCurrentCityId());
                GUIEvents.RaiseDrawPlayerArea(partnerModel.Panel);
            }
            Timeline.Instance.AddEvent(new GShareActionClicked(Input.mousePosition));
        }

        private void Treat()
        {
            Timeline.Instance.AddEvent(new GTreatActionClicked(Input.mousePosition)); 
        }

        private void Charter()
        {
            CardState = CardGUIStates.CardsExpandedCharterActionToSelect;
            selection.AddSingleCardToSelection(model.GetCurrentCityId());
            animator.CreateMovingPawn();
            Timeline.Instance.AddEvent(new GCharterActionClicked(Input.mousePosition));
        }

        private void Fly()
        {
            CardState = CardGUIStates.CardsExpandedFlyActionToSelect;
            Timeline.Instance.AddEvent(new GFlyActionClicked(Input.mousePosition));
        }

        private void Move()
        {
            animator.CreateMovingPawn();
            Timeline.Instance.AddEvent(new GMoveActionClicked(Input.mousePosition));
        }

        private void EndTurn()
        {
            Timeline.Instance.AddEvent(new GEndTurnActionClicked(Input.mousePosition));
            model.DecreaseActionsRemaining(model.ActionsRemaining);
        }

        public void ClearTransientUI()
        {
            selection.Clear();
            animator.DestroyMovingPawn();
            animator.DestroyFlyLine();
            CardState = CardGUIStates.None;
        }


        public void ContextAccept()
        {
            if (CardState == CardGUIStates.CardsExpandedFlyActionSelected)
            {
                Timeline.Instance.AddEvent(new PFlyToCity(selection.Selected[0]));
            }
            else if (CardState == CardGUIStates.CardsExpandedCureActionSelected)
            {
                Timeline.Instance.AddEvent(new PCureDisease(selection.Selected));
            }
            else if (CardState == CardGUIStates.CardsExpandedShareAction)
            {
                bool curPlayerHasCity = model.Hand.Contains(model.GetCurrentCityId());

                if (curPlayerHasCity)
                    Timeline.Instance.AddEvent(new PShareKnowledge(model, partnerModel, false));
                else
                    Timeline.Instance.AddEvent(new PShareKnowledge(partnerModel, model, false));
            }
        }

        public void ContextDiscard()
        {
            if (selection.Selected.Count > 0)
                Timeline.Instance.AddEvent(new PDiscardCard(selection.Selected[0], model, true));
        }

        public void ChangeCardState(CardGUIStates newState)
        {
            CardState = newState;
        }

        internal void ChangeActionState(ActionType type)
        {
            ActionSelected = type;
        }
    }
}