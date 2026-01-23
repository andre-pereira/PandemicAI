using OPEN.PandemicAI;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static OPEN.PandemicAI.Enums;

public class PlayerPanel : MonoBehaviour
{
    [SerializeField] public PlayerHandView hand;
    [SerializeField] ActionPanelView actionsView;
    [SerializeField] ContextPanelView context;
    [SerializeField] InstructionPresenter instruction;
    [SerializeField] public PawnAnimator pawnAnimator;
    [SerializeField] public Image Background;
    [SerializeField] public Outline Outline;
    [SerializeField] public bool IsAIControlled = false;

    CardSelectionLogic selection = new();
    ActionController ActionControl;
    public CardGUIStates CardState => ActionControl.CardState;
    public ActionType ActionSelected => ActionControl.ActionSelected;
    public Player Model => ActionControl.model;

    [SerializeField] TextMeshProUGUI playerNameText;
    public void SetPlayerNameText(string name) => playerNameText.text = name;

    public void Init(Player p, Player partner)
    {
        ActionControl = new ActionController(p, partner, pawnAnimator, selection);
        pawnAnimator.Init(p);
        GUIEvents.EligibilityChanged += OnEligibility;
        GUIEvents.CubeClicked += OnCubeClick;
        hand.CardClicked += OnCardClicked;
        actionsView.Clicked += OnActionClicked;
        context.Pressed += OnContext;
        GameEvents.StateChanged += OnGameStateChanged;
        SetPlayerNameText(p.Name);
        Color rColer = p.GetRoleCard().roleColor;
        Background.color = new Color(rColer.r, rColer.g, rColer.b, rColer.a / 3);
    }

    public void OnContext(ContextButton btn)
    {
        switch (btn)
        {
            case ContextButton.Reject: ClearSelectedAction(false); break;
            case ContextButton.Accept: ActionControl.ContextAccept(); break;
            case ContextButton.Discard: ActionControl.ContextDiscard(); break;
        }
        ActionControl.ClearTransientUI();
        Draw();
    }

    private void OnActionClicked(ActionType a)
    {
        ActionControl.ClearTransientUI();

        if (ActionSelected == a)
            ActionControl.ChangeActionState(ActionType.None);
        else
            ActionControl.Execute(a);

        actionsView.Highlight(ActionSelected);
        Draw();
        Timeline.Instance.AddEvent(new GActionButtonClicked(a));
    }


    private void OnCardClicked(int city)
    {
        Timeline.Instance.AddEvent(new GCityCardClicked(city, Input.mousePosition));

        if (CardState != CardGUIStates.CardsDiscarding &&
            (GameRoot.State.CurrentState != GameState.PlayerActions || Model != GameRoot.State.CurrentPlayer))
            return;

        if (CardState == CardGUIStates.None)
        {
            ActionControl.ClearTransientUI();
            ActionControl.ChangeActionState(ActionType.None);
            ActionControl.ChangeCardState(CardGUIStates.CardsExpanded);
            Draw();
        }
        else
        {
            if (selection.Toggle(city, CardState, Model))
            {
                if (CardState == CardGUIStates.CardsExpandedFlyActionToSelect
                    || CardState == CardGUIStates.CardsExpandedFlyActionSelected)
                {
                    if (selection.Selected.Count == 0)
                    {
                        ActionControl.ClearTransientUI();
                        ActionControl.ChangeCardState(CardGUIStates.CardsExpandedFlyActionToSelect);
                        context.SetState(true, false, false);
                    }
                    else
                    {
                        ActionControl.ChangeCardState(CardGUIStates.CardsExpandedFlyActionSelected);
                        pawnAnimator.CreateFlyPreviewLine(city);
                        context.SetState(true, true, false);
                    }

                }
                else if (CardState == CardGUIStates.CardsExpandedCureActionToSelect || CardState == CardGUIStates.CardsExpandedCureActionSelected)
                {
                    if (selection.CurePossible(Model))
                    {
                        context.SetState(true, true, false);
                        ActionControl.ChangeCardState(CardGUIStates.CardsExpandedCureActionSelected);
                    }
                    else
                    {
                        context.SetState(true, false, false);
                        ActionControl.ChangeCardState(CardGUIStates.CardsExpandedCureActionToSelect);
                    }
                }
                else if (CardState == CardGUIStates.CardsDiscarding)
                {
                    if (selection.Selected.Count == 0)
                    {
                        context.SetState(false, false, false);
                    }
                    else
                    {
                        context.SetState(false, false, true);
                    }
                }
                hand.SetHand(Model.Hand, selection.Selected);
                RefreshInstructions();
            }
        }
    }

