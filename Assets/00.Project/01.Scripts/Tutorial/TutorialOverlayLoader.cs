using System;
using System.Collections;
using TaskTown.KDH;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TaskTown.Tutorial
{
    public enum TutorialOverlayLoadState
    {
        Idle = 0,
        Skipped = 1,
        Loading = 2,
        Active = 3,
        Unloading = 4,
        Failed = 5
    }

    /// <summary>
    /// Main Scene이 준비된 뒤 저장된 튜토리얼 진행 상태를 확인하고,
    /// 미완료 사용자에게만 튜토리얼 Scene을 Additive로 로드합니다.
    /// </summary>
    [DefaultExecutionOrder(-70)]
    public sealed class TutorialOverlayLoader : MonoBehaviour
    {
        private const float SceneReadyProgress = 0.9f;

        [Header("튜토리얼 오버레이")]
        // TODO(PR 전): 테스트 Scene 직접 연결을 제거하고 정식 SceneFlow 로드 경로로 교체합니다.
        [SerializeField] private string tutorialSceneName = "Choi_tutorial_Overlay_Test";
        [SerializeField] private bool loadOnStart = true;

        private SaveManager saveManager;
        private TutorialManager activeTutorialManager;
        private AsyncOperation sceneOperation;

        public TutorialOverlayLoadState State { get; private set; } = TutorialOverlayLoadState.Idle;
        public float Progress { get; private set; }
        public string LastError { get; private set; } = string.Empty;
        public bool IsBusy => State == TutorialOverlayLoadState.Loading ||
                              State == TutorialOverlayLoadState.Unloading;

        public event Action<float> LoadProgressChanged;
        public event Action OverlayLoaded;
        public event Action OverlayUnloaded;
        public event Action<string> LoadFailed;

        private void Start()
        {
            if (loadOnStart)
                TryLoadIfRequired();
        }

        private void OnDestroy()
        {
            UnsubscribeTutorialManager();
        }

        /// <summary>
        /// 저장 데이터가 없거나 미완료 상태라면 튜토리얼 오버레이가 필요합니다.
        /// 이전 버전 저장처럼 튜토리얼 필드가 없는 경우도 기본 미완료로 처리합니다.
        /// </summary>
        public static bool ShouldLoad(TutorialSaveData progress)
        {
            return progress == null || !progress.IsCompleted;
        }

        public bool TryLoadIfRequired()
        {
            if (IsBusy || State == TutorialOverlayLoadState.Active)
                return false;

            saveManager = SaveManager.Instance;
            if (saveManager == null)
            {
                SetFailure("SaveManager가 준비되지 않아 튜토리얼 완료 여부를 확인할 수 없습니다.");
                return false;
            }

            if (!ShouldLoad(saveManager.GetTutorialProgressCopy()))
            {
                State = TutorialOverlayLoadState.Skipped;
                SetProgress(1f);
                return false;
            }

            if (string.IsNullOrWhiteSpace(tutorialSceneName))
            {
                SetFailure("튜토리얼 Scene 이름이 비어 있습니다.");
                return false;
            }

            Scene loadedScene = SceneManager.GetSceneByName(tutorialSceneName);
            if (loadedScene.IsValid() && loadedScene.isLoaded)
                return AttachLoadedOverlay(loadedScene);

            if (!Application.CanStreamedLevelBeLoaded(tutorialSceneName))
            {
                SetFailure($"Build Settings에서 활성화된 튜토리얼 Scene을 찾지 못했습니다: {tutorialSceneName}");
                return false;
            }

            sceneOperation = SceneManager.LoadSceneAsync(tutorialSceneName, LoadSceneMode.Additive);
            if (sceneOperation == null)
            {
                SetFailure($"튜토리얼 Scene Additive 로드를 시작하지 못했습니다: {tutorialSceneName}");
                return false;
            }

            LastError = string.Empty;
            State = TutorialOverlayLoadState.Loading;
            SetProgress(0f);
            StartCoroutine(LoadOverlayRoutine());
            return true;
        }

        public bool TryUnloadOverlay()
        {
            if (IsBusy)
                return false;

            Scene scene = SceneManager.GetSceneByName(tutorialSceneName);
            if (!scene.IsValid() || !scene.isLoaded)
                return false;

            UnsubscribeTutorialManager();
            sceneOperation = SceneManager.UnloadSceneAsync(scene);
            if (sceneOperation == null)
            {
                SetFailure($"튜토리얼 Scene 언로드를 시작하지 못했습니다: {tutorialSceneName}");
                return false;
            }

            State = TutorialOverlayLoadState.Unloading;
            StartCoroutine(UnloadOverlayRoutine());
            return true;
        }

        private IEnumerator LoadOverlayRoutine()
        {
            while (!sceneOperation.isDone)
            {
                SetProgress(Mathf.Clamp01(sceneOperation.progress / SceneReadyProgress));
                yield return null;
            }

            sceneOperation = null;
            SetProgress(1f);

            Scene loadedScene = SceneManager.GetSceneByName(tutorialSceneName);
            AttachLoadedOverlay(loadedScene);
        }

        private bool AttachLoadedOverlay(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                SetFailure($"로드 완료 후 튜토리얼 Scene을 찾지 못했습니다: {tutorialSceneName}");
                return false;
            }

            TutorialManager manager = FindTutorialManager(scene);
            if (manager == null)
            {
                SetFailure($"튜토리얼 Scene에 {nameof(TutorialManager)}가 없습니다: {tutorialSceneName}");
                StartCoroutine(UnloadInvalidOverlayRoutine(scene));
                return false;
            }

            activeTutorialManager = manager;
            activeTutorialManager.TutorialCompleted -= HandleTutorialCompleted;
            activeTutorialManager.TutorialCompleted += HandleTutorialCompleted;

            LastError = string.Empty;
            State = TutorialOverlayLoadState.Active;
            SetProgress(1f);
            OverlayLoaded?.Invoke();

            if (activeTutorialManager.IsCompleted)
                HandleTutorialCompleted();

            return true;
        }

        private IEnumerator UnloadOverlayRoutine()
        {
            while (!sceneOperation.isDone)
                yield return null;

            sceneOperation = null;
            State = TutorialOverlayLoadState.Idle;
            OverlayUnloaded?.Invoke();
        }

        private IEnumerator UnloadInvalidOverlayRoutine(Scene scene)
        {
            AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(scene);
            if (unloadOperation != null)
            {
                while (!unloadOperation.isDone)
                    yield return null;
            }

        }

        private void HandleTutorialCompleted()
        {
            // 완료 진행 상태를 메모리에만 남기지 않도록 Scene을 내리기 전에 즉시 저장합니다.
            saveManager?.SaveGame();
            TryUnloadOverlay();
        }

        private void UnsubscribeTutorialManager()
        {
            if (activeTutorialManager == null)
                return;

            activeTutorialManager.TutorialCompleted -= HandleTutorialCompleted;
            activeTutorialManager = null;
        }

        private static TutorialManager FindTutorialManager(Scene scene)
        {
            GameObject[] rootObjects = scene.GetRootGameObjects();
            for (int i = 0; i < rootObjects.Length; i++)
            {
                TutorialManager manager = rootObjects[i].GetComponentInChildren<TutorialManager>(true);
                if (manager != null)
                    return manager;
            }

            return null;
        }

        private void SetProgress(float value)
        {
            Progress = Mathf.Clamp01(value);
            LoadProgressChanged?.Invoke(Progress);
        }

        private void SetFailure(string errorMessage)
        {
            LastError = errorMessage;
            State = TutorialOverlayLoadState.Failed;
            Debug.LogError($"[TutorialOverlayLoader] {errorMessage}", this);
            LoadFailed?.Invoke(errorMessage);
        }
    }
}
