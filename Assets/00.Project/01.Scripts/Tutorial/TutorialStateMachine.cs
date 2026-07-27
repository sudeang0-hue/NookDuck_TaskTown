using System;
using TaskTown.KDH;

namespace TaskTown.Tutorial
{
    /// <summary>
    /// Unity Scene이나 게임 매니저에 의존하지 않는 튜토리얼 진행 규칙입니다.
    /// 현재 단계에 필요한 신호만 처리하므로 중복 또는 이전 단계 신호는 무시합니다.
    /// </summary>
    public sealed class TutorialStateMachine
    {
        public const long ManualCoinTarget = 100L;

        private readonly TutorialSaveData progress;

        public event Action<TutorialSaveData> ProgressChanged;
        public event Action<TutorialStep, TutorialStep> StepChanged;
        public event Action Completed;

        public TutorialStep CurrentStep => progress.currentStep;
        public long ManualEarnedCoin => progress.manualEarnedCoin;
        public int DialogueIndex => progress.dialogueIndex;
        public bool IsCompleted => progress.IsCompleted;
        public bool IsPaused { get; private set; }
        public TutorialSaveData Progress => progress.Copy();

        public TutorialStateMachine(TutorialSaveData initialProgress)
        {
            progress = initialProgress?.Copy() ?? TutorialSaveData.CreateDefault();
        }

        public void SetPaused(bool isPaused)
        {
            IsPaused = isPaused;
        }

        /// <summary>
        /// 대화 UI가 재개 지점을 저장할 때 사용합니다.
        /// 대화 단계가 아니거나 진행이 일시정지된 경우에는 변경하지 않습니다.
        /// </summary>
        public bool TrySetDialogueIndex(int dialogueIndex)
        {
            if (IsPaused || IsCompleted || dialogueIndex < 0 || !IsDialogueStep(CurrentStep))
                return false;

            if (progress.dialogueIndex == dialogueIndex)
                return false;

            progress.dialogueIndex = dialogueIndex;
            NotifyProgressChanged();
            return true;
        }

        /// <summary>
        /// 현재 단계가 요구하는 신호만 처리합니다.
        /// ManualCoinEarned의 amount에는 보유량이 아니라 실제 지급된 코인량을 전달해야 합니다.
        /// </summary>
        public bool TryHandleSignal(TutorialSignalType signalType, long amount = 0L)
        {
            if (IsPaused || IsCompleted)
                return false;

            switch (CurrentStep)
            {
                case TutorialStep.IntroDialogue:
                case TutorialStep.CompletionDialogue:
                    return TryAdvance(signalType, TutorialSignalType.DialogueCompleted);

                case TutorialStep.EarnManualCoin:
                    return TryAddManualCoin(signalType, amount);

                case TutorialStep.DrawAnimal:
                    return TryAdvance(signalType, TutorialSignalType.AnimalDrawn);

                case TutorialStep.DrawTool:
                    return TryAdvance(signalType, TutorialSignalType.ToolDrawn);

                case TutorialStep.AssignAnimal:
                    return TryAdvance(signalType, TutorialSignalType.AnimalAssigned);

                case TutorialStep.ConfirmAutoProduction:
                    return TryAdvance(signalType, TutorialSignalType.AutoProductionConfirmed);

                case TutorialStep.OpenVillageInfo:
                    return TryAdvance(signalType, TutorialSignalType.VillageInfoOpened);

                case TutorialStep.UpgradeVillage:
                    return TryAdvance(signalType, TutorialSignalType.AnyUpgradePurchased);

                default:
                    return false;
            }
        }

        private bool TryAddManualCoin(TutorialSignalType signalType, long amount)
        {
            if (signalType != TutorialSignalType.ManualCoinEarned || amount <= 0L)
                return false;

            progress.manualEarnedCoin = AddWithoutOverflow(progress.manualEarnedCoin, amount);

            if (progress.manualEarnedCoin >= ManualCoinTarget)
                AdvanceTo(TutorialStep.DrawAnimal);
            else
                NotifyProgressChanged();

            return true;
        }

        private bool TryAdvance(
            TutorialSignalType receivedSignal,
            TutorialSignalType expectedSignal)
        {
            if (receivedSignal != expectedSignal)
                return false;

            AdvanceTo(GetNextStep(CurrentStep));
            return true;
        }

        private void AdvanceTo(TutorialStep nextStep)
        {
            TutorialStep previousStep = progress.currentStep;
            progress.currentStep = nextStep;
            progress.dialogueIndex = 0;

            NotifyProgressChanged();
            StepChanged?.Invoke(previousStep, nextStep);

            if (nextStep == TutorialStep.Completed)
                Completed?.Invoke();
        }

        private void NotifyProgressChanged()
        {
            ProgressChanged?.Invoke(progress.Copy());
        }

        private static TutorialStep GetNextStep(TutorialStep currentStep)
        {
            return currentStep switch
            {
                TutorialStep.IntroDialogue => TutorialStep.EarnManualCoin,
                TutorialStep.DrawAnimal => TutorialStep.DrawTool,
                TutorialStep.DrawTool => TutorialStep.AssignAnimal,
                TutorialStep.AssignAnimal => TutorialStep.ConfirmAutoProduction,
                TutorialStep.ConfirmAutoProduction => TutorialStep.OpenVillageInfo,
                TutorialStep.OpenVillageInfo => TutorialStep.UpgradeVillage,
                TutorialStep.UpgradeVillage => TutorialStep.CompletionDialogue,
                TutorialStep.CompletionDialogue => TutorialStep.Completed,
                _ => currentStep
            };
        }

        private static bool IsDialogueStep(TutorialStep step)
        {
            return step == TutorialStep.IntroDialogue ||
                   step == TutorialStep.CompletionDialogue;
        }

        private static long AddWithoutOverflow(long current, long amount)
        {
            return current > long.MaxValue - amount
                ? long.MaxValue
                : current + amount;
        }
    }
}
