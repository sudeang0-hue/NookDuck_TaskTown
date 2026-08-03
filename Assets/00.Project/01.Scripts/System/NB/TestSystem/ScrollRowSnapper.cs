using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ScrollRowSnapper : MonoBehaviour, IEndDragHandler
{
    [Header("연결 요소")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform content;

    [Header("스냅 설정")]
    [Tooltip("한 줄의 높이 = 카드 높이(Cell Size Y) + 줄 간격(Spacing Y)")]
    [SerializeField] private float rowHeight = 180f;
    [SerializeField] private float snapSpeed = 12f;

    private bool isSnapping = false;
    private Vector2 targetPosition;

    private void Reset()
    {
        scrollRect = GetComponent<ScrollRect>();
        if (scrollRect != null) content = scrollRect.content;
    }

    private void Update()
    {
        // 드래그가 끝난 후 목표 한 줄 위치로 부드럽게 이동 (Lerp)
        if (isSnapping)
        {
            content.anchoredPosition = Vector2.Lerp(content.anchoredPosition, targetPosition, Time.deltaTime * snapSpeed);

            if (Vector2.Distance(content.anchoredPosition, targetPosition) < 0.5f)
            {
                content.anchoredPosition = targetPosition;
                isSnapping = false;
            }
        }
    }

    // 사용자가 손을 뗐을 때(드래그 종료 시) 작동
    public void OnEndDrag(PointerEventData eventData)
    {
        // 현재 Y 위치 기준 가장 가까운 줄(Row)의 위치 계산
        float currentY = content.anchoredPosition.y;
        float targetY = Mathf.Round(currentY / rowHeight) * rowHeight;

        // 경계선을 넘어가지 않도록 범위 제한
        targetY = Mathf.Clamp(targetY, 0, GetMaxScrollY());

        targetPosition = new Vector2(content.anchoredPosition.x, targetY);

        // ScrollRect 자체 관성 운동을 끄고 커스텀 스냅 동작 시작
        scrollRect.velocity = Vector2.zero;
        isSnapping = true;
    }

    private float GetMaxScrollY()
    {
        float maxScroll = content.rect.height - scrollRect.GetComponent<RectTransform>().rect.height;
        return Mathf.Max(0, maxScroll);
    }
}