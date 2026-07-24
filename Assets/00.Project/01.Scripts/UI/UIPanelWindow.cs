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
public enum GameMenuType
{
    None,
    AnimalInv,
    ToolInv,
    Gacha,
    AnimalDex,
    Villiage,
    Option,
}

namespace UI
{

    // 현재 단계: 패널 드래그 비활성 — IBeginDragHandler / IDragHandler 는 주석 처리
    public class UIPanelWindow : MonoBehaviour, IBeginDragHandler, IDragHandler, IPointerDownHandler
    {
        [Header("Canvas Layer")]
        [SerializeField] private Canvas panelCanvas;

        [Header("Panel")]
        [SerializeField] private RectTransform panelRect;
        [SerializeField] private GameMenuType gameMenuType;

        public GameMenuType MenuType => gameMenuType;

        private Canvas rootCanvas;
        private Vector2 defaultUIPanelPosition; // UIController_AnimalInvPage 패널의 초기 위치
        private Vector2 dragOffset; // 드래그 비활성 (복구 시 주석 해제)

        private bool isInitialized;
        [SerializeField] private bool isDragging = false;

        private void Awake()
        {
            Initialize();

        }

        private void Start()
        {
            if (panelCanvas == null)
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
                Debug.LogWarning($"[UIPanelWindow] {gameObject.name}�� RectTransform�� �����ϴ�.");

                return;
            }

            defaultUIPanelPosition = panelRect.anchoredPosition;
            isInitialized = true;
        }


        public void OpenPanelDefaultPosition()
        {
            Initialize();

            if (panelRect != null)
                panelRect.anchoredPosition = defaultUIPanelPosition;

            gameObject.SetActive(true);

            BringToFront();
            PlayOpenScaleTween();
        }

        public void OpenPanelSetPosition()
        {
            gameObject.SetActive(true);
            ResetPosition();
            BringToFront();
            PlayOpenScaleTween();
        }

        public void ClosePanel()
        {
            StopOpenScaleTween();
            ResetPosition();
            gameObject.SetActive(false);
        }

        private void PlayOpenScaleTween()
        {
            if (UITweenManager.Instance != null)
                UITweenManager.Instance.PlayOpenScale(panelRect);
        }

        private void StopOpenScaleTween()
        {
            if (UITweenManager.Instance != null)
                UITweenManager.Instance.StopAndReset(panelRect);
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

        // --- 패널 드래그 (현재 단계 비활성 / 복구 시 주석 해제 + 클래스 인터페이스도 복구) ---
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (panelRect == null)
                return;

            if (isDragging == true)
            {
                BringToFront();

                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    panelRect.parent as RectTransform,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPointerPosition);

                dragOffset = panelRect.anchoredPosition - localPointerPosition;
            }

        }

        public void OnDrag(PointerEventData eventData)
        {
            if (panelRect == null)
                return;

            if (isDragging == true)

            {
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

        }

        /// <summary>
        /// 패널을 클릭했을 때 해당 Canvas를 맨 앞으로 이동합니다.
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            BringToFront();
        }

        private void BringToFront()
        {
            if (UIWindowLayerManager.Instance == null)
            {
                Debug.LogWarning("[UIPanelWindow] UIWindowLayerManager.Instance�� �����ϴ�.");

                return;
            }

            UIWindowLayerManager.Instance.BringToFront(panelCanvas);
        }
    }
}
