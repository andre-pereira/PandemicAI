using OPEN.PandemicAI;
using static OPEN.PandemicAI.Enums;

public static class InstructionBuilder
{
    public static string Build(Player p,
                               CardGUIStates cardState,
                               ActionType currentAction,
                               CardSelectionLogic sel)
    {
        if (p != GameRoot.State.CurrentPlayer || GameRoot.State.CurrentState != GameState.PlayerActions)
            return cardState == CardGUIStates.CardsDiscarding
                   ? "Discard a card"
                   : "Not your turn.";

        string msg = $"<b>{p.ActionsRemaining}</b> actions left.";
        string extra = "";

        switch (cardState)
        {
            case CardGUIStates.CardsDiscarding:
                extra = sel.Selected.Count == 0 ? "Select to discard" : "Discard city";
                break;

            case CardGUIStates.CardsExpanded:
                extra = "Close to see actions.";
                break;

            case CardGUIStates.CardsExpandedFlyActionToSelect:
                extra = "Pick a card to fly to";
                break;


            case CardGUIStates.CardsExpandedFlyActionSelected:
                extra = "Complete Action?";
                break;

            case CardGUIStates.CardsExpandedCharterActionToSelect:
                extra = "Move pawn to any city";
                break;

            case CardGUIStates.CardsExpandedCureActionToSelect:
                extra = "Select 4 cards";
                break;
        }

        switch (currentAction)
        {
            case ActionType.Move:
                extra = $"Move pawn up to {p.ActionsRemaining} square" +
                        (p.ActionsRemaining == 1 ? "" : "s") + ".";
                break;

            case ActionType.Treat:
                extra = "Pick a cube on your city.";
                break;

            case ActionType.Share:
                extra = "Complete action?";
                break;

            case ActionType.FindCure when sel.Selected.Count >= 4:
                extra = "Complete action?";
                break;
        }

        if (!string.IsNullOrEmpty(extra)) msg += "\n" + extra;
        return msg;
    }
}