    private void OnCubeClick(City city, VirusName virusName)
    {
        if (Model != GameRoot.State.CurrentPlayer) return;
        if (ActionSelected == ActionType.Treat &&
                Model.GetCurrentCityId() == city.CityCard.CityID &&
                Model.ActionsRemaining > 0 && city.HasCubes())
        {
            Timeline.Instance.AddEvent(new PTreatDisease(city.CityCard.CityID, virusName));
        }
    }

    void OnEligibility(EligibilityFlags f)
    {
        if (Model != GameRoot.State.CurrentPlayer) return;
        actionsView.Refresh(f);
    }

    public void OnGameStateChanged(GameState state)
    {
        if (state == GameState.PlayerActions)
        {
            RefreshInstructions();
            if (Model != GameRoot.State.CurrentPlayer)
            {
                actionsView.Refresh(EligibilityFlags.None);
            }
        }
        else if (state == GameState.Discarding)
        {
            if (Model.Hand.Count > 6)
            {
                Timeline.Instance.AddEvent(new GCardStateChange(CardGUIStates.CardsDiscarding, this));
                ActionControl.ChangeCardState(CardGUIStates.CardsDiscarding);
                // I have added this:
                ActionControl.ChangeActionState(ActionType.None);
            }
        }
    }

    public void Draw()
    {
        bool isMyTurn = Model == GameRoot.State.CurrentPlayer;
        hand.SetHand(Model.Hand, selection.Selected);

        int preset =
            Model.Hand.Count > 6 ? 0 :
            (CardState != CardGUIStates.None ||
             Model != GameRoot.State.CurrentPlayer) ? 1 : 2;

        hand.ApplyPreset(preset);

        if (GameRoot.Config.UseFurhat && Model.Role == Player.Roles.QuarantineSpecialist)
        {
            hand.CommunicateHandPositionToFurhat();
        }

        bool drawActions = isMyTurn && CardState != CardGUIStates.CardsExpanded && GameRoot.State.CurrentState != GameState.Discarding;
        actionsView.Refresh(drawActions ? GameRoot.State.PossibleActions : EligibilityFlags.None);

        RefreshInstructions();

        context.Refresh(CardState, selection, Model == GameRoot.State.CurrentPlayer);

        if (GameRoot.State.CurrentState == GameState.PlayerActions && isMyTurn)
            Outline.enabled = true;
        else
            Outline.enabled = false;
    }

    private void RefreshInstructions()
    {
        instruction.Show(InstructionBuilder.Build(
                      Model, CardState, ActionControl.ActionSelected, selection));
    }

    public void ClearSelectedAction(bool clearCardsState, int actionsRemaining = 0)
    {
        if (clearCardsState)
            ActionControl.ChangeCardState(CardGUIStates.None);

        if (ActionSelected != ActionType.Treat || !Model.CurrentCity.HasCubes() || actionsRemaining == 0)
        {
            ActionControl.ChangeActionState(ActionType.None);
            actionsView.Highlight(ActionSelected);
        }
    }

    public void AddShareCardToSelection(int cityId)
    {
        selection.AddSingleCardToSelection(cityId);
    }

    public Transform GetLastCardTransform()
    {
        if (hand.pool.Count == 0) return null;
        var lastCard = hand.GetLastActiveCard();
        if (lastCard == null) return null;
        return lastCard.transform;
    }
}
