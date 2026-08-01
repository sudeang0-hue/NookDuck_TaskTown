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
        public const long AutoProductionCoinTarget = 50L;

        private readonly TutorialSaveData progress;

        public event Action<TutorialSaveData> ProgressChanged;
        public event Action<TutorialStep, TutorialStep> StepChanged;
        public event Action Completed;

        public TutorialStep CurrentStep => progress.currentStep;
        public long ManualEarnedCoin => progress.manualEarnedCoin;
        public long AutoProductionEarnedCoin => progress.autoProductionEarnedCoin;
        public int DialogueIndex => progress.dialogueIndex;
        public bool IsCompleted => progress.IsCompleted;
        public bool IsPaused { get; private set; }
        public TutorialSaveData Progress => progress.Copy();
        public bool IsTownWindowGuideCompleted => HasFlag(
            TutorialProgressFlags.TownWindowGuideCompleted);
        public bool IsTownWindowMinimized => HasFlag(
            TutorialProgressFlags.TownWindowMinimized);
        public bool IsTownWindowExpanded => HasFlag(
            TutorialProgressFlags.TownWindowExpanded);
        public bool IsTownWindowRewardGranted => HasFlag(
            TutorialProgressFlags.TownWindowRewardGranted);
        public bool IsToolDrawCoinRewardGranted => HasFlag(
            TutorialProgressFlags.ToolDrawCoinRewardGranted);
        public bool IsTownWindowRewardReady =>
            CurrentStep == TutorialStep.CollapseAndExpandTown &&
            IsTownWindowGuideCompleted &&
            IsTownWindowMinimized &&
            IsTownWindowExpanded &&
            !IsTownWindowRewardGranted;
        public bool IsToolDrawCoinRewardReady =>
            CurrentStep == TutorialStep.DrawAnimal &&
            !IsToolDrawCoinRewardGranted;

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
            if (IsCompleted)
                return false;

            // 축소 화면에서 확장 버튼을 누른 신호만 일시정지 중에도 복원할 수 있습니다.
            if (IsPaused && signalType != TutorialSignalType.TownWindowExpanded)
                return false;

            switch (CurrentStep)
            {
                case TutorialStep.IntroDialogue:
                case TutorialStep.AnimalDrawExplanation:
                case TutorialStep.VillagePlacementExplanation:
                case TutorialStep.UpgradeExplanation:
                case TutorialStep.CompletionDialogue:
                    return TryAdvance(signalType, TutorialSignalType.DialogueCompleted);

                case TutorialStep.EarnManualCoin:
                    return TryAddManualCoin(signalType, amount);

                case TutorialStep.CollapseAndExpandTown:
                    return TryHandleTownWindowStep(signalType);

                case TutorialStep.DrawAnimal:
                    return signalType == TutorialSignalType.AnimalDrawn &&
                           TryCompleteAnimalDrawReward();

                case TutorialStep.DrawTool:
                    return TryAdvance(signalType, TutorialSignalType.ToolDrawn);

                case TutorialStep.AssignAnimal:
                    return TryAdvance(signalType, TutorialSignalType.AnimalAssigned);

                case TutorialStep.ConfirmAutoProduction:
                    return TryAddAutoProductionCoin(signalType, amount);

                case TutorialStep.PlaceAnimalInVillage:
                    return TryAdvance(signalType, TutorialSignalType.VillageAnimalPlaced);

                case TutorialStep.OpenVillageInfo:
                    return TryAdvance(signalType, TutorialSignalType.VillageInfoOpened);

                case TutorialStep.UpgradeVillage:
                    return TryAdvance(signalType, TutorialSignalType.AnyUpgradePurchased);

                default:
                    return false;
            }
        }

        /// <summary>
        /// 현재 진행 위치와 일시정지 여부에 관계없이 튜토리얼 전체를 완료 처리합니다.
        /// 보상 조건은 처리하지 않으며 기존 완료 이벤트 흐름만 재사용합니다.
        /// </summary>
        public bool TrySkipTutorial()
        {
            if (IsCompleted)
                return false;

            AdvanceTo(TutorialStep.Completed);
            return true;
        }

        private bool TryAddManualCoin(TutorialSignalType signalType, long amount)
        {
            if (signalType != TutorialSignalType.ManualCoinEarned || amount <= 0L)
                return false;

            progress.manualEarnedCoin = AddWithoutOverflow(progress.manualEarnedCoin, amount);

            if (progress.manualEarnedCoin >= ManualCoinTarget)
                AdvanceTo(TutorialStep.CollapseAndExpandTown);
            else
                NotifyProgressChanged();

            return true;
        }

        private bool TryAddAutoProductionCoin(
            TutorialSignalType signalType,
            long amount)
        {
            if (signalType != TutorialSignalType.AutoProductionConfirmed || amount <= 0L)
                return false;

            progress.autoProductionEarnedCoin = AddWithoutOverflow(
                progress.autoProductionEarnedCoin,
                amount);

            if (progress.autoProductionEarnedCoin >= AutoProductionCoinTarget)
                AdvanceTo(TutorialStep.VillagePlacementExplanation);
            else
                NotifyProgressChanged();

            return true;
        }

        private bool TryHandleTownWindowStep(TutorialSignalType signalType)
        {
            switch (signalType)
            {
                case TutorialSignalType.DialogueCompleted:
                    if (IsTownWindowGuideCompleted)
                        return false;

                    SetFlag(TutorialProgressFlags.TownWindowGuideCompleted);
                    progress.dialogueIndex = 0;
                    NotifyProgressChanged();
                    return true;

                case TutorialSignalType.TownWindowMinimized:
                    if (!IsTownWindowGuideCompleted || IsTownWindowMinimized)
                        return false;

                    SetFlag(TutorialProgressFlags.TownWindowMinimized);
                    NotifyProgressChanged();
                    return true;

                case TutorialSignalType.TownWindowExpanded:
                    if (!IsTownWindowMinimized || IsTownWindowExpanded)
                        return false;

                    SetFlag(TutorialProgressFlags.TownWindowExpanded);
                    progress.dialogueIndex = 0;
                    NotifyProgressChanged();
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// TutorialManager가 1,500 Town Coin 지급 가능 여부를 확인한 뒤 한 번만 호출합니다.
        /// 플래그와 다음 단계를 같은 상태 변경 안에서 확정해 중복 호출을 차단합니다.
        /// </summary>
        public bool TryCompleteTownWindowReward()
        {
            if (!IsTownWindowRewardReady)
                return false;

            SetFlag(TutorialProgressFlags.TownWindowRewardGranted);
            AdvanceTo(TutorialStep.DrawAnimal);
            return true;
        }

        /// <summary>
        /// 동물 뽑기 성공 뒤 도구 뽑기 비용 지급 여부와 다음 단계를 한 번에 확정합니다.
        /// 실제 1,500 Town Coin 지급은 TutorialManager가 처리합니다.
        /// </summary>
        public bool TryCompleteAnimalDrawReward()
        {
            if (!IsToolDrawCoinRewardReady)
                return false;

            SetFlag(TutorialProgressFlags.ToolDrawCoinRewardGranted);
            AdvanceTo(TutorialStep.AnimalDrawExplanation);
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
                TutorialStep.AnimalDrawExplanation => TutorialStep.DrawTool,
                TutorialStep.DrawAnimal => TutorialStep.DrawTool,
                TutorialStep.DrawTool => TutorialStep.AssignAnimal,
                TutorialStep.AssignAnimal => TutorialStep.ConfirmAutoProduction,
                TutorialStep.ConfirmAutoProduction => TutorialStep.VillagePlacementExplanation,
                TutorialStep.VillagePlacementExplanation => TutorialStep.PlaceAnimalInVillage,
                TutorialStep.PlaceAnimalInVillage => TutorialStep.OpenVillageInfo,
                TutorialStep.OpenVillageInfo => TutorialStep.UpgradeExplanation,
                TutorialStep.UpgradeExplanation => TutorialStep.UpgradeVillage,
                TutorialStep.UpgradeVillage => TutorialStep.CompletionDialogue,
                TutorialStep.CompletionDialogue => TutorialStep.Completed,
                _ => currentStep
            };
        }

        private static bool IsDialogueStep(TutorialStep step)
        {
            return step == TutorialStep.IntroDialogue ||
                   step == TutorialStep.AnimalDrawExplanation ||
                   step == TutorialStep.VillagePlacementExplanation ||
                   step == TutorialStep.UpgradeExplanation ||
                   step == TutorialStep.CompletionDialogue ||
                   step == TutorialStep.CollapseAndExpandTown;
        }

        private bool HasFlag(TutorialProgressFlags flag)
        {
            return progress.HasProgressFlag(flag);
        }

        private void SetFlag(TutorialProgressFlags flag)
        {
            progress.SetProgressFlag(flag);
        }

        private static long AddWithoutOverflow(long current, long amount)
        {
            return current > long.MaxValue - amount
                ? long.MaxValue
                : current + amount;
        }
    }
}
