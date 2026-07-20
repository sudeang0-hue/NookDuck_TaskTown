using DG.Tweening;
using UnityEngine;

public class UITweenManager : MonoBehaviour
{
    public static UITweenManager Instance { get; private set; }

    // 현재 단계 고정 연출값 (추후 확장 시 파라미터화 예정)
    [SerializeField] private float OpenPeakScale = 1.1f;
    [SerializeField] private float OpenScaleDuration = 0.12f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// 패널 오픈 시 scale을 잠시 키웠다가 원래 크기로 되돌린다.
    /// </summary>
    public void PlayOpenScale(RectTransform target)
    {
        if (target == null)
            return;

        target.DOKill();
        target.localScale = Vector3.one;

        DOTween.Sequence()
            .Append(target.DOScale(OpenPeakScale, OpenScaleDuration).SetEase(Ease.OutQuad))
            .Append(target.DOScale(1f, OpenScaleDuration).SetEase(Ease.InOutQuad))
            .SetLink(target.gameObject);
    }

    /// <summary>
    /// 진행 중인 scale 트윈을 중지하고 기본 scale로 되돌린다.
    /// </summary>
    public void StopAndReset(RectTransform target)
    {
        if (target == null)
            return;

        target.DOKill();
        target.localScale = Vector3.one;
    }
}
