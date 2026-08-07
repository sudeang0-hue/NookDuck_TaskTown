using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TaskTown.Tutorial
{
    [Serializable]
    public struct TutorialRectTransformLayout
    {
        [SerializeField] private Vector2 anchorMin;
        [SerializeField] private Vector2 anchorMax;
        [SerializeField] private Vector2 anchoredPosition;
        [SerializeField] private Vector2 pivot;

        public static TutorialRectTransformLayout Capture(RectTransform target)
        {
            if (target == null)
                return default;

            return new TutorialRectTransformLayout
            {
                anchorMin = target.anchorMin,
                anchorMax = target.anchorMax,
                anchoredPosition = target.anchoredPosition,
                pivot = target.pivot
            };
        }

        public void Apply(RectTransform target, Vector2 positionOffset = default)
        {
            if (target == null)
                return;

            target.anchorMin = anchorMin;
            target.anchorMax = anchorMax;
            target.pivot = pivot;
            target.anchoredPosition = anchoredPosition + positionOffset;
        }

        public TutorialRectTransformLayout CreateHorizontalMirror()
        {
            return new TutorialRectTransformLayout
            {
                anchorMin = new Vector2(1f - anchorMax.x, anchorMin.y),
                anchorMax = new Vector2(1f - anchorMin.x, anchorMax.y),
                anchoredPosition = new Vector2(
                    -anchoredPosition.x,
                    anchoredPosition.y),
                pivot = new Vector2(1f - pivot.x, pivot.y)
            };
        }
    }

    [Serializable]
    public struct TutorialBubbleLayoutPreset
    {
        [SerializeField] private bool configured;
        [SerializeField] private TutorialRectTransformLayout layoutRoot;
        [SerializeField] private TutorialRectTransformLayout portraitRoot;
        [SerializeField] private TutorialRectTransformLayout bubbleRoot;
        [SerializeField] private TutorialRectTransformLayout bubbleBackground;
        [SerializeField] private Vector3 bubbleBackgroundScale;
        [SerializeField] private TutorialRectTransformLayout bubbleBorder;
        [SerializeField] private Vector3 bubbleBorderScale;

        public bool IsConfigured => configured;

        public static TutorialBubbleLayoutPreset Capture(
            RectTransform layoutRootTarget,
            RectTransform portraitRootTarget,
            RectTransform bubbleRootTarget,
            RectTransform bubbleBackgroundTarget,
            RectTransform bubbleBorderTarget)
        {
            return new TutorialBubbleLayoutPreset
            {
                configured = layoutRootTarget != null &&
                             portraitRootTarget != null &&
                             bubbleRootTarget != null,
                layoutRoot = TutorialRectTransformLayout.Capture(
                    layoutRootTarget),
                portraitRoot = TutorialRectTransformLayout.Capture(
                    portraitRootTarget),
                bubbleRoot = TutorialRectTransformLayout.Capture(
                    bubbleRootTarget),
                bubbleBackground = TutorialRectTransformLayout.Capture(
                    bubbleBackgroundTarget),
                bubbleBackgroundScale = bubbleBackgroundTarget != null
                    ? bubbleBackgroundTarget.localScale
                    : Vector3.one,
                bubbleBorder = TutorialRectTransformLayout.Capture(
                    bubbleBorderTarget),
                bubbleBorderScale = bubbleBorderTarget != null
                    ? bubbleBorderTarget.localScale
                    : Vector3.one
            };
        }

        public void Apply(
            RectTransform layoutRootTarget,
            RectTransform portraitRootTarget,
            RectTransform bubbleRootTarget,
            RectTransform bubbleBackgroundTarget,
            RectTransform bubbleBorderTarget,
            Vector2 layoutOffset)
        {
            if (!configured)
                return;

            layoutRoot.Apply(layoutRootTarget, layoutOffset);
            portraitRoot.Apply(portraitRootTarget);
            bubbleRoot.Apply(bubbleRootTarget);
            bubbleBackground.Apply(bubbleBackgroundTarget);
            bubbleBorder.Apply(bubbleBorderTarget);

            if (bubbleBackgroundTarget != null)
                bubbleBackgroundTarget.localScale = bubbleBackgroundScale;

            if (bubbleBorderTarget != null)
                bubbleBorderTarget.localScale = bubbleBorderScale;
        }

        public TutorialBubbleLayoutPreset CreateHorizontalMirror()
        {
            Vector3 mirroredBackgroundScale = bubbleBackgroundScale;
            mirroredBackgroundScale.x = -mirroredBackgroundScale.x;

            Vector3 mirroredBorderScale = bubbleBorderScale;
            mirroredBorderScale.x = -mirroredBorderScale.x;

            return new TutorialBubbleLayoutPreset
            {
                configured = configured,
                layoutRoot = layoutRoot.CreateHorizontalMirror(),
                portraitRoot = portraitRoot.CreateHorizontalMirror(),
                bubbleRoot = bubbleRoot.CreateHorizontalMirror(),
                bubbleBackground = bubbleBackground.CreateHorizontalMirror(),
                bubbleBackgroundScale = mirroredBackgroundScale,
                bubbleBorder = bubbleBorder.CreateHorizontalMirror(),
                bubbleBorderScale = mirroredBorderScale
            };
        }
    }

    /// <summary>
    /// 말풍선 UI 요소를 표시하는 역할만 담당합니다.
    /// 튜토리얼 진행 상태나 완료 조건은 알지 못합니다.
    /// </summary>
    public sealed class TutorialBubbleView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image speakerPortraitImage;
        [SerializeField] private GameObject portraitPlaceholder;
        [SerializeField] private TMP_Text speakerNameText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private GameObject objectiveContainer;
        [SerializeField] private TMP_Text objectiveText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private Button advanceButton;
        [SerializeField] private GameObject advanceIndicator;
        [SerializeField] private TutorialBubbleHoverTween hoverTween;

        [Header("Speaker Layout")]
        [SerializeField] private RectTransform layoutRoot;
        [SerializeField] private RectTransform portraitRoot;
        [SerializeField] private RectTransform bubbleRoot;
        [SerializeField] private RectTransform bubbleBackground;
        [SerializeField] private RectTransform bubbleBorder;
        [SerializeField] private TutorialBubbleLayoutPreset leftLayout;
        [SerializeField] private TutorialBubbleLayoutPreset rightLayout;

        [Header("Tutorial Skip")]
        [SerializeField] private GameObject skipRoot;
        [SerializeField] private Button skipButton;
        [SerializeField] private GameObject skipConfirmationPanel;
        [SerializeField] private Button confirmSkipButton;
        [SerializeField] private Button cancelSkipButton;

        private bool canAdvance;
        private bool isTutorialVisible;
        private bool isSkipConfirmationOpen;
        private bool isSkipRequestPending;
        private bool hasAppliedLayout;
        private TutorialSpeakerSide appliedLayoutSide;
        private Vector2 appliedLayoutOffset;

        public event Action AdvanceRequested;
        public event Action SkipConfirmationOpened;
        public event Action SkipConfirmed;
        public event Action SkipCancelled;

        public bool IsVisible => canvasGroup != null && canvasGroup.alpha > 0f;
        public bool IsSkipConfirmationOpen => isSkipConfirmationOpen;

        private void Awake()
        {
            if (canvasGroup == null)
                TryGetComponent(out canvasGroup);

            if (hoverTween == null && advanceButton != null)
                advanceButton.TryGetComponent(out hoverTween);

            ResolveLayoutReferences();
        }

        private void OnEnable()
        {
            if (advanceButton != null)
                advanceButton.onClick.AddListener(HandleAdvanceClicked);

            if (skipButton != null)
                skipButton.onClick.AddListener(HandleSkipClicked);
            if (confirmSkipButton != null)
                confirmSkipButton.onClick.AddListener(HandleConfirmSkipClicked);
            if (cancelSkipButton != null)
                cancelSkipButton.onClick.AddListener(HandleCancelSkipClicked);

            isTutorialVisible = IsVisible;
            ResetSkipRequest();
        }

        private void OnDisable()
        {
            if (advanceButton != null)
                advanceButton.onClick.RemoveListener(HandleAdvanceClicked);

            if (skipButton != null)
                skipButton.onClick.RemoveListener(HandleSkipClicked);
            if (confirmSkipButton != null)
                confirmSkipButton.onClick.RemoveListener(HandleConfirmSkipClicked);
            if (cancelSkipButton != null)
                cancelSkipButton.onClick.RemoveListener(HandleCancelSkipClicked);

            isTutorialVisible = false;
            ResetSkipRequest();
        }

        public void Render(
            string speakerName,
            Sprite speakerPortrait,
            string message,
            string objective,
            string progress,
            bool showAdvanceButton)
        {
            SetText(speakerNameText, speakerName);
            SetText(messageText, message, true);
            bool hasObjective = SetOptionalText(objectiveText, objective, true);
            SetOptionalText(progressText, progress);

            if (objectiveContainer != null)
                objectiveContainer.SetActive(hasObjective);

            if (speakerPortraitImage != null)
            {
                speakerPortraitImage.sprite = speakerPortrait;
                speakerPortraitImage.enabled = speakerPortrait != null;
            }

            if (portraitPlaceholder != null)
                portraitPlaceholder.SetActive(speakerPortrait == null);

            canAdvance = showAdvanceButton;
            if (advanceButton != null)
            {
                // 퀘스트 단계에서도 Hover 판정은 유지하고 클릭 진행만 차단합니다.
                advanceButton.interactable = showAdvanceButton;
                if (advanceButton.targetGraphic != null)
                    advanceButton.targetGraphic.raycastTarget = true;
            }

            if (advanceIndicator != null)
                advanceIndicator.SetActive(showAdvanceButton);
        }

        public void SetVisible(bool isVisible)
        {
            if (!isVisible)
                hoverTween?.CollapseImmediate();

            isTutorialVisible = isVisible;
            if (!isVisible)
                ResetSkipRequest();
            else
                RefreshSkipUi();

            if (canvasGroup == null)
                return;

            canvasGroup.alpha = isVisible ? 1f : 0f;
            canvasGroup.interactable = isVisible;
            canvasGroup.blocksRaycasts = isVisible;
        }

        public void PlayPunch()
        {
            hoverTween?.PlayPunch();
        }

        public void ApplySpeakerLayout(
            TutorialSpeakerSide side,
            Vector2 layoutOffset,
            bool force = false)
        {
            ResolveLayoutReferences();

            if (!force && hasAppliedLayout &&
                appliedLayoutSide == side &&
                appliedLayoutOffset == layoutOffset)
            {
                return;
            }

            TutorialBubbleLayoutPreset preset =
                side == TutorialSpeakerSide.Right
                    ? rightLayout
                    : leftLayout;
            if (!preset.IsConfigured)
                return;

            if (Application.isPlaying)
                hoverTween?.RestoreStableScaleImmediate();

            preset.Apply(
                layoutRoot,
                portraitRoot,
                bubbleRoot,
                bubbleBackground,
                bubbleBorder,
                layoutOffset);
            hasAppliedLayout = true;
            appliedLayoutSide = side;
            appliedLayoutOffset = layoutOffset;
        }

#if UNITY_EDITOR
        public TutorialBubbleLayoutPreset CaptureCurrentLayoutPreset()
        {
            ResolveLayoutReferences();
            return TutorialBubbleLayoutPreset.Capture(
                layoutRoot,
                portraitRoot,
                bubbleRoot,
                bubbleBackground,
                bubbleBorder);
        }

        public void CaptureCurrentLayout(TutorialSpeakerSide side)
        {
            TutorialBubbleLayoutPreset preset = CaptureCurrentLayoutPreset();
            if (side == TutorialSpeakerSide.Right)
                rightLayout = preset;
            else
                leftLayout = preset;
        }

        public void ApplyLayoutPreset(TutorialBubbleLayoutPreset preset)
        {
            ResolveLayoutReferences();
            preset.Apply(
                layoutRoot,
                portraitRoot,
                bubbleRoot,
                bubbleBackground,
                bubbleBorder,
                Vector2.zero);
            hasAppliedLayout = false;
        }

        public void CreateMirroredRightLayout()
        {
            rightLayout = leftLayout.CreateHorizontalMirror();
            hasAppliedLayout = false;
        }
#endif

        public void ResetSkipRequest()
        {
            isSkipConfirmationOpen = false;
            isSkipRequestPending = false;

            if (skipConfirmationPanel != null)
                skipConfirmationPanel.SetActive(false);

            RefreshSkipUi();
        }

        private void HandleAdvanceClicked()
        {
            if (canAdvance)
            {
                PlayPunch();
                AdvanceRequested?.Invoke();
            }
        }

        private void HandleSkipClicked()
        {
            if (!isTutorialVisible || isSkipRequestPending ||
                skipConfirmationPanel == null)
            {
                return;
            }

            isSkipConfirmationOpen = true;
            skipConfirmationPanel.SetActive(true);
            RefreshSkipUi();
            SkipConfirmationOpened?.Invoke();
        }

        private void HandleConfirmSkipClicked()
        {
            if (!isTutorialVisible || !isSkipConfirmationOpen ||
                isSkipRequestPending)
            {
                return;
            }

            isSkipRequestPending = true;
            RefreshSkipUi();
            SkipConfirmed?.Invoke();
        }

        private void HandleCancelSkipClicked()
        {
            if (!isTutorialVisible || !isSkipConfirmationOpen ||
                isSkipRequestPending)
            {
                return;
            }

            isSkipConfirmationOpen = false;
            if (skipConfirmationPanel != null)
                skipConfirmationPanel.SetActive(false);

            RefreshSkipUi();
            SkipCancelled?.Invoke();
        }

        private void RefreshSkipUi()
        {
            if (skipRoot != null)
                skipRoot.SetActive(isTutorialVisible);

            if (skipButton != null)
            {
                skipButton.interactable = isTutorialVisible &&
                                          !isSkipConfirmationOpen &&
                                          !isSkipRequestPending;
            }

            bool canChoose = isTutorialVisible &&
                             isSkipConfirmationOpen &&
                             !isSkipRequestPending;
            if (confirmSkipButton != null)
                confirmSkipButton.interactable = canChoose;
            if (cancelSkipButton != null)
                cancelSkipButton.interactable = canChoose;
        }

        private void ResolveLayoutReferences()
        {
            if (layoutRoot == null)
                TryGetComponent(out layoutRoot);

            if (bubbleRoot == null && advanceButton != null)
                bubbleRoot = advanceButton.GetComponent<RectTransform>();

            if (bubbleBackground == null && advanceButton != null &&
                advanceButton.targetGraphic != null)
            {
                bubbleBackground =
                    advanceButton.targetGraphic.rectTransform;
            }

            if (portraitRoot == null && speakerPortraitImage != null)
                portraitRoot = speakerPortraitImage.transform.parent as RectTransform;
        }

        private static void SetText(
            TMP_Text target,
            string value,
            bool applyWordLineBreaks = false)
        {
            if (target != null)
            {
                target.text = applyWordLineBreaks
                    ? TutorialTextLineBreakUtility.ApplyWordLineBreaks(
                        target,
                        value)
                    : value ?? string.Empty;
            }
        }

        private static bool SetOptionalText(
            TMP_Text target,
            string value,
            bool applyWordLineBreaks = false)
        {
            if (target == null)
                return false;

            bool hasValue = !string.IsNullOrWhiteSpace(value);
            target.text = hasValue
                ? applyWordLineBreaks
                    ? TutorialTextLineBreakUtility.ApplyWordLineBreaks(
                        target,
                        value)
                    : value
                : string.Empty;
            target.gameObject.SetActive(hasValue);
            return hasValue;
        }
    }
}
