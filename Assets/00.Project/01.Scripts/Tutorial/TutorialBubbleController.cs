using System;
using System.Globalization;
using TaskTown.KDH;
using UnityEngine;

namespace TaskTown.Tutorial
{
    /// <summary>
    /// TutorialManager의 진행 상태를 말풍선 표시 데이터로 변환합니다.
    /// 상태 변경 이벤트와 UI 클릭 때만 동작하며 Update를 사용하지 않습니다.
    /// </summary>
    public sealed class TutorialBubbleController : MonoBehaviour
    {
        [SerializeField] private TutorialManager tutorialManager;
        [SerializeField] private TutorialConfigSO config;
        [SerializeField] private TutorialBubbleView view;

        private bool isSubscribed;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
        }

        private void Start()
        {
            if (tutorialManager == null || config == null || view == null)
            {
                Debug.LogWarning(
                    "[TutorialBubbleController] TutorialManager, Config, View 연결을 확인하세요.",
                    this);
            }

            RefreshView();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void ResolveReferences()
        {
            if (tutorialManager == null)
                TryGetComponent(out tutorialManager);

            if (view == null)
                view = GetComponentInChildren<TutorialBubbleView>(true);
        }

        private void Subscribe()
        {
            if (isSubscribed || tutorialManager == null || view == null)
                return;

            tutorialManager.ProgressChanged += HandleProgressChanged;
            tutorialManager.StepChanged += HandleStepChanged;
            tutorialManager.PauseChanged += HandlePauseChanged;
            view.AdvanceRequested += HandleAdvanceRequested;
            isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!isSubscribed)
                return;

            if (tutorialManager != null)
            {
                tutorialManager.ProgressChanged -= HandleProgressChanged;
                tutorialManager.StepChanged -= HandleStepChanged;
                tutorialManager.PauseChanged -= HandlePauseChanged;
            }

            if (view != null)
                view.AdvanceRequested -= HandleAdvanceRequested;

            isSubscribed = false;
        }

        private void HandleProgressChanged(TutorialSaveData progress)
        {
            RefreshView();
        }

        private void HandlePauseChanged(bool isPaused)
        {
            RefreshView();
        }

        private void HandleStepChanged(
            TutorialStep previousStep,
            TutorialStep nextStep)
        {
            // 대화 단계 전환은 버튼 클릭 시 이미 Punch를 실행하므로 중복 재생하지 않습니다.
            if (!IsDialogueStep(previousStep) && nextStep != TutorialStep.Completed)
                view?.PlayPunch();
        }

        private void HandleAdvanceRequested()
        {
            if (!CanDisplayTutorial())
                return;

            TutorialStep step = tutorialManager.CurrentStep;
            if (!config.TryGetStepContent(step, out TutorialStepContent content) ||
                !content.AllowClickAdvance)
            {
                return;
            }

            int messageCount = content.Messages.Count;
            if (messageCount <= 0)
                return;

            int dialogueIndex = config.ClampDialogueIndex(
                step,
                tutorialManager.DialogueIndex);

            if (dialogueIndex < messageCount - 1)
            {
                tutorialManager.TrySetDialogueIndex(dialogueIndex + 1);
                return;
            }

            tutorialManager.ReportSignal(TutorialSignalType.DialogueCompleted);
        }

        private void RefreshView()
        {
            if (view == null)
                return;

            if (!CanDisplayTutorial())
            {
                view.SetVisible(false);
                return;
            }

            TutorialStep step = tutorialManager.CurrentStep;
            if (!config.TryGetStepContent(step, out TutorialStepContent content) ||
                !config.TryGetMessage(step, tutorialManager.DialogueIndex, out string message))
            {
                view.SetVisible(false);
                return;
            }

            int clampedIndex = config.ClampDialogueIndex(
                step,
                tutorialManager.DialogueIndex);

            if (IsDialogueStep(step) &&
                clampedIndex != tutorialManager.DialogueIndex &&
                tutorialManager.TrySetDialogueIndex(clampedIndex))
            {
                return;
            }

            string progress = BuildProgressText(content);
            view.Render(
                config.SpeakerName,
                config.SpeakerPortrait,
                message,
                content.ObjectiveText,
                progress,
                content.AllowClickAdvance);
            view.SetVisible(true);
        }

        private bool CanDisplayTutorial()
        {
            return tutorialManager != null &&
                   tutorialManager.IsInitialized &&
                   !tutorialManager.IsPaused &&
                   !tutorialManager.IsCompleted &&
                   config != null;
        }

        private string BuildProgressText(TutorialStepContent content)
        {
            if (content.ProgressDisplayType != TutorialProgressDisplayType.ManualCoin)
                return string.Empty;

            string format = string.IsNullOrWhiteSpace(content.ProgressFormat)
                ? "{0} / {1}"
                : content.ProgressFormat;

            try
            {
                return string.Format(
                    CultureInfo.CurrentCulture,
                    format,
                    tutorialManager.ManualEarnedCoin,
                    TutorialStateMachine.ManualCoinTarget);
            }
            catch (FormatException)
            {
                Debug.LogWarning(
                    $"[TutorialBubbleController] 잘못된 진행도 형식입니다: {format}",
                    config);
                return $"{tutorialManager.ManualEarnedCoin} / " +
                       TutorialStateMachine.ManualCoinTarget;
            }
        }

        private static bool IsDialogueStep(TutorialStep step)
        {
            return step == TutorialStep.IntroDialogue ||
                   step == TutorialStep.CompletionDialogue;
        }
    }
}
