using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TaskTown.Tutorial
{
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

        public event Action AdvanceRequested;
        public event Action SkipConfirmed;

        public bool IsVisible => canvasGroup != null && canvasGroup.alpha > 0f;
        public bool IsSkipConfirmationOpen => isSkipConfirmationOpen;

        private void Awake()
        {
            if (canvasGroup == null)
                TryGetComponent(out canvasGroup);

            if (hoverTween == null && advanceButton != null)
                advanceButton.TryGetComponent(out hoverTween);
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
            SetText(messageText, message);
            bool hasObjective = SetOptionalText(objectiveText, objective);
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
            if (isSkipRequestPending)
                return;

            isSkipConfirmationOpen = false;
            if (skipConfirmationPanel != null)
                skipConfirmationPanel.SetActive(false);

            RefreshSkipUi();
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

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
                target.text = value ?? string.Empty;
        }

        private static bool SetOptionalText(TMP_Text target, string value)
        {
            if (target == null)
                return false;

            bool hasValue = !string.IsNullOrWhiteSpace(value);
            target.text = hasValue ? value : string.Empty;
            target.gameObject.SetActive(hasValue);
            return hasValue;
        }
    }
}
