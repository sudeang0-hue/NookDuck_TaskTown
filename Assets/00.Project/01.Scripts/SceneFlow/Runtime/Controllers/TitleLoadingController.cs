using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TaskTown.SceneFlow
{
    /// <summary>
    /// Title 화면에서 Startup Pipeline 진행률을 표시하고 완료 시 대상 Scene을 자동 활성화합니다.
    /// </summary>
    public sealed class TitleLoadingController : MonoBehaviour
    {
        [Header("Loading")]
        [SerializeField] private StartupLoadPipeline startupLoadPipeline;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Text statusText;

        [Header("BGM")]
        [SerializeField] private bool playLoadingBgmOnStart = true;
        [SerializeField] private string loadingBgmSoundId = "Title_LodingBGM";
        [SerializeField] private bool stopLoadingBgmOnExit = true;

        private Coroutine activationRoutine;
        private bool loadingBgmStarted;

        private void OnEnable()
        {
            if (startupLoadPipeline == null)
            {
                return;
            }

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

            if (progressSlider != null)
            {
                progressSlider.minValue = 0f;
                progressSlider.maxValue = 1f;
                progressSlider.value = 0f;
            }

            HandleProgressChanged(0f, "시작 준비");
            PlayLoadingBgm();

            if (!startupLoadPipeline.Run())
            {
                StopLoadingBgm();
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
            float clampedProgress = Mathf.Clamp01(progress);

            if (progressSlider != null)
            {
                progressSlider.value = clampedProgress;
            }

            if (statusText != null)
            {
                int percent = Mathf.RoundToInt(clampedProgress * 100f);
                statusText.text = $"{stepName}  {percent}%";
            }
        }

        private void HandleCompleted(StartupLoadContext context)
        {
            HandleProgressChanged(1f, "로딩 완료");

            if (activationRoutine == null)
            {
                activationRoutine = StartCoroutine(ActivateTargetSceneRoutine());
            }
        }

        private IEnumerator ActivateTargetSceneRoutine()
        {
            // 100% UI가 반영된 다음 프레임에 입력 없이 자동 전환합니다.
            yield return null;

            StopLoadingBgm();

            SceneFlowManager manager = SceneFlowManager.EnsureInstance();
            if (!manager.ActivatePreloadedScene())
            {
                ShowFailure(manager.LastError);
            }
        }

        private void HandleFailed(string errorMessage)
        {
            ShowFailure(errorMessage);
        }

        private void ShowFailure(string errorMessage)
        {
            if (statusText != null)
            {
                statusText.text = "로딩 실패";
            }

            Debug.LogError($"[TitleLoadingController] {errorMessage}", this);
        }

        private void PlayLoadingBgm()
        {
            if (!playLoadingBgmOnStart || string.IsNullOrWhiteSpace(loadingBgmSoundId))
            {
                return;
            }

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
            {
                return;
            }

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.StopBGM();
            }

            loadingBgmStarted = false;
        }
    }
}
