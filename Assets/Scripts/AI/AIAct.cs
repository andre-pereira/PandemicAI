using System.Collections.Generic;
using System.Linq;
using static OPEN.PandemicAI.Enums;


namespace OPEN.PandemicAI
{
    // Abstract base class for all actions
    public abstract class PlayerAction
    {

        public ActionType Type;
        public int TargetCity = -1;
        public VirusName TargetVirus = VirusName.None;
        public string Description;
        // execute action
        public virtual void Execute()
        {
        }
    }

    // Share action
    public class ShareAction : PlayerAction
    {
        public PlayerPanel PlayerFrom;
        public PlayerPanel PlayerTo;
        public PlayerPanel ai;

        public ShareAction(int targetCity, PlayerPanel playerFrom, PlayerPanel playerTo, PlayerPanel ai) : base()
        {
            Type = ActionType.Share;
            TargetCity = targetCity;
            PlayerFrom = playerFrom;
            PlayerTo = playerTo;
            Description = $"{playerFrom.Model.Name} shares {CityDrawer.CityScripts[targetCity].CityCard.CityName} with {playerTo.Model.Name}.";
            this.ai = ai;
        }

        public override void Execute()
        {
            // Add the event to the timeline
            //FurhatGameMap.Instance.SetItemPlacement("ShareAction", ai.ShareAction.GetComponent<RectTransform>());
            //Timeline.Instance.AddEvent(new GShareActionClicked(FurhatGameMap.Instance.GetItemPlacement("ShareAction").position));
            PShareKnowledge pShare = new PShareKnowledge(PlayerFrom.Model, PlayerTo.Model, true);
            Timeline.Instance.AddEvent(pShare);

        }
    }

    // Move action
    public class MoveAction : PlayerAction
    {
        public MoveAction(int targetCity) : base()
        {
            Type = ActionType.Move;
            TargetCity = targetCity;
            Description = $"Moves to {CityDrawer.CityScripts[targetCity].CityCard.CityName}.";
            if (GameRoot.State.CurrentPlayer.Role == Player.Roles.ContainmentSpecialist && CityDrawer.CityScripts[targetCity].GetMaxNumberCubes() >= 2)
            {
                Description += $" Uses special ability to automatically treat a cube in {CityDrawer.CityScripts[targetCity].CityCard.CityName}.";
            }
        }

        public override void Execute()
        {
            Timeline.Instance.AddEvent(new PMoveEvent(TargetCity, 1));
        }
    }

    // Fly action
    public class FlyAction : PlayerAction
    {

        public FlyAction(int targetCity) : base()
        {
            Type = ActionType.Fly;
            TargetCity = targetCity;
            Description = $"Flies to {CityDrawer.CityScripts[targetCity].CityCard.CityName} by discarding {CityDrawer.CityScripts[targetCity].CityCard.CityName} card.";
        }

        public override void Execute()
        {
            Timeline.Instance.AddEvent(new PFlyToCity(TargetCity));
        }
    }

    // Charter action
    public class CharterAction : PlayerAction
    {
        public int SourceCity;

        public CharterAction(int sourceCity, int targetCity) : base()
        {
            Type = ActionType.Charter;
            TargetCity = targetCity;
            SourceCity = sourceCity;
            Description = $"Charters to {CityDrawer.CityScripts[targetCity].CityCard.CityName} by discarding {CityDrawer.CityScripts[SourceCity].CityCard.CityName} card.";
        }

        public override void Execute()
        {
            Timeline.Instance.AddEvent(new PCharterEvent(CityDrawer.CityScripts[TargetCity].CityCard.CityID));
        }
    }

    // Find cure action
    public class FindCureAction : PlayerAction
    {
        public List<int> SelectedCards = new List<int>();

        public FindCureAction(int targetCity, VirusName targetVirus, List<int> selectedCards) : base()
        {
            Type = ActionType.FindCure;
            TargetCity = targetCity;
            TargetVirus = targetVirus;
            SelectedCards = selectedCards;
            Description = $"Finds a cure for {targetVirus} in {CityDrawer.CityScripts[targetCity].CityCard.CityName} by discarding {string.Join(", ", selectedCards.Select(card => CityDrawer.CityScripts[card].CityCard.CityName))} cards.";

        }

        // Execute method for MoveAction
        public override void Execute()
        {
            Timeline.Instance.AddEvent(new PCureDisease(SelectedCards));
        }
    }

    // Treat cube action
    public class TreatCubeAction : PlayerAction
    {
        public TreatCubeAction(int targetCity, VirusName targetVirus) : base()
        {
            Type = ActionType.Treat;
            TargetCity = targetCity;
            TargetVirus = targetVirus;
            Description = $"Treats {targetVirus} in {CityDrawer.CityScripts[targetCity].CityCard.CityName}.";
        }
        public override void Execute()
        {
            Timeline.Instance.AddEvent(new PTreatDisease(TargetCity, TargetVirus));
        }
    }

    // End turn action
    public class EndTurnAction : PlayerAction
    {
        public Player player;
        public EndTurnAction(Player player)
        {
            Type = ActionType.EndTurn;
            this.player = player;
            Description = $"{player.Name} ends their turn.";
        }

        public override void Execute()
        {
            player.DecreaseActionsRemaining(player.ActionsRemaining);
            Timeline.Instance.AddEvent(new GActionButtonClicked(ActionType.EndTurn));
        }
    }

    // Discard card action
    public class DiscardCardAction : PlayerAction
    {
        public int TargetCard;
        public PlayerPanel CurrentPlayer;
        public DiscardCardAction(int targetCard, PlayerPanel currentPlayer) : base()
        {
            Type = ActionType.None;
            TargetCard = targetCard;
            CurrentPlayer = currentPlayer;
            Description = $"{currentPlayer.Model.Name} discards {CityDrawer.CityScripts[targetCard].CityCard.CityName} card.";
        }

        public override void Execute()
        {
            Timeline.Instance.AddEvent(new PDiscardCard(TargetCard, CurrentPlayer.Model, false));
        }
    }

}

