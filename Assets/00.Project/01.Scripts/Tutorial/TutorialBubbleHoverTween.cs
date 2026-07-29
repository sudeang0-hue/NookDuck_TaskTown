using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TaskTown.Tutorial
{
    /// <summary>
    /// 말풍선에 마우스를 올렸을 때 왼쪽 아래 Pivot을 기준으로 확대합니다.
    /// 튜토리얼 진행 상태는 다루지 않고 표시 연출만 담당합니다.
    /// </summary>
    public sealed class TutorialBubbleHoverTween : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Scale Target")]
        [SerializeField] private RectTransform scaleTarget;

        [Header("Scale")]
        [SerializeField, Range(0.1f, 1f)] private float collapsedScale = 0.5f;
        [SerializeField, Min(1f)] private float expandedScale = 1f;

        [Header("Tween")]
        [SerializeField, Min(0f)] private float expandDuration = 0.2f;
        [SerializeField, Min(0f)] private float collapseDuration = 0.15f;
        [Tooltip("마우스가 말풍선에서 벗어난 뒤 축소를 시작하기까지 기다리는 시간입니다.")]
        [SerializeField, Min(0f)] private float collapseDelay = 2f;
        [SerializeField] private Ease ease = Ease.OutCubic;

        [Header("Punch")]
        [Tooltip("현재 말풍선 크기에 비례해 추가되는 Punch 크기입니다.")]
        [SerializeField, Range(0f, 1f)] private float punchStrength = 0.12f;
        [SerializeField, Min(0f)] private float punchDuration = 0.25f;
        [SerializeField, Min(1)] private int punchVibrato = 6;
        [SerializeField, Range(0f, 1f)] private float punchElasticity = 0.6f;

        private Tween scaleTween;
        private bool isPointerOver;

        private void Reset()
        {
            TryResolveTarget();
        }

        private void Awake()
        {
            TryResolveTarget();
            CollapseImmediate();
        }

        private void OnEnable()
        {
            CollapseImmediate();
        }

        private void OnDisable()
        {
            CollapseImmediate();
        }

        private void OnDestroy()
        {
            KillScaleTween();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isPointerOver = true;
            PlayScale(expandedScale, expandDuration, 0f);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerOver = false;
            PlayScale(collapsedScale, collapseDuration, collapseDelay);
        }

        /// <summary>
        /// 현재 Hover 크기를 기준으로 말풍선이 튀어 오르는 Punch 연출을 재생합니다.
        /// </summary>
        public void PlayPunch()
        {
            if (!TryResolveTarget() || punchStrength <= 0f || punchDuration <= 0f)
                return;

            KillScaleTween();

            // 진행 중인 Punch를 Kill하면 Transform은 중간 Scale에 남습니다.
            // 그 값을 다음 Punch의 기준으로 사용하지 않고 현재 Hover 상태의 절대 Scale로 복원해
            // 짧은 시간에 연속 클릭해도 말풍선 크기가 누적되지 않도록 합니다.
            Vector3 baseScale = GetStableScale();
            scaleTarget.localScale = baseScale;
            Vector3 punchAmount = baseScale * punchStrength;

            Tween punchTween = scaleTarget
                .DOPunchScale(
                    punchAmount,
                    punchDuration,
                    punchVibrato,
                    punchElasticity)
                .SetUpdate(true)
                .SetLink(gameObject);

            TrackScaleTween(punchTween);
            punchTween.OnComplete(() =>
            {
                if (scaleTween != punchTween)
                    return;

                // DOPunchScale의 계산 오차나 중간 재생 상태가 다음 클릭에 남지 않도록
                // 완료 시에도 현재 Hover 상태의 절대 Scale을 보장합니다.
                scaleTarget.localScale = GetStableScale();
                scaleTween = null;
            });
        }

        /// <summary>
        /// 진행 중인 연출을 종료하고 말풍선을 기본 축소 상태로 즉시 복원합니다.
        /// </summary>
        public void CollapseImmediate()
        {
            KillScaleTween();

            if (TryResolveTarget())
                scaleTarget.localScale = Vector3.one * collapsedScale;
        }

        private void PlayScale(float targetScale, float duration, float delay)
        {
            if (!TryResolveTarget())
                return;

            KillScaleTween();

            if (delay <= 0f && duration <= 0f)
            {
                scaleTarget.localScale = Vector3.one * targetScale;
                return;
            }

            Sequence sequence = DOTween.Sequence();
            if (delay > 0f)
                sequence.AppendInterval(delay);

            if (duration > 0f)
                sequence.Append(scaleTarget.DOScale(targetScale, duration).SetEase(ease));
            else
                sequence.AppendCallback(
                    () => scaleTarget.localScale = Vector3.one * targetScale);

            TrackScaleTween(sequence
                .SetUpdate(true)
                .SetLink(gameObject));
        }

        private bool TryResolveTarget()
        {
            if (scaleTarget != null)
                return true;

            return TryGetComponent(out scaleTarget);
        }

        private Vector3 GetStableScale()
        {
            float stableScale = isPointerOver ? expandedScale : collapsedScale;
            return Vector3.one * stableScale;
        }

        private void KillScaleTween()
        {
            if (scaleTween != null && scaleTween.IsActive())
                scaleTween.Kill();

            scaleTween = null;
        }

        private void TrackScaleTween(Tween tween)
        {
            scaleTween = tween;
            tween.OnKill(() =>
            {
                if (scaleTween == tween)
                    scaleTween = null;
            });
        }
    }
}
