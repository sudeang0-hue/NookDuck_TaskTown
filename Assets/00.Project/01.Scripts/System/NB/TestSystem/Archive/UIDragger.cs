//NB

using UnityEngine;
using UnityEngine.EventSystems; // IDragHandler, IPointerDownHandler를 쓰기 위해 필수!

// UIController_AnimalInvPage 패널 드래그 이동 컴포넌트
public class UIDragger : MonoBehaviour, IDragHandler, IPointerDownHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;

    private void Awake()
    {
        // 자신의 RectTransform과 부모 Canvas 컴포넌트를 캐싱
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        // 클릭하는 순간, 해당 UIController_AnimalInvPage 패널을 화면 맨 앞으로 가져옴
        if (rectTransform != null)
        {
            rectTransform.SetAsLastSibling();
        }
    }

    // 드래그하는 동안 매 프레임 실행되는 이벤트
    public void OnDrag(PointerEventData eventData)
    {
        // 마우스의 이동량(delta)을 캔버스 스케일 값으로 나누어 정밀하게 이동 계산
        // 공식: P_new = P_old + (Delta_mouse / Scale_canvas)
        if (canvas != null && rectTransform != null)
        {
            rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
        }
    }
}