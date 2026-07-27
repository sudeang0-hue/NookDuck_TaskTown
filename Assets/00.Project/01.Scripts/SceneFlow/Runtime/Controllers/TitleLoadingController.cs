using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TaskTown.SceneFlow
{
    /// <summary>
    /// Title Scene에서 Startup Pipeline 진행률을 표시하고,
    /// Main Scene과 튜토리얼 오버레이가 준비된 뒤 로딩 화면을 제거합니다.
    /// </summary>
    public sealed class TitleLoadingController : MonoBehaviour
    {
        private const float MainSceneProgressWeight = 0.9f;

        [Header("Loading")]
        [SerializeField] private StartupLoadPipeline startupLoadPipeline;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Text statusText;

        [Header("Handoff")]
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.2f;

        [Header("BGM")]
        [SerializeField] private bool playLoadingBgmOnStart = true;
        [SerializeField] private string loadingBgmSoundId = "Title_LodingBGM";
        [Tooltip("활성화하면 Title 종료 시 BGM을 정지합니다. 공용 배경 BGM은 비활성화 상태로 유지합니다.")]
        [SerializeField] private bool stopLoadingBgmOnExit = false;

        private Coroutine activationRoutine;
        private bool loadingBgmStarted;
        private StartupLoadingHandoff loadingHandoff;

        private void OnEnable()
        {
            if (startupLoadPipeline == null)
                return;

            startupLoadPipeline.ProgressChanged += HandleProgressChanged;
            startupLoadPipeline.Completed += HandleCompleted;
            startupLoadPipeline.Failed += HandleFailed;
        }

        private void Start()
        {
            if (startupLoadPipeline == null)
            {
                ShowFailure("StartupLoadPipeline이 연결되지 않았습니다.");
                return;
            }

            Canvas loadingCanvas = ResolveLoadingCanvas();
            loadingHandoff = GetComponent<StartupLoadingHandoff>();
            if (loadingHandoff == null)
                loadingHandoff = gameObject.AddComponent<StartupLoadingHandoff>();

            if (loadingCanvas == null || !loadingHandoff.Prepare(loadingCanvas, fadeOutDuration))
            {
                ShowFailure("Title 로딩 Canvas를 Main Scene까지 인계할 수 없습니다.");
                return;
            }

            if (progressSlider != null)
            {
                progressSlider.minValue = 0f;
                progressSlider.maxValue = 1f;
                progressSlider.value = 0f;
            }

            ShowProgress(0f, "시작 준비");
            PlayLoadingBgm();

            if (!startupLoadPipeline.Run())
            {
                StopLoadingBgm();
                loadingHandoff.Cancel();
            }
        }

        private void OnDisable()
        {
            if (startupLoadPipeline != null)
            {
                startupLoadPipeline.ProgressChanged -= HandleProgressChanged;
                startupLoadPipeline.Completed -= HandleCompleted;
                startupLoadPipeline.Failed -= HandleFailed;
            }

            StopLoadingBgm();
        }

        private void HandleProgressChanged(float progress, string stepName)
        {
            ShowProgress(Mathf.Clamp01(progress) * MainSceneProgressWeight, stepName);
        }

        private void ShowProgress(float progress, string stepName)
        {
            float clampedProgress = Mathf.Clamp01(progress);

            if (progressSlider != null)
                progressSlider.value = clampedProgress;

            if (statusText != null)
            {
                int percent = Mathf.RoundToInt(clampedProgress * 100f);
                statusText.text = $"{stepName}  {percent}%";
            }
        }

        private void HandleCompleted(StartupLoadContext context)
        {
            ShowProgress(MainSceneProgressWeight, "Main Scene 활성화 준비");

            if (activationRoutine == null)
                activationRoutine = StartCoroutine(ActivateTargetSceneRoutine());
        }

        private IEnumerator ActivateTargetSceneRoutine()
        {
            // 90% UI를 한 프레임 표시한 뒤 Main Scene을 활성화합니다.
            yield return null;

            SceneFlowManager manager = SceneFlowManager.EnsureInstance();
            if (!manager.ActivatePreloadedScene())
            {
                ShowFailure(manager.LastError);
                loadingHandoff?.Cancel();
                yield break;
            }

            if (loadingHandoff != null)
                yield return loadingHandoff.CompleteAfterSceneActivation(manager, ShowProgress);

            StopLoadingBgm();
        }

        private void HandleFailed(string errorMessage)
        {
            ShowFailure(errorMessage);
            loadingHandoff?.Cancel();
        }

        private void ShowFailure(string errorMessage)
        {
            if (statusText != null)
                statusText.text = "로딩 실패";

            Debug.LogError($"[TitleLoadingController] {errorMessage}", this);
        }

        private Canvas ResolveLoadingCanvas()
        {
            if (progressSlider != null)
            {
                Canvas sliderCanvas = progressSlider.GetComponentInParent<Canvas>();
                if (sliderCanvas != null)
                    return sliderCanvas;
            }

            return statusText != null ? statusText.GetComponentInParent<Canvas>() : null;
        }

        private void PlayLoadingBgm()
        {
            if (!playLoadingBgmOnStart || string.IsNullOrWhiteSpace(loadingBgmSoundId))
                return;

            if (SoundManager.Instance == null)
            {
                Debug.LogWarning(
                    "[TitleLoadingController] SoundManager가 준비되지 않아 Title 로딩 BGM을 재생하지 못했습니다. " +
                    "BootstrapScene에서 시작했는지 확인해 주세요.",
                    this);
                return;
            }

            if (!SoundManager.Instance.PlayBGM(loadingBgmSoundId))
            {
                Debug.LogWarning(
                    $"[TitleLoadingController] Title 로딩 BGM 재생에 실패했습니다: {loadingBgmSoundId}",
                    this);
                return;
            }

            loadingBgmStarted = true;
        }

        private void StopLoadingBgm()
        {
            if (!stopLoadingBgmOnExit || !loadingBgmStarted)
                return;

            if (SoundManager.Instance != null)
                SoundManager.Instance.StopBGM();

            loadingBgmStarted = false;
        }
    }
}
