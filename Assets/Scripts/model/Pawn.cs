using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static OPEN.PandemicAI.Enums;

namespace OPEN.PandemicAI
{
    /// <summary>
    /// Represents a pawn in the game, which can be dragged and clicked.
    /// </summary>
    public class Pawn : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler//, IPointerClickHandler
    {
        private Canvas citiesCanvas;
        private Canvas canvas;
        private Vector2 offset;
        public RectTransform rectTransform;
        public bool CanMove = false;
        public bool IsInterfaceElement = false;
        private Vector2 initialPosition;
        private int initialCityID;

        private City endedInCity = null;
        public Player.Roles PawnRole;

        public Player PlayerModel;

        private void Awake()
        {
            offset = new Vector2(0, 0);
            rectTransform = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();
            citiesCanvas = GameGUI.CityCanvas.GetComponent<Canvas>();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (CanMove)
            {
                Vector2 pointerPosition = eventData.position;
                Vector2 localPointerPosition;
                endedInCity = null;

                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, pointerPosition, canvas.worldCamera, out localPointerPosition))
                {
                    rectTransform.localPosition = localPointerPosition - offset;
                }

                GraphicRaycaster raycaster = citiesCanvas.GetComponent<GraphicRaycaster>();
                List<RaycastResult> results = new List<RaycastResult>();
                raycaster.Raycast(eventData, results);

                foreach (RaycastResult result in results)
                {
                    if (result.gameObject.name == "Image")
                    {
                        gameObject.transform.position = result.gameObject.transform.position;
                        endedInCity = result.gameObject.GetComponentInParent<City>();
                    }
                }
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            PlayerPanel panel = PlayerAreaDrawer.PlayerPadForPlayer(PlayerModel);

            if (endedInCity != null && endedInCity.CityCard.CityID != PlayerModel.GetCurrentCityId())
            {
                if (panel.ActionSelected == ActionType.Charter)
                {
                    Timeline.Instance.AddEvent(new PCharterEvent(endedInCity.CityCard.CityID));
                    Destroy(gameObject);
                }
                else
                {
                    int distance = BoardSearch.Distance(PlayerModel.GetCurrentCityId(), endedInCity.CityCard.CityID, CityDrawer.CityScripts);
                    if (panel.ActionSelected == ActionType.Move)
                    {
                        if (distance > 0 && distance <= panel.Model.ActionsRemaining)
                        {
                            Timeline.Instance.AddEvent(new PMoveEvent(endedInCity.CityCard.CityID, distance));
                            Destroy(gameObject);
                        }
                        else rectTransform.localPosition = initialPosition;
                    }
                }
            }
            else rectTransform.localPosition = initialPosition;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            initialPosition = rectTransform.localPosition;
            initialCityID = PlayerModel.GetCurrentCityId();
        }

        internal void SetRoleAndPlayer(Player player)
        {
            PawnRole = player.Role;
            PlayerModel = player;
            GetComponent<Image>().color = GameRoot.Catalog.RoleCards[(int)PawnRole].roleColor;
        }
    }
}
