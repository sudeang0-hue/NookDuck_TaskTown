using DG.Tweening;
using UnityEngine;

public class UITweenManager : MonoBehaviour
{
    public static UITweenManager Instance { get; private set; }


    // 현재 단계 고정 연출값 (추후 확장 시 파라미터화 예정)
    [Header("Panel Open")]
    [SerializeField] private float OpenPeakScale = 1.1f;
    [SerializeField] private float OpenScaleDuration = 0.12f;

    [Header("Button Hover")]
    [SerializeField] private float HoverMoveDistance = 12f;
    [SerializeField] private float HoverMoveDuration = 0.12f;

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

    /// <summary>
    /// 버튼에 마우스를 올렸을 때 오른쪽으로 이동시킨다.
    /// </summary>
    public void PlayButtonHover(
        RectTransform target,
        Vector2 defaultPosition,
        object tweenId)
    {
        if (target == null)
            return;

        DOTween.Kill(tweenId);

        Vector2 hoverPosition =
            defaultPosition + Vector2.right * HoverMoveDistance;

        target.DOAnchorPos(hoverPosition, HoverMoveDuration)
            .SetEase(Ease.OutQuad)
            .SetId(tweenId)
            .SetLink(target.gameObject);
    }

    /// <summary>
    /// 버튼에서 마우스가 벗어나면 원래 위치로 되돌린다.
    /// </summary>
    public void PlayButtonHoverExit(
        RectTransform target,
        Vector2 defaultPosition,
        object tweenId)
    {
        if (target == null)
            return;

        DOTween.Kill(tweenId);

        target.DOAnchorPos(defaultPosition, HoverMoveDuration)
            .SetEase(Ease.OutQuad)
            .SetId(tweenId)
            .SetLink(target.gameObject);
    }

    /// <summary>
    /// 버튼 Hover 트윈을 중지하고 원래 위치로 즉시 되돌린다.
    /// </summary>
    public void StopAndResetButtonHover(
        RectTransform target,
        Vector2 defaultPosition,
        object tweenId)
    {
        DOTween.Kill(tweenId);

        if (target != null)
            target.anchoredPosition = defaultPosition;
    }
}
