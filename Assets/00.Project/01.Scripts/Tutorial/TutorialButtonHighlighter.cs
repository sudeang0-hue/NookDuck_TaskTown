using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace TaskTown.Tutorial
{
    /// <summary>
    /// 기존 버튼 입력을 가리지 않고 Scale Pulse로 튜토리얼 대상만 강조합니다.
    /// 단계 종료나 비활성화 시 Tween을 제거하고 원래 Scale로 복구합니다.
    /// </summary>
    public sealed class TutorialButtonHighlighter : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float pulseScale = 1.12f;
        [SerializeField, Min(0.05f)] private float pulseDuration = 0.55f;
        [SerializeField] private Ease pulseEase = Ease.InOutSine;

        private readonly List<HighlightEntry> entries = new();

        private void OnDisable()
        {
            Clear();
        }

        private void OnDestroy()
        {
            Clear();
        }

        public void Highlight(params Button[] buttons)
        {
            Clear();

            if (buttons == null)
                return;

            HashSet<Transform> targets = new();
            foreach (Button button in buttons)
            {
                if (button == null || !targets.Add(button.transform))
                    continue;

                Transform target = button.transform;
                Vector3 originalScale = target.localScale;
                Tween tween = target
                    .DOScale(originalScale * pulseScale, pulseDuration)
                    .SetEase(pulseEase)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true)
                    .SetLink(button.gameObject, LinkBehaviour.KillOnDestroy);

                entries.Add(new HighlightEntry(target, originalScale, tween));
            }
        }

        public void Clear()
        {
            foreach (HighlightEntry entry in entries)
            {
                if (entry.Tween != null && entry.Tween.IsActive())
                    entry.Tween.Kill();

                if (entry.Target != null)
                    entry.Target.localScale = entry.OriginalScale;
            }

            entries.Clear();
        }

        private readonly struct HighlightEntry
        {
            public HighlightEntry(
                Transform target,
                Vector3 originalScale,
                Tween tween)
            {
                Target = target;
                OriginalScale = originalScale;
                Tween = tween;
            }

            public Transform Target { get; }
            public Vector3 OriginalScale { get; }
            public Tween Tween { get; }
        }
    }
}
