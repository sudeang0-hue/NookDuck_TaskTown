using UnityEngine;
using UnityEngine.UI;

namespace TaskTown.Tutorial
{
    /// <summary>
    /// 단계 설정에 따라 Scale Pulse와 손가락 강조를 독립적으로 조합합니다.
    /// 각 효과의 대상 목록이 달라도 서로의 Tween에는 영향을 주지 않습니다.
    /// </summary>
    public sealed class TutorialHighlightCoordinator : MonoBehaviour
    {
        [SerializeField] private TutorialConfigSO config;
        [SerializeField] private TutorialButtonHighlighter scaleHighlighter;
        [SerializeField] private TutorialPointerIndicator pointerIndicator;

        private void OnDisable()
        {
            ClearAllHighlights();
        }

        private void OnDestroy()
        {
            ClearAllHighlights();
        }

        public void Highlight(
            TutorialStep step,
            Button[] scaleTargets,
            Button[] pointerTargets)
        {
            ClearAllHighlights();

            if (config == null ||
                !config.TryGetStepContent(step, out TutorialStepContent content))
            {
                return;
            }

            if (content.UsesHighlightEffect(TutorialHighlightEffect.Pointer))
                pointerIndicator?.Show(content, pointerTargets);

            if (content.UsesHighlightEffect(TutorialHighlightEffect.ScalePulse))
                scaleHighlighter?.Highlight(scaleTargets);
        }

        public void HighlightScale(params Button[] targets)
        {
            scaleHighlighter?.Highlight(targets);
        }

        public void HighlightPointer(TutorialStep step, params Button[] targets)
        {
            if (config != null &&
                config.TryGetStepContent(step, out TutorialStepContent content))
            {
                pointerIndicator?.Show(content, targets);
            }
        }

        public void ClearScaleHighlight()
        {
            scaleHighlighter?.Clear();
        }

        public void HidePointerHighlight()
        {
            pointerIndicator?.Hide();
        }

        public void ClearAllHighlights()
        {
            ClearScaleHighlight();
            HidePointerHighlight();
        }
    }
}
