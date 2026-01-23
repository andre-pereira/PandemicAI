using OPEN.PandemicAI;
using static OPEN.PandemicAI.Enums;
using System;
using UnityEngine;
using UnityEngine.UI;

public class ActionPanelView : MonoBehaviour
{
    [SerializeField]
    Button [] Buttons;

    public event Action<ActionType> Clicked;
    static readonly Color dim = new(.3f, .3f, .3f, .3f);
    static readonly Color on = new(1f, 1f, 1f, .5f);
    static readonly Color selected = new(1f, 1f, 1f, 1f);
    readonly Image[] images = new Image[7];
    private int SelectedButtonIndex = -1; //used to track the currently selected button
    private ActionState[] buttonStates = new ActionState[7];
    private EligibilityFlags[] AllFlags = new EligibilityFlags[]
{
        EligibilityFlags.Move,
        EligibilityFlags.Treat,
        EligibilityFlags.DirectFlight,
        EligibilityFlags.CharterFlight,
        EligibilityFlags.Share,
        EligibilityFlags.Cure,
        EligibilityFlags.EndTurn
};


    private enum ActionState { disabled, unavailable, available};

    void Awake()
    {
        // Get the parent PlayerPanel component
        PlayerPanel parentPanel = GetComponentInParent<PlayerPanel>();

        for (int i = 0; i < buttonStates.Length; i++)
        {
            int index = i; //necessary to create a local copy for the closure
            Buttons[i].onClick.AddListener(() => Click(index));
            images[i] = Buttons[i].GetComponent<Image>();

            if (parentPanel.IsAIControlled)
            {
                FurhatGameMap.Instance.SetItemPlacement($"{AllFlags[i]}", Buttons[i].GetComponent<RectTransform>());
            }
            
            // Debug.Log($"ActionPanelView: Button {AllFlags[i]} initialized at index {i}.");
        }
    }

    public void Refresh(EligibilityFlags f)
    {
        bool enableActions = f != EligibilityFlags.None;
        bool cardsExpanded = SelectedButtonIndex > 1 && SelectedButtonIndex < 6;
        int i = -1;
        foreach (var flag in AllFlags)
        {
            i++;
            if(!enableActions || (cardsExpanded && SelectedButtonIndex != i))
            {
                buttonStates[i] = ActionState.disabled;
                Buttons[i].gameObject.SetActive(false);
                continue;
            }
            Buttons[i].gameObject.SetActive(true);
            if(f.HasFlag(flag))
            {    
                buttonStates[i] = ActionState.available;
                if (SelectedButtonIndex == i)
                    images[i].color = selected;
                else
                    images[i].color = on;
            }
            else
            {
                buttonStates[i] = ActionState.unavailable;
                images[i].color = dim;
            }

        }
    }

    public void Highlight(ActionType t)
    {
        if (SelectedButtonIndex == (int)t || t == ActionType.None)
            SelectedButtonIndex = -1;
        else
            SelectedButtonIndex = (int)t;

        //Refresh(GameRoot.State.PossibleActions);
    }

    void Click(int t)
    {
        //Debug.Log($"ActionPanelView: Clicked button {t}");
        var currentButtonState = buttonStates[t];
        if (currentButtonState == ActionState.available)
            Clicked?.Invoke((ActionType) t);
    }



}
