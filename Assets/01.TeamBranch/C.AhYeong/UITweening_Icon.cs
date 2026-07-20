using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// button_Panel 등 호버 영역에 마우스가 올라오면 targetIcon을 좌우로 흔든다.
/// </summary>
public class UITweening_Icon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Target")]
    [SerializeField] private RectTransform targetIcon;

    [Header("Shake Settings")]
    [SerializeField] private float shakeAmount = 10f;
    [SerializeField] private float duration = 0.2f;

    private Vector2 _originAnchoredPos;
    private Tween _shakeTween;
    private bool _hasOrigin;

    private void Awake()
    {
        CacheOriginIfNeeded();
    }

    private void OnDisable()
    {
        StopShake(resetToOrigin: true);
    }

    private void OnDestroy()
    {
        StopShake(resetToOrigin: false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        StartShake();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StopShake(resetToOrigin: true);
    }

    private void StartShake()
    {
        if (targetIcon == null)
        {
            Debug.LogWarning($"{nameof(UITweening_Icon)}: targetIcon이 연결되지 않았습니다.", this);
            return;
        }

        CacheOriginIfNeeded();
        StopShake(resetToOrigin: false);

        _shakeTween = targetIcon
            .DOAnchorPosX(_originAnchoredPos.x + shakeAmount, duration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(targetIcon.gameObject);
    }

    private void StopShake(bool resetToOrigin)
    {
        if (_shakeTween != null && _shakeTween.IsActive())
        {
            _shakeTween.Kill();
        }

        _shakeTween = null;

        if (resetToOrigin && targetIcon != null && _hasOrigin)
        {
            targetIcon.anchoredPosition = _originAnchoredPos;
        }
    }

    private void CacheOriginIfNeeded()
    {
        if (_hasOrigin || targetIcon == null)
        {
            return;
        }

        _originAnchoredPos = targetIcon.anchoredPosition;
        _hasOrigin = true;
    }
}
