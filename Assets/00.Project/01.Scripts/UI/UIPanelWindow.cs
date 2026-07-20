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

    public class UIPanelWindow : MonoBehaviour, IBeginDragHandler, IDragHandler, IPointerDownHandler
    {
        [Header("Canvas Layer")]
        [SerializeField] private Canvas panelCanvas;

        [Header("Panel")]
        [SerializeField] private RectTransform panelRect;
        [SerializeField] private GameMenuType gameMenuType;

        private Canvas rootCanvas;
        private Vector2 defaultUIPanelPosition; // UIController_AnimalInvPage �г��� �ʱ� ��ġ
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
                Debug.LogWarning($"[UIPanelWindow] {gameObject.name}�� RectTransform�� �����ϴ�.");

                return;
            }

            defaultUIPanelPosition = panelRect.anchoredPosition;
            isInitialized = true;
        }


        /// <summary>
        /// �ʱ� ��ġ���� �г� Ȱ��ȭ
        /// </summary>
        public void OpenPanelDefaultPosition()
        {
            Initialize();

            if (panelRect != null)
                panelRect.anchoredPosition = defaultUIPanelPosition;

            gameObject.SetActive(true);
            
            BringToFront();
            PlayOpenScaleTween();
        }

        /// <summary>
        /// �̵��� ��ġ���� �г� Ȱ��ȭ
        /// </summary>
        public void OpenPanelSetPosition()
        {
            gameObject.SetActive(true);
            ResetPosition();
            BringToFront();
            PlayOpenScaleTween();
        }

        /// <summary>
        /// �г� �ݱ�
        /// </summary>
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
        /// �г��� Ŭ������ �� �ش� Canvas�� ���� ������ �̵��մϴ�.
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
