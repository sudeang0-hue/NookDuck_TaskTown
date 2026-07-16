using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableUIPanel : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [SerializeField] private RectTransform targetPanel;
    [SerializeField] private Canvas canvas;

    private void Awake()
    {
        if (targetPanel == null)
            targetPanel = transform.parent as RectTransform;

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();
    }


    public void OnBeginDrag(PointerEventData eventData)
    {
        targetPanel.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (targetPanel == null || canvas == null) return;

        targetPanel.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }
}
