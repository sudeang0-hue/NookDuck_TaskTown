using System;
using System.Collections;
using System.Collections.Generic;
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
        [SerializeField] private TutorialBubbleSfxPlayer sfxPlayer;

        private bool isSubscribed;
        private Coroutine pendingStepPresentation;
        private TutorialStep presentedStep;
        private bool hasPresentedStep;

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
            StopPendingStepPresentation();
            Unsubscribe();
        }

        private void ResolveReferences()
        {
            if (tutorialManager == null)
                TryGetComponent(out tutorialManager);

            if (view == null)
                view = GetComponentInChildren<TutorialBubbleView>(true);

            if (sfxPlayer == null)
                TryGetComponent(out sfxPlayer);
        }

        private void Subscribe()
        {
            if (isSubscribed || tutorialManager == null || view == null)
                return;

            tutorialManager.ProgressChanged += HandleProgressChanged;
            tutorialManager.StepChanged += HandleStepChanged;
            tutorialManager.PauseChanged += HandlePauseChanged;
            view.AdvanceRequested += HandleAdvanceRequested;
            view.SkipConfirmationOpened += HandleSkipConfirmationOpened;
            view.SkipConfirmed += HandleSkipConfirmed;
            view.SkipCancelled += HandleSkipCancelled;
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
            {
                view.AdvanceRequested -= HandleAdvanceRequested;
                view.SkipConfirmationOpened -= HandleSkipConfirmationOpened;
                view.SkipConfirmed -= HandleSkipConfirmed;
                view.SkipCancelled -= HandleSkipCancelled;
            }

            isSubscribed = false;
        }

        private void HandleProgressChanged(TutorialSaveData progress)
        {
            // 상태 머신은 목표치를 달성한 ProgressChanged를 다음 단계 값으로 전달합니다.
            // 기존 퀘스트를 유지하는 완료 연출 동안에는 최종 목표 수치를 먼저 확정 표시합니다.
            if (hasPresentedStep &&
                progress != null &&
                progress.currentStep != presentedStep)
            {
                TryRenderCompletedProgress(progress);
                return;
            }

            RefreshView();
        }

        private void HandlePauseChanged(bool isPaused)
        {
            if (isPaused)
                StopPendingStepPresentation();

            RefreshView();
        }

        private void HandleStepChanged(
            TutorialStep previousStep,
            TutorialStep nextStep)
        {
            StopPendingStepPresentation();

            if (IsQuestStep(previousStep) && nextStep != TutorialStep.Completed)
            {
                view?.PlayPunch();

                float presentationDelay = 0f;
                if (sfxPlayer != null)
                {
                    sfxPlayer.PlayQuestClear(previousStep);
                    presentationDelay = sfxPlayer.QuestClearPresentationDelay;
                }

                if (presentationDelay > 0f && isActiveAndEnabled)
                {
                    pendingStepPresentation = StartCoroutine(
                        PresentStepAfterDelay(presentationDelay));
                    return;
                }
            }

            RefreshView();
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

            if (step == TutorialStep.CollapseAndExpandTown)
            {
                HandleTownWindowAdvance(content);
                return;
            }

            int messageCount = content.Messages.Count;
            if (messageCount <= 0)
                return;

            int dialogueIndex = config.ClampDialogueIndex(
                step,
                tutorialManager.DialogueIndex);

            sfxPlayer?.PlayDialogueAdvance();

            if (dialogueIndex < messageCount - 1)
            {
                tutorialManager.TrySetDialogueIndex(dialogueIndex + 1);
                return;
            }

            tutorialManager.ReportSignal(TutorialSignalType.DialogueCompleted);
        }

        private void HandleSkipConfirmationOpened()
        {
            sfxPlayer?.PlaySkipOpen();
        }

        private void HandleSkipConfirmed()
        {
            sfxPlayer?.PlaySkipConfirm();
            StopPendingStepPresentation();

            if (tutorialManager == null || !tutorialManager.TrySkipTutorial())
                view?.ResetSkipRequest();
        }

        private void HandleSkipCancelled()
        {
            sfxPlayer?.PlaySkipCancel();
        }

        private void HandleTownWindowAdvance(TutorialStepContent content)
        {
            IReadOnlyList<string> messages;
            bool isGuideDialogue = !tutorialManager.IsTownWindowGuideCompleted;

            if (isGuideDialogue)
            {
                messages = content.Messages;
            }
            else if (tutorialManager.IsTownWindowExpanded)
            {
                messages = content.CompletionMessages;
            }
            else
            {
                return;
            }

            if (messages.Count <= 0)
                return;

            int dialogueIndex = isGuideDialogue
                ? config.ClampDialogueIndex(
                    TutorialStep.CollapseAndExpandTown,
                    tutorialManager.DialogueIndex)
                : config.ClampCompletionDialogueIndex(
                    TutorialStep.CollapseAndExpandTown,
                    tutorialManager.DialogueIndex);

            sfxPlayer?.PlayDialogueAdvance();

            if (dialogueIndex < messages.Count - 1)
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
            if (!config.TryGetStepContent(step, out TutorialStepContent content))
            {
                view.SetVisible(false);
                return;
            }

            if (step == TutorialStep.CollapseAndExpandTown)
            {
                RefreshTownWindowView(content);
                return;
            }

            if (!config.TryGetMessage(
                    step,
                    tutorialManager.DialogueIndex,
                    out string message))
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
            RenderView(
                step,
                message,
                content.ObjectiveText,
                progress,
                content.AllowClickAdvance);
        }

        private void RefreshTownWindowView(TutorialStepContent content)
        {
            TutorialStep step = TutorialStep.CollapseAndExpandTown;

            if (!tutorialManager.IsTownWindowGuideCompleted)
            {
                int clampedIndex = config.ClampDialogueIndex(
                    step,
                    tutorialManager.DialogueIndex);
                if (clampedIndex != tutorialManager.DialogueIndex &&
                    tutorialManager.TrySetDialogueIndex(clampedIndex))
                {
                    return;
                }

                if (!config.TryGetMessage(
                        step,
                        clampedIndex,
                        out string guideMessage))
                {
                    view.SetVisible(false);
                    return;
                }

                RenderView(step, guideMessage, string.Empty, string.Empty, true);
                return;
            }

            if (!tutorialManager.IsTownWindowMinimized)
            {
                int lastGuideIndex = content.Messages.Count - 1;
                if (lastGuideIndex < 0 ||
                    !config.TryGetMessage(step, lastGuideIndex, out string guideMessage))
                {
                    view.SetVisible(false);
                    return;
                }

                RenderView(
                    step,
                    guideMessage,
                    content.ObjectiveText,
                    string.Empty,
                    false);
                return;
            }

            if (!tutorialManager.IsTownWindowExpanded)
            {
                view.SetVisible(false);
                return;
            }

            int completionIndex = config.ClampCompletionDialogueIndex(
                step,
                tutorialManager.DialogueIndex);
            if (completionIndex != tutorialManager.DialogueIndex &&
                tutorialManager.TrySetDialogueIndex(completionIndex))
            {
                return;
            }

            if (!config.TryGetCompletionMessage(
                    step,
                    completionIndex,
                    out string completionMessage))
            {
                view.SetVisible(false);
                return;
            }

            RenderView(step, completionMessage, string.Empty, string.Empty, true);
        }

        private void RenderView(
            TutorialStep step,
            string message,
            string objective,
            string progress,
            bool allowClickAdvance)
        {
            view.Render(
                config.SpeakerName,
                config.SpeakerPortrait,
                message,
                objective,
                progress,
                allowClickAdvance);
            view.SetVisible(true);
            presentedStep = step;
            hasPresentedStep = true;
        }

        private bool TryRenderCompletedProgress(TutorialSaveData progress)
        {
            if (view == null || config == null || progress == null ||
                !config.TryGetStepContent(
                    presentedStep,
                    out TutorialStepContent content) ||
                !TryGetProgressValues(
                    content.ProgressDisplayType,
                    progress.manualEarnedCoin,
                    progress.autoProductionEarnedCoin,
                    out long current,
                    out long target) ||
                current < target ||
                !config.TryGetMessage(presentedStep, 0, out string message))
            {
                return false;
            }

            RenderView(
                presentedStep,
                message,
                content.ObjectiveText,
                FormatProgressText(content, target, target),
                false);
            return true;
        }

        private IEnumerator PresentStepAfterDelay(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            pendingStepPresentation = null;
            RefreshView();
        }

        private void StopPendingStepPresentation()
        {
            if (pendingStepPresentation == null)
                return;

            StopCoroutine(pendingStepPresentation);
            pendingStepPresentation = null;
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
            if (!TryGetProgressValues(
                    content.ProgressDisplayType,
                    tutorialManager.ManualEarnedCoin,
                    tutorialManager.AutoProductionEarnedCoin,
                    out long current,
                    out long target))
            {
                return string.Empty;
            }

            return FormatProgressText(content, current, target);
        }

        private static bool TryGetProgressValues(
            TutorialProgressDisplayType displayType,
            long manualEarnedCoin,
            long autoProductionEarnedCoin,
            out long current,
            out long target)
        {
            switch (displayType)
            {
                case TutorialProgressDisplayType.ManualCoin:
                    current = manualEarnedCoin;
                    target = TutorialStateMachine.ManualCoinTarget;
                    return true;

                case TutorialProgressDisplayType.AutoProductionCoin:
                    current = autoProductionEarnedCoin;
                    target = TutorialStateMachine.AutoProductionCoinTarget;
                    return true;

                default:
                    current = 0L;
                    target = 0L;
                    return false;
            }
        }

        private string FormatProgressText(
            TutorialStepContent content,
            long current,
            long target)
        {

            string format = string.IsNullOrWhiteSpace(content.ProgressFormat)
                ? "{0} / {1}"
                : content.ProgressFormat;

            try
            {
                return string.Format(
                    CultureInfo.CurrentCulture,
                    format,
                    current,
                    target);
            }
            catch (FormatException)
            {
                Debug.LogWarning(
                    $"[TutorialBubbleController] 잘못된 진행도 형식입니다: {format}",
                    config);
                return $"{current} / {target}";
            }
        }

        private static bool IsDialogueStep(TutorialStep step)
        {
            return step == TutorialStep.IntroDialogue ||
                   step == TutorialStep.CompletionDialogue;
        }

        private static bool IsQuestStep(TutorialStep step)
        {
            return step == TutorialStep.EarnManualCoin ||
                   step == TutorialStep.CollapseAndExpandTown ||
                   step == TutorialStep.DrawAnimal ||
                   step == TutorialStep.DrawTool ||
                   step == TutorialStep.AssignAnimal ||
                   step == TutorialStep.ConfirmAutoProduction ||
                   step == TutorialStep.OpenVillageInfo ||
                   step == TutorialStep.UpgradeVillage;
        }
    }
}
