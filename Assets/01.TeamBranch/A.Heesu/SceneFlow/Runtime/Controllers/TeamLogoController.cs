using System.Collections;
using UnityEngine;

namespace TaskTown.SceneFlow
{
    /// <summary>
    /// 팀 로고 표시 시간을 관리하고 Title Scene으로 자동 전환합니다.
    /// </summary>
    public sealed class TeamLogoController : MonoBehaviour
    {
        [Header("Logo")]
        [SerializeField] private CanvasGroup logoCanvasGroup;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float fadeInDuration = 0.5f;
        [SerializeField, Min(0f)] private float displayDuration = 1.5f;
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.5f;

        [Header("Flow")]
        [SerializeField] private SceneId nextScene = SceneId.Title;

        private Coroutine logoRoutine;
        private bool skipRequested;

        private void Start()
        {
            if (logoCanvasGroup == null)
            {
                Debug.LogError("[TeamLogoController] Logo CanvasGroup이 연결되지 않았습니다.", this);
                return;
            }

            logoCanvasGroup.alpha = 0f;
            logoCanvasGroup.interactable = false;
            logoCanvasGroup.blocksRaycasts = false;
            logoRoutine = StartCoroutine(PlayLogoRoutine());
        }

        /// <summary>
        /// 추후 스킵 버튼이나 Input Action을 연결할 때 사용할 수 있습니다.
        /// </summary>
        public void Skip()
        {
            skipRequested = true;
        }

        private IEnumerator PlayLogoRoutine()
        {
            yield return FadeRoutine(0f, 1f, fadeInDuration);

            float elapsed = 0f;
            while (elapsed < displayDuration && !skipRequested)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            yield return FadeRoutine(logoCanvasGroup.alpha, 0f, fadeOutDuration);

            SceneFlowManager manager = SceneFlowManager.EnsureInstance();
            if (!manager.LoadScene(nextScene))
            {
                Debug.LogError($"[TeamLogoController] 다음 Scene 전환에 실패했습니다. {manager.LastError}", this);
            }

            logoRoutine = null;
        }

        private IEnumerator FadeRoutine(float from, float to, float duration)
        {
            if (duration <= 0f)
            {
                logoCanvasGroup.alpha = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                logoCanvasGroup.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }

            logoCanvasGroup.alpha = to;
        }

        private void OnDestroy()
        {
            if (logoRoutine != null)
            {
                StopCoroutine(logoRoutine);
                logoRoutine = null;
            }
        }
    }
}
