using System.Collections;
using UnityEngine;

namespace TaskTown.SceneFlow
{
    /// <summary>
    /// 전역 Manager들의 Awake/Start가 끝난 뒤 첫 화면인 TeamLogoScene으로 진입합니다.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public sealed class BootstrapController : MonoBehaviour
    {
        [SerializeField] private SceneId nextScene = SceneId.TeamLogo;
        [SerializeField] private bool requireSoundManager = true;

        private Coroutine bootstrapRoutine;

        private void Start()
        {
            bootstrapRoutine = StartCoroutine(BootstrapRoutine());
        }

        private IEnumerator BootstrapRoutine()
        {
            // 같은 Scene에 있는 모든 Manager의 Start까지 완료되도록 한 프레임 대기합니다.
            yield return null;

            SceneFlowManager sceneFlowManager = SceneFlowManager.Instance;
            if (sceneFlowManager == null)
            {
                Debug.LogError("[BootstrapController] SceneFlowManager가 준비되지 않았습니다.", this);
                yield break;
            }

            if (SoundManager.Instance == null)
            {
                if (requireSoundManager)
                {
                    Debug.LogError("[BootstrapController] SoundManager가 준비되지 않았습니다.", this);
                    yield break;
                }
            }
            else if (!SoundManager.Instance.ApplySavedSoundSettings())
            {
                Debug.LogWarning(
                    "[BootstrapController] 저장된 사운드 설정을 완전히 적용하지 못했습니다. " +
                    "AudioMixer 연결을 확인해 주세요.",
                    this);
            }

            if (!sceneFlowManager.LoadScene(nextScene))
            {
                Debug.LogError(
                    $"[BootstrapController] 첫 Scene 전환에 실패했습니다. {sceneFlowManager.LastError}",
                    this);
                yield break;
            }

            bootstrapRoutine = null;
        }

        private void OnDestroy()
        {
            if (bootstrapRoutine == null)
            {
                return;
            }

            StopCoroutine(bootstrapRoutine);
            bootstrapRoutine = null;
        }
    }
}
