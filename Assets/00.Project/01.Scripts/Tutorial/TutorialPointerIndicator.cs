using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace TaskTown.Tutorial
{
    /// <summary>
    /// 버튼 Scale과 독립적으로 도넛 및 손가락 클릭 Flow를 반복 재생합니다.
    /// 표시 중인 후보 버튼이 바뀌면 Overlay Canvas 좌표로 다시 맞춥니다.
    /// </summary>
    public sealed class TutorialPointerIndicator : MonoBehaviour
    {
        [Header("Overlay UI")]
        [SerializeField] private Canvas overlayCanvas;
        [SerializeField] private RectTransform indicatorRoot;
        [SerializeField] private CanvasGroup indicatorCanvasGroup;
        [SerializeField] private RectTransform ringTransform;
        [SerializeField] private RectTransform handTransform;

        [Header("손가락 이동")]
        [SerializeField] private Vector2 approachOffset = new(80f, -80f);
        [SerializeField, Min(0.01f)] private float fadeInDuration = 0.12f;
        [SerializeField, Min(0.01f)] private float approachDuration = 0.45f;
        [SerializeField, Min(0.01f)] private float pressDuration = 0.12f;
        [SerializeField, Min(0.01f)] private float fadeOutDuration = 0.15f;
        [SerializeField, Min(0f)] private float loopInterval = 0.45f;
        [SerializeField] private Ease approachEase = Ease.OutCubic;

        [Header("클릭 표현")]
        [SerializeField, Range(0.1f, 1f)] private float handPressScale = 0.82f;
        [SerializeField, Range(0.1f, 1f)] private float ringStartScale = 0.78f;
        [SerializeField, Min(1f)] private float ringClickScale = 1.16f;

        private readonly List<Button> targetCandidates = new();
        private readonly Vector3[] targetWorldCorners = new Vector3[4];

        private TutorialPointerPositionMode positionMode;
        private Vector2 pointerOffset;
        private Vector2 canvasPosition;
        private Vector2 targetAnchor = new(0.5f, 0.5f);
        private float fixedRingSize;
        private float ringPadding;
        private Button activeTarget;
        private RectTransform directTarget;
        private Sequence flowSequence;
        private bool isRequested;
        private bool isVisualActive;
        private bool showHand;
        private bool showFocusRing;
        private Collider worldTargetCollider;
        private Camera worldTargetCamera;

        private void LateUpdate()
        {
            if (!isRequested)
                return;

            if (worldTargetCollider != null)
            {
                if (!IsWorldTargetAvailable())
                {
                    StopVisual();
                    return;
                }

                ApplyWorldTargetLayout();
                EnsureVisualActive();
                return;
            }

            if (positionMode == TutorialPointerPositionMode.CanvasPosition)
            {
                ApplyFixedPosition();
                EnsureVisualActive();
                return;
            }

            if (directTarget != null)
            {
                if (!directTarget.gameObject.activeInHierarchy)
                {
                    StopVisual();
                    return;
                }

                ApplyTargetLayout(directTarget, !isVisualActive);
                EnsureVisualActive();
                return;
            }

            Button nextTarget = FindFirstActiveTarget();
            if (nextTarget != activeTarget)
            {
                activeTarget = nextTarget;
                if (activeTarget == null)
                {
                    StopVisual();
                    return;
                }

                ApplyTargetLayout(activeTarget.transform as RectTransform, true);
                EnsureVisualActive();
                return;
            }

            if (activeTarget != null)
                ApplyTargetLayout(activeTarget.transform as RectTransform, false);
        }

        private void OnDisable()
        {
            Hide();
        }

        private void OnDestroy()
        {
            KillFlowSequence();
        }

        public void Show(TutorialStepContent content, params Button[] candidates)
        {
            Show(
                content,
                content != null
                    ? content.HighlightEffects
                    : TutorialHighlightEffect.None,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                candidates);
        }

        public void Show(
            TutorialStepContent content,
            TutorialHighlightEffect effects,
            params Button[] candidates)
        {
            Show(
                content,
                effects,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                candidates);
        }

        /// <summary>
        /// 버튼 내부의 정규화 기준점과 추가 픽셀 오프셋을 사용하여 손가락 위치를
        /// 단계 내부 구간별로 조정합니다.
        /// </summary>
        public void Show(
            TutorialStepContent content,
            Vector2 targetAnchorNormalized,
            Vector2 additionalPointerOffset,
            params Button[] candidates)
        {
            Show(
                content,
                content != null
                    ? content.HighlightEffects
                    : TutorialHighlightEffect.None,
                targetAnchorNormalized,
                additionalPointerOffset,
                candidates);
        }

        public void Show(
            TutorialStepContent content,
            TutorialHighlightEffect effects,
            Vector2 targetAnchorNormalized,
            Vector2 additionalPointerOffset,
            params Button[] candidates)
        {
            if (!PrepareVisual(
                    content,
                    effects,
                    targetAnchorNormalized,
                    additionalPointerOffset))
                return;

            if (candidates != null)
            {
                HashSet<Button> uniqueTargets = new();
                foreach (Button candidate in candidates)
                {
                    if (candidate != null && uniqueTargets.Add(candidate))
                        targetCandidates.Add(candidate);
                }
            }

            isRequested = true;

            if (positionMode == TutorialPointerPositionMode.CanvasPosition)
            {
                ApplyFixedPosition();
                EnsureVisualActive();
                return;
            }

            activeTarget = FindFirstActiveTarget();
            if (activeTarget == null)
                return;

            ApplyTargetLayout(activeTarget.transform as RectTransform, true);
            EnsureVisualActive();
        }

        /// <summary>
        /// Button 컴포넌트가 없는 일반 UI도 동일한 손가락/도넛 강조 대상으로 사용합니다.
        /// </summary>
        public void Show(
            TutorialStepContent content,
            RectTransform target,
            Vector2 targetAnchorNormalized,
            Vector2 additionalPointerOffset)
        {
            if (target == null ||
                !PrepareVisual(
                    content,
                    content != null
                        ? content.HighlightEffects
                        : TutorialHighlightEffect.None,
                    targetAnchorNormalized,
                    additionalPointerOffset))
            {
                return;
            }

            directTarget = target;
            isRequested = true;

            if (!directTarget.gameObject.activeInHierarchy)
                return;

            ApplyTargetLayout(directTarget, true);
            EnsureVisualActive();
        }

        /// <summary>
        /// UI Button이 아닌 월드 오브젝트의 Collider 중심을 Overlay Canvas 좌표로
        /// 변환하여 손가락 강조를 표시합니다.
        /// </summary>
        public void ShowWorld(
            TutorialStepContent content,
            Collider targetCollider,
            Camera targetCamera = null)
        {
            if (targetCollider == null ||
                !PrepareVisual(
                    content,
                    content != null
                        ? content.HighlightEffects
                        : TutorialHighlightEffect.None,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero))
            {
                return;
            }

            worldTargetCollider = targetCollider;
            worldTargetCamera = targetCamera != null ? targetCamera : Camera.main;
            isRequested = true;

            if (!IsWorldTargetAvailable())
                return;

            ApplyWorldTargetLayout();
            EnsureVisualActive();
        }

        public void Hide()
        {
            isRequested = false;
            activeTarget = null;
            directTarget = null;
            targetCandidates.Clear();
            worldTargetCollider = null;
            worldTargetCamera = null;
            StopVisual();
            showHand = false;
            showFocusRing = false;
        }

        private bool PrepareVisual(
            TutorialStepContent content,
            TutorialHighlightEffect effects,
            Vector2 targetAnchorNormalized,
            Vector2 additionalPointerOffset)
        {
            Hide();

            if (content == null || overlayCanvas == null || indicatorRoot == null ||
                indicatorCanvasGroup == null || ringTransform == null ||
                handTransform == null)
            {
                return false;
            }

            showHand = (effects & TutorialHighlightEffect.Pointer) != 0;
            showFocusRing = (effects & TutorialHighlightEffect.FocusRing) != 0;
            if (!showHand && !showFocusRing)
                return false;

            positionMode = content.PointerPositionMode;
            pointerOffset = content.PointerOffset + additionalPointerOffset;
            canvasPosition = content.PointerCanvasPosition;
            targetAnchor = new Vector2(
                Mathf.Clamp01(targetAnchorNormalized.x),
                Mathf.Clamp01(targetAnchorNormalized.y));
            fixedRingSize = Mathf.Max(16f, content.PointerRingSize);
            ringPadding = Mathf.Max(0f, content.PointerRingPadding);
            handTransform.gameObject.SetActive(showHand);
            ringTransform.gameObject.SetActive(showFocusRing);
            return true;
        }

        private Button FindFirstActiveTarget()
        {
            foreach (Button candidate in targetCandidates)
            {
                if (candidate != null && candidate.isActiveAndEnabled &&
                    candidate.gameObject.activeInHierarchy)
                {
                    return candidate;
                }
            }

            return null;
        }

        private void ApplyFixedPosition()
        {
            indicatorRoot.anchoredPosition = canvasPosition + pointerOffset;
            if (showFocusRing)
                ringTransform.sizeDelta = Vector2.one * fixedRingSize;
        }

        private void ApplyTargetLayout(RectTransform targetRect, bool updateRingSize)
        {
            if (targetRect == null || indicatorRoot == null || overlayCanvas == null ||
                indicatorRoot.parent is not RectTransform overlayParent)
            {
                return;
            }

            Canvas targetCanvas = targetRect.GetComponentInParent<Canvas>();
            Camera targetCamera = targetCanvas != null &&
                                  targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? targetCanvas.worldCamera
                : null;
            Camera overlayCamera = overlayCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? overlayCanvas.worldCamera
                : null;

            Rect targetLocalRect = targetRect.rect;
            Vector2 targetLocalPoint = new(
                Mathf.Lerp(targetLocalRect.xMin, targetLocalRect.xMax, targetAnchor.x),
                Mathf.Lerp(targetLocalRect.yMin, targetLocalRect.yMax, targetAnchor.y));
            Vector3 targetWorldCenter = targetRect.TransformPoint(targetLocalPoint);
            Vector2 targetScreenCenter = RectTransformUtility.WorldToScreenPoint(
                targetCamera,
                targetWorldCenter);

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    overlayParent,
                    targetScreenCenter,
                    overlayCamera,
                    out Vector2 localCenter))
            {
                indicatorRoot.anchoredPosition = localCenter + pointerOffset;
            }

            if (!updateRingSize || !showFocusRing)
                return;

            targetRect.GetWorldCorners(targetWorldCorners);
            Vector2 min = new(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new(float.NegativeInfinity, float.NegativeInfinity);

            foreach (Vector3 worldCorner in targetWorldCorners)
            {
                Vector2 screenCorner = RectTransformUtility.WorldToScreenPoint(
                    targetCamera,
                    worldCorner);
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        overlayParent,
                        screenCorner,
                        overlayCamera,
                        out Vector2 localCorner))
                {
                    continue;
                }

                min = Vector2.Min(min, localCorner);
                max = Vector2.Max(max, localCorner);
            }

            if (float.IsInfinity(min.x) || float.IsInfinity(max.x))
                return;

            float diameter = Mathf.Max(max.x - min.x, max.y - min.y) +
                             ringPadding * 2f;
            ringTransform.sizeDelta = Vector2.one * Mathf.Max(16f, diameter);
        }

        private bool IsWorldTargetAvailable()
        {
            return worldTargetCollider != null &&
                   worldTargetCollider.enabled &&
                   worldTargetCollider.gameObject.activeInHierarchy &&
                   worldTargetCamera != null;
        }

        private void ApplyWorldTargetLayout()
        {
            if (!IsWorldTargetAvailable() || indicatorRoot == null ||
                overlayCanvas == null ||
                indicatorRoot.parent is not RectTransform overlayParent)
            {
                return;
            }

            Vector3 screenPoint = worldTargetCamera.WorldToScreenPoint(
                worldTargetCollider.bounds.center);
            if (screenPoint.z <= 0f)
            {
                StopVisual();
                return;
            }

            Camera overlayCamera = overlayCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? overlayCanvas.worldCamera
                : null;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    overlayParent,
                    screenPoint,
                    overlayCamera,
                    out Vector2 localCenter))
            {
                indicatorRoot.anchoredPosition = localCenter + pointerOffset;
            }

            if (showFocusRing)
                ringTransform.sizeDelta = Vector2.one * fixedRingSize;
        }

        private void EnsureVisualActive()
        {
            if (isVisualActive)
                return;

            indicatorRoot.gameObject.SetActive(true);
            isVisualActive = true;
            PlayFlowSequence();
        }

        private void StopVisual()
        {
            KillFlowSequence();
            isVisualActive = false;

            if (indicatorRoot != null)
                indicatorRoot.gameObject.SetActive(false);
        }

        private void PlayFlowSequence()
        {
            KillFlowSequence();

            indicatorCanvasGroup.alpha = 0f;
            if (showHand)
            {
                handTransform.anchoredPosition = approachOffset;
                handTransform.localScale = Vector3.one;
            }
            if (showFocusRing)
                ringTransform.localScale = Vector3.one * ringStartScale;

            flowSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(indicatorRoot.gameObject, LinkBehaviour.KillOnDestroy)
                .Append(indicatorCanvasGroup.DOFade(1f, fadeInDuration));

            if (showFocusRing)
                flowSequence.Join(ringTransform.DOScale(1f, fadeInDuration));

            if (showHand)
            {
                flowSequence
                    .Append(handTransform
                        .DOAnchorPos(Vector2.zero, approachDuration)
                        .SetEase(approachEase))
                    .Append(handTransform
                        .DOScale(handPressScale, pressDuration)
                        .SetEase(Ease.InQuad));
            }
            else if (showFocusRing)
            {
                flowSequence.Append(ringTransform
                    .DOScale(ringClickScale, pressDuration)
                    .SetEase(Ease.OutQuad));
            }

            if (showHand && showFocusRing)
            {
                flowSequence.Join(ringTransform
                    .DOScale(ringClickScale, pressDuration)
                    .SetEase(Ease.OutQuad));
            }

            flowSequence
                .Append(indicatorCanvasGroup.DOFade(0f, fadeOutDuration))
                .AppendInterval(loopInterval)
                .SetLoops(-1, LoopType.Restart);
        }

        private void KillFlowSequence()
        {
            if (flowSequence != null && flowSequence.IsActive())
                flowSequence.Kill();

            flowSequence = null;
        }
    }
}
