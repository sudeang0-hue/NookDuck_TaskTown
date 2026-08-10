using DG.Tweening;
using UnityEngine;

namespace TaskTown.Tutorial
{
    /// <summary>
    /// 수동 코인 획득 및 자동 생산 확인 튜토리얼에서만 전체 코인 텍스트에 짧은 Punch 피드백을 재생합니다.
    /// 연속 입력 시 기존 Tween을 종료하고 원래 크기에서 다시 시작하여 Scale 누적을 막습니다.
    /// </summary>
    public sealed class TutorialManualCoinFeedback : MonoBehaviour
    {
        [Header("코인 텍스트 Punch")]
        [SerializeField] private Vector3 punchScale = new(0.14f, 0.14f, 0f);
        [SerializeField, Min(0.01f)] private float duration = 0.22f;
        [SerializeField, Min(1)] private int vibrato = 4;
        [SerializeField, Range(0f, 1f)] private float elasticity = 0.65f;

        private RectTransform target;
        private Vector3 originalScale = Vector3.one;
        private Tween punchTween;

        public void Bind(RectTransform targetRect)
        {
            if (target == targetRect)
                return;

            StopAndRestore();
            target = targetRect;

            if (target != null)
                originalScale = target.localScale;
        }

        public void Play()
        {
            if (target == null || !target.gameObject.activeInHierarchy)
                return;

            StopCurrentTweenAndRestore();

            Tween createdTween = target
                .DOPunchScale(punchScale, duration, vibrato, elasticity)
                .SetId(this)
                .SetUpdate(true)
                .SetLink(target.gameObject, LinkBehaviour.KillOnDestroy);

            punchTween = createdTween;
            createdTween.OnComplete(() =>
            {
                if (punchTween != createdTween)
                    return;

                target.localScale = originalScale;
                punchTween = null;
            });
        }

        public void StopAndRestore()
        {
            StopCurrentTweenAndRestore();
            target = null;
        }

        private void OnDisable()
        {
            StopAndRestore();
        }

        private void OnDestroy()
        {
            StopAndRestore();
        }

        private void StopCurrentTweenAndRestore()
        {
            DOTween.Kill(this, false);

            if (punchTween != null && punchTween.IsActive())
                punchTween.Kill(false);

            punchTween = null;

            if (target != null)
                target.localScale = originalScale;
        }
    }
}
