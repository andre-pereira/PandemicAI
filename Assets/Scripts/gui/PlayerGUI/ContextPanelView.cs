using System;
using UnityEngine;
using UnityEngine.UI;
using static OPEN.PandemicAI.Enums;

namespace OPEN.PandemicAI
{
    public enum ContextButton { Reject, Accept, Discard }

    public class ContextPanelView : MonoBehaviour
    {
        [SerializeField] Button rejectBtn;
        [SerializeField] Button acceptBtn;
        [SerializeField] Button discardBtn;

        public event Action<ContextButton> Pressed;

        void Awake()
        {
            rejectBtn.onClick.AddListener(() => Pressed?.Invoke(ContextButton.Reject));
            acceptBtn.onClick.AddListener(() => Pressed?.Invoke(ContextButton.Accept));
            discardBtn.onClick.AddListener(() => Pressed?.Invoke(ContextButton.Discard));
        }

        public void SetState(bool showReject, bool showAccept, bool showDiscard)
        {
            // Get the parent PlayerPanel component
            PlayerPanel parentPanel = GetComponentInParent<PlayerPanel>();
            
            rejectBtn.gameObject.SetActive(showReject);
            acceptBtn.gameObject.SetActive(showAccept);
            discardBtn.gameObject.SetActive(showDiscard);
            if (parentPanel.IsAIControlled)
            {
                FurhatGameMap.Instance.SetItemPlacement("reject", rejectBtn.GetComponent<RectTransform>());
                FurhatGameMap.Instance.SetItemPlacement("accept", acceptBtn.GetComponent<RectTransform>());
                FurhatGameMap.Instance.SetItemPlacement("discard", discardBtn.GetComponent<RectTransform>());
            }
        }

        public void Refresh(CardGUIStates cardState, CardSelectionLogic selection, bool isCurrentPlayer)
        {
            bool showReject = false;
            bool showAccept = false;
            bool showDiscard = false;

            switch (cardState)
            {
                case CardGUIStates.None:
                    break;

                case CardGUIStates.CardsExpandedShareAction:
                    if (isCurrentPlayer)
                    {
                        showReject = true;
                        showAccept = true;
                    }
                    break;

                case CardGUIStates.CardsExpandedCharterActionToSelect:
                case CardGUIStates.CardsExpanded:
                case CardGUIStates.CardsExpandedFlyActionToSelect:
                case CardGUIStates.CardsExpandedCureActionToSelect:
                    showReject = true;
                    break;

                case CardGUIStates.CardsDiscarding:
                    if (selection.Selected.Count > 0) showDiscard = true;
                    break;
            }

            SetState(showReject, showAccept, showDiscard);
        }
    }
}
