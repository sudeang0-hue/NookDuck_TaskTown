/* 개별 도구 슬롯의 화면 표시를 담당함
 * 현재 수량 표시
 * 필요 수량 표시
 * 레벨업 가능시 버튼 활성화
 * 불가능하면 버튼 비활성화
 */

using System;
using TaskTown.KDH;
using TMPro;
using Tool.Data;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace KAY
{

    public class SlotUI_ToolInv : SlotUIBase
    {
        [Header("References")]
        [SerializeField] private InventoryManager_Tool toolInventory;

        [Header("해당 도구 데이터")]
        [SerializeField] private Image toolIconImage;
        [SerializeField] private TMP_Text toolNameText;
        [SerializeField] private TMP_Text currentCountText;
        [SerializeField] private TMP_Text requireCountText;

        [Header("도구 인벤토리 슬롯 UIController_ToolInvPage")]
        [Tooltip("현재 레벨의 별 모양 이미지")]
        [SerializeField] private Image levelImage;

        [Header("레벨업 버튼")]
        [SerializeField] private Button levelupButton;

        [Tooltip("현재 이 도구에 배치된 동물 아이콘")]
        [SerializeField] private Image curentAnimalicon;
        [Tooltip("이 도구의 특화 동물 아이콘")]
        [SerializeField] private Image specialAnimalicon;
        //[Tooltip("이 도구의 시간당 생산량 텍스트")]
        //[SerializeField] private TMP_Text outoCoinPerHourText;

        [Header("레벨업 이미지")]
        [Tooltip("1성 ~ 5성 이미지. 단일 컬러")]
        [SerializeField] private Sprite[] levelSprites;

        private SlotData_Tool currentSlotData;

        [Header("클릭 범위 버튼")]
        [SerializeField] private Button coverButton;


        private UIController_ToolInvPage toolInvPageController;

        // 필요하면 외부에서 콜백으로도 받을 수 있게
        private Action<SlotData_Tool> onSelected;

        private void Awake()
        {
            ResolveInventoryReference();

            if (levelupButton != null)
            {
                levelupButton.onClick.AddListener(OnClickLevelUp);
                levelupButton.gameObject.SetActive(false);
            }

            //--------------------------26.07.23 KDH 수정--------------------------------
            if (coverButton == null)
                coverButton = GetComponentInChildren<Button>(true);

            if (coverButton != null)
                coverButton.onClick.AddListener(HandleSlotClicked);
            //---------------------------------------------------------

            if (toolInventory == null)
            {
                toolInventory = InventoryManager_Tool.Instance;
            }
        }

        public void Initialize(
            SlotData_Tool slotData,
            InventoryManager_Tool inventory = null,
            UIController_ToolInvPage pageController = null)
        {
            if (inventory != null)
                toolInventory = inventory;

            if (pageController != null)
                toolInvPageController = pageController;

            ResolveInventoryReference();

            if (slotData == null)
            {
                Debug.LogWarning("[SlotUI_ToolInv] 초기화할 슬롯 데이터가 없습니다.", this);
                return;
            }
            if (coverButton == null)
                Debug.LogWarning("[SlotUI_ToolInv] coverButton이 연결되지 않았습니다.", this);

            currentSlotData = slotData;
            RefreshView();
        }

        //------------------------26.07.23 KDH 수정----------------------------------------
        private void HandleSlotClicked()
        {
            if (currentSlotData == null)
            {
                Debug.LogWarning("[SlotUI_ToolInv] 현재 슬롯에 도구 데이터가 없습니다.", this);
                return;
            }

            if (toolInvPageController == null)
            {
                Debug.LogWarning("[SlotUI_ToolInv] UIController_ToolInvPage가 연결되지 않았습니다.", this);
                return;
            }

            if (toolInvPageController.IsOpen &&
                toolInvPageController.CurrentToolId == currentSlotData.ToolId)
            {
                toolInvPageController.CloseToolInvPage();
                return;
            }

            toolInvPageController.OpenToolInvPage(currentSlotData);

            //// 1) 배치 컨트롤러에 ToolId 전달
            //UIController_ToolPlacement placementUi = FindFirstObjectByType<UIController_ToolPlacement>();

            //if (placementUi != null) placementUi.SetSelectedTool(currentSlotData.ToolId);

            //// 2) (선택) 다른 쪽에서도 듣고 싶으면 콜백
            //onSelected?.Invoke(currentSlotData);

            //Debug.Log($"[SlotUI_ToolInv] 선택됨: {currentSlotData.ToolId}");

        }
        //-----------------------------------------------------------------------------------

        public void Refresh(SlotData_Tool slotData)
        {
            if (slotData == null)
            {
                Debug.LogWarning("[SlotUI_ToolInv] 갱신할 슬롯 데이터가 없습니다.", this);
                return;
            }

            currentSlotData = slotData;
            RefreshView();
        }

        private void ResolveInventoryReference()
        {
            if (toolInventory == null)
                toolInventory = InventoryManager_Tool.Instance;
        }

        private void RefreshView()
        {
            if (currentSlotData == null)
                return;

            ToolDataSO data = currentSlotData.ToolData;

            if (data == null)
                return;

            SetBaseInfo(data.Id, data.DisplayName, data.Icon);
            ApplyIconAndName(data);

            // 본체 1개를 제외한 재료 수량을 UI에 표시 (예: 내부 1 → 0/4)
            if (currentCountText != null)
                currentCountText.text = Mathf.Max(0, currentSlotData.CurrentCount - 1).ToString();

            if (requireCountText != null)
                requireCountText.text = $"/ {currentSlotData.RequiredUpgradeCount}";

            if (levelImage != null && levelSprites != null && levelSprites.Length > 0)
            {
                int levelIndex = Mathf.Clamp(currentSlotData.Level - 1, 0, levelSprites.Length - 1);
                levelImage.sprite = levelSprites[levelIndex];
                levelImage.enabled = levelSprites[levelIndex] != null;
            }

            UpdateLevelUpButton();
        }

        /// <summary>
        /// 폰트 깨짐 현상으로 인해 표시되는 이름을 ID로 설정함.
        /// 추후 폰트 작업이 완료되면 DisplayName 으로 변경
        /// </summary>
        private void ApplyIconAndName(ToolDataSO data)
        {
            if (toolIconImage != null)
            {
                toolIconImage.sprite = data.Icon;
                toolIconImage.enabled = data.Icon != null;
            }

            if (toolNameText != null)
                //toolNameText.text = data.DisplayName;
                toolNameText.text = data.Id;
        }

        private void UpdateLevelUpButton()
        {
            if (levelupButton == null)
                return;

            bool canLevelUp = toolInventory != null &&
                toolInventory.CanLevelUpTool(currentSlotData.ToolId);

            levelupButton.gameObject.SetActive(canLevelUp);
        }

        private void OnClickLevelUp()
        {
            if (currentSlotData == null)
            {
                Debug.LogWarning("[SlotUI_ToolInv] 레벨업할 슬롯 데이터가 없습니다.", this);
                return;
            }

            ResolveInventoryReference();

            if (toolInventory == null)
            {
                Debug.LogWarning("[SlotUI_ToolInv] InventoryManager_Tool이 연결되지 않았습니다.", this);
                return;
            }

            toolInventory.TryLevelUpTool(currentSlotData.ToolId);
        }

        private void OnDestroy()
        {
            if (levelupButton != null)
                levelupButton.onClick.RemoveListener(OnClickLevelUp);

            //-----------------------26.07.23 KDH 수정----------------------------------
            if (coverButton != null)
                coverButton.onClick.RemoveListener(HandleSlotClicked);
            //----------------------------------------------------------
        }

        public override void Clear()
        {
            base.Clear();

            currentSlotData = null;

            if (toolIconImage != null)
            {
                toolIconImage.sprite = null;
                toolIconImage.enabled = false;
            }

            if (toolNameText != null)
                toolNameText.text = string.Empty;

            if (currentCountText != null)
                currentCountText.text = string.Empty;

            if (requireCountText != null)
                requireCountText.text = string.Empty;

            if (levelImage != null)
            {
                levelImage.sprite = null;
                levelImage.enabled = false;
            }

            if (levelupButton != null)
                levelupButton.gameObject.SetActive(false);
        }
    }
}