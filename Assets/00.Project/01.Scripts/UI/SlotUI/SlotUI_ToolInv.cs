/* 개별 도구 슬롯의 화면 표시를 담당함
 * 현재 수량 표시
 * 필요 수량 표시
 * 레벨업 가능시 버튼 활성화
 * 불가능하면 버튼 비활성화
 * [2026.07.31 나범 수정] 등급별 5종 고유 카드 배경 스프라이트 통스왑 로직 적용
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

        // ------------------------------------------------------------------------------------------
        // [2026.07.31 수정] 5가지 개별 카드 배경 스프라이트 에셋 바인딩
        // ------------------------------------------------------------------------------------------
        [Header("카드 배경 (등급별 5종 개별 이미지)")]
        [Tooltip("슬롯 카드의 바탕이 되는 UI Image 컴포넌트")]
        [SerializeField] private Image cardBackgroundImage;

        [Tooltip("Size를 5로 설정하고 각 등급별 전용 배경 스프라이트를 드래그 앤 드롭하세요.\n[0]: 노말, [1]: 레어, [2]: 에픽, [3]: 유니크, [4]: 레전더리")]
        [SerializeField] private Sprite[] gradeBackgroundSprites = new Sprite[5];
        // ------------------------------------------------------------------------------------------

        [Header("도구 인벤토리 상세 UIController_ToolInvPage")]
        [Tooltip("현재 레벨의 별 모양 이미지")]
        [SerializeField] private Image levelImage;

        [Header("레벨업 버튼")]
        [SerializeField] private Button levelupButton;

        [Tooltip("현재 이 도구에 배치된 동물 아이콘")]
        [SerializeField] private Image setupAnimal;
        [Tooltip("이 도구의 특화 동물 아이콘")]
        [SerializeField] private Image findspecialAnimal;

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

            if (coverButton == null)
                coverButton = GetComponentInChildren<Button>(true);

            if (coverButton != null)
                coverButton.onClick.AddListener(HandleSlotClicked);

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
        }

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

            // ------------------------------------------------------------------------------------------
            // [2026.07.31 수정] 도구 등급에 따른 5종 고유 배경 스프라이트 적용
            // ------------------------------------------------------------------------------------------
            ApplyGradeBackground(data);
            // ------------------------------------------------------------------------------------------

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
            UpdateStatusIcons();
        }

        /// <summary>
        /// 동물 장착 / 특화 동물 해금 상태 아이콘만 갱신합니다.
        /// </summary>
        public void RefreshStatusIcons()
        {
            UpdateStatusIcons();
        }

        private void UpdateStatusIcons()
        {
            bool hasSetupAnimal = currentSlotData != null &&
                currentSlotData.CurrentAnimalSet &&
                !string.IsNullOrEmpty(currentSlotData.CurrentAnimalId);

            if (setupAnimal != null)
                setupAnimal.gameObject.SetActive(hasSetupAnimal);

            bool specialRevealed = currentSlotData != null &&
                currentSlotData.HasRevealedSpecialAnimal;

            if (findspecialAnimal != null)
                findspecialAnimal.gameObject.SetActive(specialRevealed);
        }

        private void ApplyIconAndName(ToolDataSO data)
        {
            if (toolIconImage != null)
            {
                toolIconImage.sprite = data.Icon;
                toolIconImage.enabled = data.Icon != null;
            }

            if (toolNameText != null)
                toolNameText.text = data.Id;
        }

        // ------------------------------------------------------------------------------------------
        // [2026.07.31 수정] 등급에 따라 5개의 고유 배경 스프라이트 중 하나를 1:1 교체
        // ------------------------------------------------------------------------------------------
        private void ApplyGradeBackground(ToolDataSO data)
        {
            if (cardBackgroundImage == null)
            {
                Debug.LogWarning("[SlotUI_ToolInv] cardBackgroundImage가 할당되지 않았습니다.", this);
                return;
            }

            if (gradeBackgroundSprites == null || gradeBackgroundSprites.Length == 0)
            {
                Debug.LogWarning("[SlotUI_ToolInv] gradeBackgroundSprites 배열이 비어있습니다.", this);
                return;
            }

            // 1. Grade Enum/int 값을 인덱스로 변환 (0: Normal, 1: Rare, 2: Epic, 3: Unique, 4: Legendary)
            int rawIndex = (int)data.Grade;

            // 2. 배열 범위를 안전하게 클램핑 (0 <= Index <= Length - 1)
            int safeIndex = Mathf.Clamp(rawIndex, 0, gradeBackgroundSprites.Length - 1);

            Sprite selectedSprite = gradeBackgroundSprites[safeIndex];

            if (selectedSprite != null)
            {
                // 3. 원본 아트 색상이 변색 없이 순수하게 나오도록 White로 보장
                cardBackgroundImage.color = Color.white;

                // 4. 고유 배경 스프라이트 텍스처 자산을 통째로 교체
                cardBackgroundImage.sprite = selectedSprite;
                cardBackgroundImage.enabled = true;
            }
            else
            {
                Debug.LogWarning($"[SlotUI_ToolInv] {safeIndex}번 등급에 해당하는 배경 스프라이트가 Inspector에 할당되지 않았습니다.", this);
            }
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

            if (coverButton != null)
                coverButton.onClick.RemoveListener(HandleSlotClicked);
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

            // ------------------------------------------------------------------------------------------
            // [2026.07.31 수정] 슬롯 초기화 시 카드 배경 스프라이트도 리셋
            // ------------------------------------------------------------------------------------------
            if (cardBackgroundImage != null)
            {
                cardBackgroundImage.sprite = null;
                cardBackgroundImage.enabled = false;
            }
            // ------------------------------------------------------------------------------------------

            if (levelupButton != null)
                levelupButton.gameObject.SetActive(false);

            if (setupAnimal != null)
                setupAnimal.gameObject.SetActive(false);

            if (findspecialAnimal != null)
                findspecialAnimal.gameObject.SetActive(false);
        }
    }
}