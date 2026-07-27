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
        private SaveManager saveManager;
        private TutorialStateMachine stateMachine;

        public event Action<TutorialSaveData> ProgressChanged;
        public event Action<TutorialStep, TutorialStep> StepChanged;
        public event Action<bool> PauseChanged;
        public event Action TutorialCompleted;

        public bool IsInitialized => stateMachine != null;
        public TutorialStep CurrentStep => stateMachine?.CurrentStep ?? TutorialStep.IntroDialogue;
        public long ManualEarnedCoin => stateMachine?.ManualEarnedCoin ?? 0L;
        public int DialogueIndex => stateMachine?.DialogueIndex ?? 0;
        public bool IsCompleted => stateMachine?.IsCompleted ?? false;
        public bool IsPaused => stateMachine?.IsPaused ?? false;
        public TutorialSaveData Progress =>
            stateMachine?.Progress ?? TutorialSaveData.CreateDefault();

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

            stateMachine = new TutorialStateMachine(initialProgress);
            stateMachine.ProgressChanged += HandleProgressChanged;
            stateMachine.StepChanged += HandleStepChanged;
            stateMachine.Completed += HandleCompleted;

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
            return TryGetStateMachine(out TutorialStateMachine machine) &&
                   machine.TryHandleSignal(signalType, amount);
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
    }
}
