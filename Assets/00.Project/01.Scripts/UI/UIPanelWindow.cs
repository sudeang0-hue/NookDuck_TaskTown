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

namespace UI
{

    public class UIPanelWindow : MonoBehaviour, IBeginDragHandler, IDragHandler, IPointerDownHandler
    {
        [Header("Canvas Layer")]
        [SerializeField] private Canvas panelCanvas;

        [Header("Panel")]
        [SerializeField] private RectTransform panelRect;

        private Canvas rootCanvas;
        private Vector2 defaultUIPanelPosition; // UI 패널의 초기 위치
        private Vector2 dragOffset;

        private bool isInitialized;


        private void Awake()
        {
            Initialize();

        }

        private void Start()
        {
            if(panelCanvas == null)
            {
                panelCanvas = gameObject.GetComponentInParent<Canvas>();
            }
        }

        private void Initialize()
        {
            if (isInitialized)
                return;

            if (panelRect == null) panelRect = GetComponent<RectTransform>();

            if (panelCanvas == null) panelCanvas = GetComponentInParent<Canvas>();

            if (panelCanvas != null) rootCanvas = panelCanvas.rootCanvas;

            if (panelRect == null)
            {
                Debug.LogWarning($"[UIPanelWindow] {gameObject.name}에 RectTransform이 없습니다.");

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
            
            BringToFront();
        }

        /// <summary>
        /// 이동한 위치에서 패널 활성화
        /// </summary>
        public void OpenPanelSetPosition()
        {
            gameObject.SetActive(true);
            ResetPosition();
            BringToFront();
        }

        /// <summary>
        /// 패널 닫기
        /// </summary>
        public void ClosePanel()
        {
            ResetPosition();
            gameObject.SetActive(false);
        }

        private void ResetPosition()
        {
            if (!isInitialized) Initialize();

            if (panelRect != null) panelRect.anchoredPosition = defaultUIPanelPosition;
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

        /// <summary>
        /// 패널을 클릭했을 때 해당 Canvas를 가장 앞으로 이동합니다.
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            BringToFront();
        }

        private void BringToFront()
        {
            if (UIWindowLayerManager.Instance == null)
            {
                Debug.LogWarning("[UIPanelWindow] UIWindowLayerManager.Instance가 없습니다.");

                return;
            }

            UIWindowLayerManager.Instance.BringToFront(panelCanvas);
        }
    }
}
