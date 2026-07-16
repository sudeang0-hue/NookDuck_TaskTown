using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace TaskTown.SceneFlow
{
    /// <summary>
    /// Scene 전환과 비동기 사전 로드만 담당하는 영구 Manager입니다.
    /// 저장 데이터 처리와 화면 UI는 별도 시스템에서 담당합니다.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class SceneFlowManager : MonoBehaviour
    {
        private const string CatalogResourcePath = "SceneFlow/SceneCatalog";
        private const float ReadyProgress = 0.9f;

        private AsyncOperation currentOperation;
        private Coroutine loadCoroutine;
        private SceneId currentTarget;
        private SceneCatalogSO sceneCatalog;

        public static SceneFlowManager Instance { get; private set; }

        public SceneLoadState State { get; private set; } = SceneLoadState.Idle;
        public float Progress { get; private set; }
        public bool IsBusy => State == SceneLoadState.Loading ||
                              State == SceneLoadState.ReadyToActivate ||
                              State == SceneLoadState.Activating;
        public string LastError { get; private set; } = string.Empty;

        public event Action<float> LoadProgressChanged;
        public event Action<SceneId> SceneReady;
        public event Action<SceneId> SceneLoadCompleted;
        public event Action<string> LoadFailed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        public static SceneFlowManager EnsureInstance()
        {
            if (Instance != null)
            {
                return Instance;
            }

            SceneFlowManager existing = FindFirstObjectByType<SceneFlowManager>();
            if (existing != null)
            {
                Instance = existing;
                return existing;
            }

            GameObject managerObject = new("[SceneFlowManager]");
            return managerObject.AddComponent<SceneFlowManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            sceneCatalog = Resources.Load<SceneCatalogSO>(CatalogResourcePath);

            if (sceneCatalog == null)
            {
                SetFailure($"Resources/{CatalogResourcePath}.asset을 찾지 못했습니다.");
            }
        }

        public bool TryGetSceneName(SceneId sceneId, out string sceneName)
        {
            if (sceneCatalog == null)
            {
                sceneName = string.Empty;
                return false;
            }

            return sceneCatalog.TryGetSceneName(sceneId, out sceneName);
        }

        public bool CanLoadScene(SceneId sceneId, out string errorMessage)
        {
            if (sceneCatalog == null)
            {
                errorMessage = "SceneCatalogSO가 연결되지 않았습니다.";
                return false;
            }

            if (!sceneCatalog.TryValidate(out errorMessage))
            {
                return false;
            }

            if (!TryGetSceneName(sceneId, out string sceneName))
            {
                errorMessage = $"SceneId에 해당하는 Scene 이름이 없습니다: {sceneId}";
                return false;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                errorMessage = $"Build Settings에서 활성화된 Scene을 찾지 못했습니다: {sceneName}";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        public bool LoadScene(SceneId sceneId)
        {
            if (!TryBeginLoad(sceneId, out string sceneName))
            {
                return false;
            }

            loadCoroutine = StartCoroutine(LoadAndActivateRoutine(sceneName));
            return true;
        }

        public bool PreloadScene(SceneId sceneId)
        {
            if (!TryBeginLoad(sceneId, out string sceneName))
            {
                return false;
            }

            loadCoroutine = StartCoroutine(PreloadRoutine(sceneName));
            return true;
        }

        public bool ActivatePreloadedScene()
        {
            if (State != SceneLoadState.ReadyToActivate || currentOperation == null)
            {
                SetFailure("활성화할 준비가 끝난 Scene이 없습니다.");
                return false;
            }

            loadCoroutine = StartCoroutine(ActivatePreloadedRoutine());
            return true;
        }

        private bool TryBeginLoad(SceneId sceneId, out string sceneName)
        {
            sceneName = string.Empty;

            if (IsBusy)
            {
                LastError = $"이미 Scene 전환이 진행 중입니다: {currentTarget}";
                Debug.LogWarning($"[SceneFlowManager] {LastError}", this);
                return false;
            }

            if (!CanLoadScene(sceneId, out string errorMessage))
            {
                SetFailure(errorMessage);
                return false;
            }

            TryGetSceneName(sceneId, out sceneName);
            currentTarget = sceneId;
            currentOperation = null;
            loadCoroutine = null;
            LastError = string.Empty;
            State = SceneLoadState.Loading;
            SetProgress(0f);
            return true;
        }

        private IEnumerator LoadAndActivateRoutine(string sceneName)
        {
            currentOperation = UnitySceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (currentOperation == null)
            {
                SetFailure($"Scene 비동기 로드를 시작하지 못했습니다: {sceneName}");
                yield break;
            }

            while (!currentOperation.isDone)
            {
                SetProgress(Mathf.Clamp01(currentOperation.progress / ReadyProgress));
                yield return null;
            }

            CompleteLoad();
        }

        private IEnumerator PreloadRoutine(string sceneName)
        {
            currentOperation = UnitySceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (currentOperation == null)
            {
                SetFailure($"Scene 사전 로드를 시작하지 못했습니다: {sceneName}");
                yield break;
            }

            currentOperation.allowSceneActivation = false;

            while (currentOperation.progress < ReadyProgress)
            {
                SetProgress(Mathf.Clamp01(currentOperation.progress / ReadyProgress));
                yield return null;
            }

            SetProgress(1f);
            State = SceneLoadState.ReadyToActivate;
            loadCoroutine = null;
            SceneReady?.Invoke(currentTarget);
        }

        private IEnumerator ActivatePreloadedRoutine()
        {
            State = SceneLoadState.Activating;
            currentOperation.allowSceneActivation = true;

            while (!currentOperation.isDone)
            {
                yield return null;
            }

            CompleteLoad();
        }

        private void CompleteLoad()
        {
            SceneId completedScene = currentTarget;
            SetProgress(1f);
            currentOperation = null;
            loadCoroutine = null;
            State = SceneLoadState.Idle;
            SceneLoadCompleted?.Invoke(completedScene);
        }

        private void SetProgress(float value)
        {
            Progress = Mathf.Clamp01(value);
            LoadProgressChanged?.Invoke(Progress);
        }

        private void SetFailure(string errorMessage)
        {
            LastError = errorMessage;
            currentOperation = null;
            loadCoroutine = null;
            State = SceneLoadState.Failed;
            Debug.LogError($"[SceneFlowManager] {errorMessage}", this);
            LoadFailed?.Invoke(errorMessage);
        }
    }
}
