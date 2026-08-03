using System;
using TaskTown.KDH;
using UnityEngine;

namespace TaskTown.Tutorial
{
    /// <summary>
    /// 튜토리얼 Scene의 수명 동안 상태 머신과 저장 진행 상태를 연결합니다.
    /// Update나 전역 이벤트 구독 없이 TutorialEventBridge가 전달한 신호만 처리합니다.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public sealed class TutorialManager : MonoBehaviour
    {
        public const long TownWindowCompletionReward = 1500L;
        public const long ToolDrawCoinReward = 1500L;

        private SaveManager saveManager;
        private TutorialStateMachine stateMachine;

        public event Action<TutorialSaveData> ProgressChanged;
        public event Action<TutorialStep, TutorialStep> StepChanged;
        public event Action<bool> PauseChanged;
        public event Action TutorialRestarted;
        public event Action TutorialCompleted;

        public bool IsInitialized => stateMachine != null;
        public TutorialStep CurrentStep => stateMachine?.CurrentStep ?? TutorialStep.IntroDialogue;
        public long ManualEarnedCoin => stateMachine?.ManualEarnedCoin ?? 0L;
        public long AutoProductionEarnedCoin =>
            stateMachine?.AutoProductionEarnedCoin ?? 0L;
        public int DialogueIndex => stateMachine?.DialogueIndex ?? 0;
        public bool IsCompleted => stateMachine?.IsCompleted ?? false;
        public bool IsPaused => stateMachine?.IsPaused ?? false;
        public TutorialSaveData Progress =>
            stateMachine?.Progress ?? TutorialSaveData.CreateDefault();
        public bool IsTownWindowGuideCompleted =>
            stateMachine?.IsTownWindowGuideCompleted ?? false;
        public bool IsTownWindowMinimized =>
            stateMachine?.IsTownWindowMinimized ?? false;
        public bool IsTownWindowExpanded =>
            stateMachine?.IsTownWindowExpanded ?? false;
        public bool IsTownWindowRewardGranted =>
            stateMachine?.IsTownWindowRewardGranted ?? false;
        public bool IsToolDrawCoinRewardGranted =>
            stateMachine?.IsToolDrawCoinRewardGranted ?? false;

        private void Start()
        {
            Initialize(SaveManager.Instance);
        }

        private void OnDestroy()
        {
            UnsubscribeStateMachine();
        }

        /// <summary>
        /// SaveManager의 Start 로드 이후 실행되어 저장된 튜토리얼 진행 상태를 복원합니다.
        /// </summary>
        public void Initialize(SaveManager progressSaveManager)
        {
            if (IsInitialized)
                return;

            saveManager = progressSaveManager;
            TutorialSaveData initialProgress = saveManager != null
                ? saveManager.GetTutorialProgressCopy()
                : TutorialSaveData.CreateDefault();

            ReplaceStateMachine(initialProgress);

            if (saveManager == null)
            {
                Debug.LogWarning(
                    "[TutorialManager] SaveManager가 없어 기본 진행 상태로 시작합니다. " +
                    "현재 진행은 디스크 저장에 포함되지 않습니다.",
                    this);
            }
        }

        public void SetPaused(bool isPaused)
        {
            if (!TryGetStateMachine(out TutorialStateMachine machine))
                return;

            if (machine.IsPaused == isPaused)
                return;

            machine.SetPaused(isPaused);
            PauseChanged?.Invoke(isPaused);
        }

        public bool TrySetDialogueIndex(int dialogueIndex)
        {
            return TryGetStateMachine(out TutorialStateMachine machine) &&
                   machine.TrySetDialogueIndex(dialogueIndex);
        }

        public bool ReportSignal(TutorialSignalType signalType, long amount = 0L)
        {
            if (!TryGetStateMachine(out TutorialStateMachine machine))
                return false;

            if (signalType == TutorialSignalType.AnimalDrawn &&
                machine.IsAnimalDrawStepCompletionReady)
            {
                return TryCompleteAnimalDrawStep(machine);
            }

            if (signalType == TutorialSignalType.DialogueCompleted &&
                machine.IsTownWindowStepCompletionReady)
            {
                return TryCompleteTownWindowStep(machine);
            }

            return machine.TryHandleSignal(signalType, amount);
        }

        /// <summary>
        /// 튜토리얼 전체를 보상 지급 없이 완료합니다.
        /// 완료 저장과 Overlay 언로드는 기존 TutorialCompleted 흐름에서 처리합니다.
        /// </summary>
        public bool TrySkipTutorial()
        {
            return TryGetStateMachine(out TutorialStateMachine machine) &&
                   machine.TrySkipTutorial();
        }

        /// <summary>
        /// 이미 받은 일회성 보상 기록을 보존한 진행 데이터로 현재 Overlay를 처음부터 다시 시작합니다.
        /// 설정 UI는 SaveManager를 직접 초기화하지 않고 TutorialOverlayLoader의 재보기 API를 호출해야 합니다.
        /// </summary>
        public bool TryRestartTutorial(TutorialSaveData replayProgress)
        {
            if (!IsInitialized || replayProgress == null)
                return false;

            bool wasPaused = IsPaused;
            ReplaceStateMachine(replayProgress);

            TutorialSaveData restartedProgress = Progress;
            saveManager?.SetTutorialProgress(restartedProgress);
            TutorialRestarted?.Invoke();
            ProgressChanged?.Invoke(restartedProgress.Copy());

            if (wasPaused)
                PauseChanged?.Invoke(false);

            return true;
        }

        private bool TryCompleteAnimalDrawStep(
            TutorialStateMachine machine)
        {
            bool shouldGrantReward = machine.IsToolDrawCoinRewardReady;
            CoinManager coinManager = shouldGrantReward ? CoinManager.Instance : null;
            if (shouldGrantReward && coinManager == null)
            {
                Debug.LogWarning(
                    "[TutorialManager] CoinManager가 없어 도구 뽑기용 보상을 " +
                    "지급하지 못했습니다.",
                    this);
                return false;
            }

            // 단계와 보상 플래그를 먼저 확정해 빠른 연속 신호의 중복 처리를 차단합니다.
            if (!machine.TryCompleteAnimalDrawReward())
                return false;

            if (shouldGrantReward)
                coinManager.Add(ToolDrawCoinReward);

            saveManager?.SaveGame();
            return true;
        }

        private bool TryCompleteTownWindowStep(
            TutorialStateMachine machine)
        {
            bool shouldGrantReward = machine.IsTownWindowRewardReady;
            CoinManager coinManager = shouldGrantReward ? CoinManager.Instance : null;
            if (shouldGrantReward && coinManager == null)
            {
                Debug.LogWarning(
                    "[TutorialManager] CoinManager가 없어 축소·확장 완료 보상을 " +
                    "지급하지 못했습니다.",
                    this);
                return false;
            }

            // 이미 지급된 다시 보기라면 단계만 진행하고, 최초 진행일 때만 실제 코인을 추가합니다.
            if (!machine.TryCompleteTownWindowReward())
                return false;

            if (shouldGrantReward)
                coinManager.Add(TownWindowCompletionReward);

            saveManager?.SaveGame();
            return true;
        }

        private bool TryGetStateMachine(out TutorialStateMachine machine)
        {
            machine = stateMachine;
            if (machine != null)
                return true;

            Debug.LogWarning(
                "[TutorialManager] 초기화 전에는 튜토리얼 진행 신호를 처리할 수 없습니다.",
                this);
            return false;
        }

        private void HandleProgressChanged(TutorialSaveData progress)
        {
            // SetTutorialProgress는 메모리 복사본만 갱신합니다. 실제 파일 쓰기는 기존 자동 저장 주기를 따릅니다.
            saveManager?.SetTutorialProgress(progress);
            ProgressChanged?.Invoke(progress.Copy());
        }

        private void HandleStepChanged(TutorialStep previousStep, TutorialStep nextStep)
        {
            StepChanged?.Invoke(previousStep, nextStep);
        }

        private void HandleCompleted()
        {
            TutorialCompleted?.Invoke();
        }

        private void UnsubscribeStateMachine()
        {
            if (stateMachine == null)
                return;

            stateMachine.ProgressChanged -= HandleProgressChanged;
            stateMachine.StepChanged -= HandleStepChanged;
            stateMachine.Completed -= HandleCompleted;
        }

        private void ReplaceStateMachine(TutorialSaveData progress)
        {
            UnsubscribeStateMachine();
            stateMachine = new TutorialStateMachine(progress);
            stateMachine.ProgressChanged += HandleProgressChanged;
            stateMachine.StepChanged += HandleStepChanged;
            stateMachine.Completed += HandleCompleted;
        }
    }
}
