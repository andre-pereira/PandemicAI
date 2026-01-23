using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace OPEN.PandemicAI
{
    public static class AnimationTemplates
    {
        private const float scaleToCenterScale = 2.5f;
        private static float durationMove = 1.0f / GameRoot.Config.AnimationTimingMultiplier;
        private static float durationShakes = 0.25f / GameRoot.Config.AnimationTimingMultiplier;
        public static bool HighlightCardAndMoveDone = false;

        public static Sequence MoveToPosition(GameObject objectToAnimate, Transform finalLocation, bool destroyOnComplete = false, bool drawBoardOnComplete = false)
        {
            Sequence sequence = DOTween.Sequence();
            sequence.Append(objectToAnimate.transform.DOMove(finalLocation.position, durationMove))
                .OnUpdate(() =>
                {
                    if (Furhat.Instance != null)
                        Furhat.Instance.NotifyObjectOnScreenMoved(objectToAnimate.transform);
                })
                .AppendCallback(() =>
                {
                    if (drawBoardOnComplete)
                    {
                        GUIEvents.RaiseDrawBoard();
                    }
                    if (destroyOnComplete)
                    {
                        Object.Destroy(objectToAnimate);
                    }
                });
            return sequence;
        }

        public static Sequence HighlightCardAndMove(GameObject objectToAnimate, Transform finalLocation, bool maintainZoom = false, bool rotateToFinalLocation = true, bool destroyOnComplete = false, bool drawBoardOnComplete = false)
        {
            HighlightCardAndMoveDone = false;
            Sequence sequence = DOTween.Sequence();

            // Step 1: Shake the rotation for a subtle highlight effect.
            sequence.Append(objectToAnimate.transform.DOShakeRotation(
                durationShakes, new Vector3(0f, 0f, scaleToCenterScale), 10, 90, false));

            // Step 2: Scale up and move towards the center.
            sequence.Append(objectToAnimate.transform.DOScale(
                new Vector3(scaleToCenterScale, scaleToCenterScale, 1f), durationMove)).
                Join(objectToAnimate.transform.DOMove(Vector3.zero, durationMove));

            // Step 3: Pause briefly at the highlighted position.
            sequence.AppendInterval(durationMove);

            // Step 4: Restore the original scale and move to the final position.
            if(!maintainZoom)
                sequence.Append(objectToAnimate.transform.DOScale(Vector3.one, durationMove));
            if (rotateToFinalLocation)
            {
                sequence.Join(objectToAnimate.transform.DORotate(finalLocation.rotation.eulerAngles, durationMove));
            }
            sequence.Join(objectToAnimate.transform.DOMove(finalLocation.position, durationMove))
                .AppendCallback(() =>
                {
                    HighlightCardAndMoveDone = true;
                    if (drawBoardOnComplete)
                    {
                        GUIEvents.RaiseDrawBoard();
                    }
                    if (destroyOnComplete)
                    {
                        Object.Destroy(objectToAnimate);
                    }

                })
                .OnUpdate(() =>
                {
                    if (Furhat.Instance != null && !HighlightCardAndMoveDone)
                        Furhat.Instance.NotifyObjectOnScreenMoved(objectToAnimate.transform);
                });

            return sequence;
        }

        public static Sequence AnimateCardTransition(GameObject card, Transform target, float durationShake = 0, bool useShake = false, float shakeMagnitude = 0f)
        {
            Sequence sequence = DOTween.Sequence();
            if (useShake)
            {
                sequence.Append(card.transform.DOShakeRotation(durationShake / 2, new Vector3(0f, 0f, shakeMagnitude), 10, 90, false));
            }
            sequence.Append(card.transform.DOMove(target.position, durationMove))
                    .Join(card.transform.DORotate(target.rotation.eulerAngles, durationMove));
            return sequence;
        }

        public static Sequence FadeOutline(Outline outline)
        {
            Sequence sequence = DOTween.Sequence();
            outline.enabled = true;
            sequence.Join(outline.DOFade(0f, durationMove/2).SetLoops(2, LoopType.Yoyo))
                .OnUpdate(() =>
                { if (Furhat.Instance != null) Furhat.Instance.NotifyObjectOnScreenMoved(outline.transform);})
                .AppendCallback(() =>
                {
                    outline.enabled = false;
                });
            return sequence;
        }
    }
}
