using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace OPEN.PandemicAI
{
    public class FurhatGameExecutor : MonoBehaviour
    {
        public float PawnDragDuration = 0.5f;
        public static string PendingClickTarget = "";
        public static string PendingDragTarget = "";
        private bool isDragging = false;

        private Queue<string> clickQueue = new Queue<string>();
        private bool isProcessingClicks = false;

        void Update()
        {
            if (Furhat.Instance == null)
                return;
            if (isDragging) return;

            // New: Queue incoming click request
            if (!string.IsNullOrEmpty(PendingClickTarget))
            {
                Furhat.Instance.LogCommand("Pending Click", PendingClickTarget);
                //Furhat.Instance.timeSinceHandledEvent.Restart();
                clickQueue.Enqueue(PendingClickTarget);
                PendingClickTarget = "";

                if (!isProcessingClicks)
                    StartCoroutine(ProcessClickQueue());
            }

            if (!string.IsNullOrEmpty(PendingDragTarget))
            {
                Furhat.Instance.LogCommand("Pending Drag", PendingDragTarget);
                string targetId = PendingDragTarget;
                PendingDragTarget = "";
                isDragging = true;
                StartCoroutine(DelayedDrag(targetId));
            }
        }

        // New: Coroutine to process queued clicks with delay
        private IEnumerator ProcessClickQueue()
        {
            isProcessingClicks = true;

            while (clickQueue.Count > 0)
            {
                string nextClick = clickQueue.Dequeue();
                ProcessClickRequest(nextClick);
                yield return new WaitForSeconds(0.5f);
            }

            isProcessingClicks = false;
        }

        private void ProcessClickRequest(string targetId)
        {
            RectTransform target = FurhatGameMap.Instance.GetItemPlacement(targetId);
            if (target != null)
            {
                Furhat.Instance.LogCommand("Simulate Click", targetId);
                SimulateClick(target);
            }
        }

        private void ProcessDragRequest(string targetId)
        {
            RectTransform target = FurhatGameMap.Instance.GetItemPlacement(targetId);
            if (target != null)
            {
                Furhat.Instance.LogCommand("Simulate Drag", targetId);
                SimulateDrag(target);
            }
        }

        private void SimulateClick(RectTransform clickTarget)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                Debug.LogError("No EventSystem found in the scene");
                return;
            }

            Vector2 worldCenter = clickTarget.TransformPoint(clickTarget.rect.center);
            Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(Camera.main, worldCenter);

            PointerEventData pointerData = new PointerEventData(eventSystem);
            pointerData.position = screenPosition;

            ExecuteEvents.Execute(clickTarget.gameObject, pointerData, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(clickTarget.gameObject, pointerData, ExecuteEvents.pointerClickHandler);
            ExecuteEvents.Execute(clickTarget.gameObject, pointerData, ExecuteEvents.pointerUpHandler);
            Furhat.Instance.GazeAtClick(screenPosition);

            ClickEffectUI.Instance.ShowClickEffectAt(screenPosition);
        }
        private void SimulateDrag(RectTransform dragTarget)
        {
            Pawn pawnToDrag = FindObjectsByType<Pawn>(FindObjectsSortMode.None)
                .FirstOrDefault(p => p.PlayerModel == GameRoot.State.CurrentPlayer);

            if (pawnToDrag == null)
            {
                Debug.LogWarning("No pawn found for current player.");
                return;
            }

            StartCoroutine(AnimateDragCoroutine(pawnToDrag, dragTarget, PawnDragDuration));
        }

        private IEnumerator AnimateDragCoroutine(Pawn pawnToDrag, RectTransform dragTarget, float duration)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null) yield break;

            GameObject pawnObj = pawnToDrag.gameObject;
            RectTransform source = pawnToDrag.rectTransform;

            Vector2 startScreenPos = RectTransformUtility.WorldToScreenPoint(Camera.main, source.position);
            Vector2 targetScreenPos = RectTransformUtility.WorldToScreenPoint(Camera.main, dragTarget.position);

            PointerEventData pointerData = new PointerEventData(eventSystem)
            {
                pointerId = -1,
                position = startScreenPos,
                pressPosition = startScreenPos,
                clickCount = 1,
                button = PointerEventData.InputButton.Left,
                dragging = true
            };

            ClickEffectUI.Instance.ShowClickEffectAt(startScreenPos);

            ExecuteEvents.Execute(pawnObj, pointerData, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(pawnObj, pointerData, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(pawnObj, pointerData, ExecuteEvents.beginDragHandler);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                pointerData.position = Vector2.Lerp(startScreenPos, targetScreenPos, t);
                ExecuteEvents.Execute(pawnObj, pointerData, ExecuteEvents.dragHandler);
                yield return null;
            }

            pointerData.position = targetScreenPos;
            ExecuteEvents.Execute(pawnObj, pointerData, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(pawnObj, pointerData, ExecuteEvents.endDragHandler);

            ClickEffectUI.Instance.ShowClickEffectAt(targetScreenPos);

            Furhat.Instance.DragEnded();
        }
        private IEnumerator DelayedDrag(string targetId)
        {
            yield return null;
            ProcessDragRequest(targetId);
            isDragging = false;
        }
    }
}
