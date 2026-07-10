using System;
using UnityEngine;
using UnityEngine.EventSystems;

public enum GameMenuState
{
    Compact,
    ExpandedTown,
    Gacha,
    AnimalDex,
    ToolPlacement,
    VillageUpgrade,
    MiniGame
}

namespace KAY
{

    public class UIPanelWindow : MonoBehaviour, IBeginDragHandler, IDragHandler, IPointerDownHandler
    {

        [SerializeField] private RectTransform panelRect;
        private Vector2 defaultUIPanelPosition; // UI 패널의 초기 위치
        private Vector2 dragOffset;

        private bool isInitialized;


        private void Awake()
        {
            Initialize();

        }

        private void Initialize()
        {
            if (isInitialized)
                return;

            if (panelRect == null)
                panelRect = GetComponent<RectTransform>();

            if (panelRect == null)
            {
                Debug.LogWarning($"[UIPanelWindow] RectTransform이 없습니다 : {name}");
                return;
            }

            defaultUIPanelPosition = panelRect.anchoredPosition;
            isInitialized = true;
        }


        /// <summary>
        /// 초기 위치에서 패널 활성화
        /// </summary>
        public void OpenPanelDefaultPosition()
        {
            Initialize();

            if (panelRect != null)
                panelRect.anchoredPosition = defaultUIPanelPosition;

            gameObject.SetActive(true);
        }

        /// <summary>
        /// 이동한 위치에서 패널 활성화
        /// </summary>
        public void OpenPanelSetPosition()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 패널 닫기
        /// </summary>
        public void ClosePanel()
        {
            gameObject.SetActive(false);
        }


        public void TogglePanelDefaultPosition()
        {
            if (gameObject.activeSelf)
                ClosePanel();
            else
                OpenPanelDefaultPosition();
        }


        public void TogglePanelSetPosition()
        {
            if (gameObject.activeSelf)
                ClosePanel();
            else
                OpenPanelSetPosition();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (panelRect == null)
                return;

            BringToFront();

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                panelRect.parent as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPointerPosition);

            dragOffset = panelRect.anchoredPosition - localPointerPosition;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (panelRect == null)
                return;

            RectTransform parentRect = panelRect.parent as RectTransform;

            if (parentRect == null)
                return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPointerPosition))
            {
                panelRect.anchoredPosition = localPointerPosition + dragOffset;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            BringToFront();
        }

        private void BringToFront()
        {
            if (panelRect == null)
                return;

            panelRect.SetAsLastSibling();
        }
    }
}
