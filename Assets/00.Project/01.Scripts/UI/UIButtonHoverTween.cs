using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonHoverTween : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Reference")]
    private RectTransform moveTarget;
    private Vector2 defaultPosition;
    private readonly object hoverTweenId = new object();

    private void Awake()
    {
        moveTarget = GetComponent<RectTransform>();

        if (moveTarget == null)
        {
            Debug.LogWarning($"[UIButtonHoverTween] 이동 대상 RectTransform이 없습니다: {name}");
            return;
        }

        defaultPosition = moveTarget.anchoredPosition;
    }

    private void OnDisable()
    {
        if (moveTarget == null)
            return;

        UITweenManager.Instance?.StopAndResetButtonHover(
            moveTarget,
            defaultPosition,
            hoverTweenId);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (moveTarget == null)
            return;

        UITweenManager.Instance?.PlayButtonHover(
            moveTarget,
            defaultPosition,
            hoverTweenId);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (moveTarget == null)
            return;

        UITweenManager.Instance?.PlayButtonHoverExit(
            moveTarget,
            defaultPosition,
            hoverTweenId);
    }
}
