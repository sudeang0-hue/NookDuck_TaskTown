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

        private bool canAdvance;

        public event Action AdvanceRequested;

        public bool IsVisible => canvasGroup != null && canvasGroup.alpha > 0f;

        private void Awake()
        {
            if (canvasGroup == null)
                TryGetComponent(out canvasGroup);
        }

        private void OnEnable()
        {
            if (advanceButton != null)
                advanceButton.onClick.AddListener(HandleAdvanceClicked);
        }

        private void OnDisable()
        {
            if (advanceButton != null)
                advanceButton.onClick.RemoveListener(HandleAdvanceClicked);
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
            if (advanceButton != null && advanceButton.targetGraphic != null)
                advanceButton.targetGraphic.raycastTarget = showAdvanceButton;

            if (advanceIndicator != null)
                advanceIndicator.SetActive(showAdvanceButton);
        }

        public void SetVisible(bool isVisible)
        {
            if (canvasGroup == null)
                return;

            canvasGroup.alpha = isVisible ? 1f : 0f;
            canvasGroup.interactable = isVisible;
            canvasGroup.blocksRaycasts = isVisible;
        }

        private void HandleAdvanceClicked()
        {
            if (canAdvance)
                AdvanceRequested?.Invoke();
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
