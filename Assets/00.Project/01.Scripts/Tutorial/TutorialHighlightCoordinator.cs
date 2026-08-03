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
            Highlight(
                step,
                scaleTargets,
                pointerTargets,
                new Vector2(0.5f, 0.5f),
                Vector2.zero);
        }

        public void Highlight(
            TutorialStep step,
            Button[] scaleTargets,
            Button[] pointerTargets,
            Vector2 pointerTargetAnchor,
            Vector2 pointerAdditionalOffset)
        {
            ClearAllHighlights();

            if (config == null ||
                !config.TryGetStepContent(step, out TutorialStepContent content))
            {
                return;
            }

            if (content.UsesHighlightEffect(TutorialHighlightEffect.Pointer))
            {
                pointerIndicator?.Show(
                    content,
                    pointerTargetAnchor,
                    pointerAdditionalOffset,
                    pointerTargets);
            }

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

        /// <summary>
        /// 중앙 마을처럼 UI Button이 아닌 월드 Collider를 손가락 강조 대상으로
        /// 사용합니다. 기존 버튼 Scale 강조와는 독립적으로 동작합니다.
        /// </summary>
        public void HighlightWorldPointer(
            TutorialStep step,
            Collider targetCollider,
            Camera targetCamera = null)
        {
            ClearAllHighlights();

            if (config == null || targetCollider == null ||
                !config.TryGetStepContent(step, out TutorialStepContent content) ||
                !content.UsesHighlightEffect(TutorialHighlightEffect.Pointer))
            {
                return;
            }

            pointerIndicator?.ShowWorld(content, targetCollider, targetCamera);
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
