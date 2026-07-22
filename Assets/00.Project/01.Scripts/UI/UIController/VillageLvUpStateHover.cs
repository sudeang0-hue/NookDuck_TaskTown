using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace UI
{
    /// <summary>
    /// Coin_Slider Hover 시 LvUp_VillageInfo_Root를 표시하고,
    /// 패널 Left Top을 마우스 위치에 맞춥니다.
    /// Slider 범위를 벗어나면 패널을 비활성화합니다.
    /// </summary>
    public class VillageLvUpStateHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("패널")]
        [Tooltip("LvUp_VillageInfo_Root (RectTransform)")]
        [SerializeField] private RectTransform lvUpPanelRoot;
        [SerializeField] private Canvas targetCanvas;
        [Tooltip("마우스 기준 추가 오프셋 (로컬 단위)")]
        [SerializeField] private Vector2 positionOffset;
        [Tooltip("true면 Pivot을 (0,1) Left Top으로 강제합니다.")]
        [SerializeField] private bool forceLeftTopPivot = true;
        [Tooltip("true면 Hover 패널이 Raycast를 막지 않아 Slider Exit가 안정적입니다.")]
        [SerializeField] private bool disablePanelRaycast = true;

        [Header("다음 레벨 텍스트 (선택)")]

        [SerializeField] private TMP_Text lvUpTownLevelText;
        [SerializeField] private TMP_Text lvUpClickCoinText;
        [SerializeField] private TMP_Text lvUpTypingCoinText;
        [SerializeField] private TMP_Text lvUpToolCapacityText;
        [SerializeField] private TMP_Text lvUpAutoProductBonusText;

        [Header("데이터 소스 (선택)")]
        [SerializeField] private VillageUI_Manager villageUIManager;

        private bool isHovering;
        private RectTransform parentRect;
        private CanvasGroup panelCanvasGroup;

        private void Awake()
        {
            if (villageUIManager == null)
                villageUIManager = GetComponentInParent<VillageUI_Manager>();

            CachePanelRefs();
            HidePanel();
        }

        private void OnDisable()
        {
            isHovering = false;
            HidePanel();
        }

        private void LateUpdate()
        {
            if (!isHovering || lvUpPanelRoot == null || !lvUpPanelRoot.gameObject.activeSelf)
                return;

            FollowMouse();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovering = true;

            if (villageUIManager != null)
                villageUIManager.ApplyLvUpHoverPreview(this);

            ShowPanel();
            FollowMouse();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovering = false;
            HidePanel();
        }

        /// <summary>
        /// 다음 레벨 미리보기 수치를 텍스트에 반영합니다.
        /// </summary>
        public void SetPreview(
            int townLevel,
            int clickCoin,
            int typingCoin,
            int toolCapacity,
            float autoProductBonus)
        {
            SetText(lvUpTownLevelText, "TownLevel : 1 > " + townLevel.ToString("N0"));
            SetText(lvUpClickCoinText, "Click : " + clickCoin.ToString("N0"));
            SetText(lvUpTypingCoinText, "Typing : " + typingCoin.ToString("N0"));
            SetText(lvUpToolCapacityText, "Tool : " + toolCapacity.ToString("N0"));
            SetText(lvUpAutoProductBonusText, "Bonus : " + autoProductBonus.ToString("N0") + " %");
        }

        private void CachePanelRefs()
        {
            if (lvUpPanelRoot == null)
                return;

            parentRect = lvUpPanelRoot.parent as RectTransform;

            if (targetCanvas == null)
                targetCanvas = lvUpPanelRoot.GetComponentInParent<Canvas>();

            if (forceLeftTopPivot)
                lvUpPanelRoot.pivot = new Vector2(0f, 1f);

            if (disablePanelRaycast)
            {
                if (!lvUpPanelRoot.TryGetComponent(out panelCanvasGroup))
                    panelCanvasGroup = lvUpPanelRoot.gameObject.AddComponent<CanvasGroup>();

                panelCanvasGroup.blocksRaycasts = false;
                panelCanvasGroup.interactable = false;
            }
        }

        private void ShowPanel()
        {
            if (lvUpPanelRoot == null)
                return;

            if (forceLeftTopPivot)
                lvUpPanelRoot.pivot = new Vector2(0f, 1f);

            if (disablePanelRaycast && panelCanvasGroup != null)
            {
                panelCanvasGroup.blocksRaycasts = false;
                panelCanvasGroup.interactable = false;
            }

            lvUpPanelRoot.gameObject.SetActive(true);
        }

        private void HidePanel()
        {
            if (lvUpPanelRoot == null)
                return;

            lvUpPanelRoot.gameObject.SetActive(false);
        }

        private void FollowMouse()
        {
            if (lvUpPanelRoot == null || parentRect == null)
                return;

            Vector2 screenPos = GetMouseScreenPosition();
            Camera eventCamera = GetEventCamera();

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect,
                    screenPos,
                    eventCamera,
                    out Vector2 localPoint))
                return;

            lvUpPanelRoot.anchoredPosition = localPoint + positionOffset;
        }

        private Vector2 GetMouseScreenPosition()
        {
            if (Mouse.current != null)
                return Mouse.current.position.ReadValue();

            return Input.mousePosition;
        }

        private Camera GetEventCamera()
        {
            if (targetCanvas == null)
                return null;

            if (targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;

            return targetCanvas.worldCamera != null
                ? targetCanvas.worldCamera
                : Camera.main;
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target == null)
                return;

            target.text = value;
        }
    }
}
