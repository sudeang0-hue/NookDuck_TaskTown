using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TaskTown.SceneFlow
{
    /// <summary>
    /// Title 화면에서 Startup Pipeline 진행률을 표시하고 완료 시 Main Scene을 자동 활성화합니다.
    /// </summary>
    public sealed class TitleLoadingController : MonoBehaviour
    {
        [Header("Loading")]
        [SerializeField] private StartupLoadPipeline startupLoadPipeline;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Text statusText;

        private Coroutine activationRoutine;

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
            startupLoadPipeline.Run();
        }

        private void OnDisable()
        {
            if (startupLoadPipeline != null)
            {
                startupLoadPipeline.ProgressChanged -= HandleProgressChanged;
                startupLoadPipeline.Completed -= HandleCompleted;
                startupLoadPipeline.Failed -= HandleFailed;
            }
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
                activationRoutine = StartCoroutine(ActivateMainSceneRoutine());
            }
        }

        private IEnumerator ActivateMainSceneRoutine()
        {
            // 100% UI가 반영된 다음 프레임에 입력 없이 자동 전환합니다.
            yield return null;

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
    }
}
