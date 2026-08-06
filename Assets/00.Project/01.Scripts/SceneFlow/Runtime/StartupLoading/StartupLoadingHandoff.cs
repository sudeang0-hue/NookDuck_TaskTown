using System;
using System.Collections;
using TaskTown.Tutorial;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace TaskTown.SceneFlow
{
    /// <summary>
    /// Title 로딩 화면을 Main Scene 활성화 이후까지 유지하고,
    /// 튜토리얼 오버레이 준비가 끝난 뒤 입력과 화면을 인계합니다.
    /// </summary>
    [DefaultExecutionOrder(-20000)]
    [DisallowMultipleComponent]
    public sealed class StartupLoadingHandoff : MonoBehaviour
    {
        private const string InputGateOwner = "StartupLoadingHandoff";
        private const int MaxLoaderResolveFrames = 30;
        private const int LoadingCanvasSortingOrder = short.MaxValue;

        private Canvas loadingCanvas;
        private CanvasGroup loadingCanvasGroup;
        private float fadeOutDuration;
        private bool gateHeld;
        private bool isPrepared;

        public bool IsPrepared => isPrepared;

        public bool Prepare(Canvas canvas, float fadeDuration)
        {
            if (isPrepared || canvas == null)
                return false;

            loadingCanvas = canvas;
            fadeOutDuration = Mathf.Max(0f, fadeDuration);

            DontDestroyOnLoad(gameObject);
            if (loadingCanvas.gameObject != gameObject)
                DontDestroyOnLoad(loadingCanvas.gameObject);

            loadingCanvas.overrideSorting = true;
            loadingCanvas.sortingOrder = LoadingCanvasSortingOrder;

            if (!loadingCanvas.TryGetComponent(out loadingCanvasGroup))
                loadingCanvasGroup = loadingCanvas.gameObject.AddComponent<CanvasGroup>();

            loadingCanvasGroup.alpha = 1f;
            loadingCanvasGroup.interactable = true;
            loadingCanvasGroup.blocksRaycasts = true;

            EnsurePointerBlocker();
            AcquireInputGate();
            isPrepared = true;
            return true;
        }

        public IEnumerator CompleteAfterSceneActivation(
            SceneFlowManager sceneFlowManager,
            Action<float, string> reportProgress)
        {
            //-----------------26.08.05 KDH-------------------------
            return CompleteAfterSceneActivation(
                sceneFlowManager,
                reportProgress,
                expectTutorialOverlay: true);
        }

        /// <param name="expectTutorialOverlay">
        /// Main처럼 튜토리얼 로더가 있는 씬만 true.
        /// DifficultySelect 등에는 false로 두어 잘못된 경고를 막습니다.
        /// </param>
        public IEnumerator CompleteAfterSceneActivation(
            SceneFlowManager sceneFlowManager,
            Action<float, string> reportProgress,
            bool expectTutorialOverlay)
        {
            //-----------------------------------------------------
            while (sceneFlowManager != null &&
                   (sceneFlowManager.State == SceneLoadState.Loading ||
                    sceneFlowManager.State == SceneLoadState.ReadyToActivate ||
                    sceneFlowManager.State == SceneLoadState.Activating))
            {
                yield return null;
            }

            if (sceneFlowManager != null && sceneFlowManager.State == SceneLoadState.Failed)
            {
                Debug.LogError(
                    "[StartupLoadingHandoff] Main Scene 활성화에 실패했습니다. " +
                    sceneFlowManager.LastError,
                    this);
                ReleaseInputGate();
                yield break;
            }

            // 새 Scene의 Start가 모두 실행되고 TutorialOverlayLoader가 로드를 시작할 기회를 줍니다.
            yield return null;

            if (expectTutorialOverlay)  // 26.08.05 KDH if Add
            {
                TutorialOverlayLoader overlayLoader = null;
                for (int frame = 0; frame < MaxLoaderResolveFrames && overlayLoader == null; frame++)
                {
                    overlayLoader = FindFirstObjectByType<TutorialOverlayLoader>();
                    if (overlayLoader == null)
                        yield return null;
                }

                if (overlayLoader == null)
                {
                    Debug.LogWarning(
                        "[StartupLoadingHandoff] Main Scene에서 TutorialOverlayLoader를 찾지 못했습니다. " +
                        "튜토리얼 로딩을 건너뛰고 게임 입력을 허용합니다.",
                        this);
                }
                else
                {
                    if (overlayLoader.State == TutorialOverlayLoadState.Idle)
                        overlayLoader.TryLoadIfRequired();

                    while (!overlayLoader.IsStartupReady)
                    {
                        float tutorialProgress = Mathf.Clamp01(overlayLoader.Progress);
                        reportProgress?.Invoke(
                            Mathf.Lerp(0.9f, 1f, tutorialProgress),
                            "튜토리얼 준비 중");
                        yield return null;
                    }

                    if (overlayLoader.State == TutorialOverlayLoadState.Failed)
                    {
                        Debug.LogWarning(
                            "[StartupLoadingHandoff] 튜토리얼 오버레이 준비에 실패했지만 Main 게임은 계속 실행합니다. " +
                            overlayLoader.LastError,
                            overlayLoader);
                    }
                }
            }

            reportProgress?.Invoke(1f, "로딩 완료");
            yield return FadeOutRoutine();

            ReleaseInputGate();
            CleanupPersistentObjects();
        }

        public void Cancel()
        {
            ReleaseInputGate();
            CleanupPersistentObjects();
        }

        private void OnDestroy()
        {
            ReleaseInputGate();
        }

        private void EnsurePointerBlocker()
        {
            GameObject blocker = new(
                "[StartupInputBlocker]",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(UIClickBlocker));

            RectTransform rectTransform = blocker.GetComponent<RectTransform>();
            rectTransform.SetParent(loadingCanvas.transform, false);
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.SetAsLastSibling();

            Image image = blocker.GetComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = true;
        }

        private IEnumerator FadeOutRoutine()
        {
            if (loadingCanvasGroup == null || fadeOutDuration <= 0f)
                yield break;

            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                loadingCanvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeOutDuration);
                yield return null;
            }

            loadingCanvasGroup.alpha = 0f;
        }

        private void AcquireInputGate()
        {
            if (gateHeld)
                return;

            GameInputGate.Block(InputGateOwner);
            gateHeld = true;
        }

        private void ReleaseInputGate()
        {
            if (!gateHeld)
                return;

            GameInputGate.Release(InputGateOwner);
            gateHeld = false;
        }

        private void CleanupPersistentObjects()
        {
            if (loadingCanvas != null && loadingCanvas.gameObject != gameObject)
                Destroy(loadingCanvas.gameObject);

            loadingCanvas = null;
            loadingCanvasGroup = null;

            if (this != null && gameObject != null)
                Destroy(gameObject);
        }
    }
}
