/* ???? ???? ?????? ??? ??��? ?????
 * ???? ???? ???
 * ??? ???? ???
 * ?????? ????? ??? ????
 * ???????? ??? ??????
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

        [Header("??? ???? ??????")]
        [SerializeField] private Image toolIconImage;
        [SerializeField] private TMP_Text toolNameText;
        [SerializeField] private TMP_Text currentCountText;
        [SerializeField] private TMP_Text requireCountText;

        [Header("???? ?��??? ???? UIController_ToolInvPage")]
        [Tooltip("???? ?????? ?? ??? ?????")]
        [SerializeField] private Image levelImage;

        [Header("?????? ???")]
        [SerializeField] private Button levelupButton;

        [Tooltip("?????? ?????? ?????? ??????")]
        [SerializeField] private Image setupAnimal;
        [Tooltip("?? ?????? ?????? ?????? ??????")]
        [SerializeField] private Image findspecialAnimal;
        //[Tooltip("?? ?????? ?��??? ???�� ????")]
        //[SerializeField] private TMP_Text outoCoinPerHourText;

        [Header("?????? ?????")]
        [Tooltip("1?? ~ 5?? ?????. ???? ?��?")]
        [SerializeField] private Sprite[] levelSprites;

        private SlotData_Tool currentSlotData;

        [Header("??? ???? ???")]
        [SerializeField] private Button coverButton;


        private UIController_ToolInvPage toolInvPageController;

        // ?????? ??��??? ??????��? ???? ?? ???
        private Action<SlotData_Tool> onSelected;

        private void Awake()
        {
            ResolveInventoryReference();

            if (levelupButton != null)
            {
                levelupButton.onClick.AddListener(OnClickLevelUp);
                levelupButton.gameObject.SetActive(false);
            }

            //--------------------------26.07.23 KDH ????--------------------------------
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
                Debug.LogWarning("[SlotUI_ToolInv] ?????? ???? ??????? ???????.", this);
                return;
            }
            if (coverButton == null)
                Debug.LogWarning("[SlotUI_ToolInv] coverButton?? ??????? ???????.", this);

            currentSlotData = slotData;
            RefreshView();
        }

        //------------------------26.07.23 KDH ????----------------------------------------
        private void HandleSlotClicked()
        {
            if (currentSlotData == null)
            {
                Debug.LogWarning("[SlotUI_ToolInv] ???? ????? ???? ??????? ???????.", this);
                return;
            }

            if (toolInvPageController == null)
            {
                Debug.LogWarning("[SlotUI_ToolInv] UIController_ToolInvPage?? ??????? ???????.", this);
                return;
            }

            if (toolInvPageController.IsOpen &&
                toolInvPageController.CurrentToolId == currentSlotData.ToolId)
            {
                toolInvPageController.CloseToolInvPage();
                return;
            }

            toolInvPageController.OpenToolInvPage(currentSlotData);

            //// 1) ??? ???????? ToolId ????
            //UIController_ToolPlacement placementUi = FindFirstObjectByType<UIController_ToolPlacement>();

            //if (placementUi != null) placementUi.SetSelectedTool(currentSlotData.ToolId);

            //// 2) (????) ??? ??????? ??? ?????? ???
            //onSelected?.Invoke(currentSlotData);

            //Debug.Log($"[SlotUI_ToolInv] ?????: {currentSlotData.ToolId}");

        }
        //-----------------------------------------------------------------------------------

        public void Refresh(SlotData_Tool slotData)
        {
            if (slotData == null)
            {
                Debug.LogWarning("[SlotUI_ToolInv] ?????? ???? ??????? ???????.", this);
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

            // ??? 1???? ?????? ??? ?????? UI?? ??? (??: ???? 1 ?? 0/4)
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
        /// ???? ???? / ?? ???? ??? ???? ??????? ????????.
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

        /// <summary>
        /// ??? ???? ???????? ???? ????? ????? ID?? ??????.
        /// ???? ??? ????? ????? DisplayName ???? ????
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
                Debug.LogWarning("[SlotUI_ToolInv] ???????? ???? ??????? ???????.", this);
                return;
            }

            ResolveInventoryReference();

            if (toolInventory == null)
            {
                Debug.LogWarning("[SlotUI_ToolInv] InventoryManager_Tool?? ??????? ???????.", this);
                return;
            }

            toolInventory.TryLevelUpTool(currentSlotData.ToolId);
        }

        private void OnDestroy()
        {
            if (levelupButton != null)
                levelupButton.onClick.RemoveListener(OnClickLevelUp);

            //-----------------------26.07.23 KDH ????----------------------------------
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

            if (setupAnimal != null)
                setupAnimal.gameObject.SetActive(false);

            if (findspecialAnimal != null)
                findspecialAnimal.gameObject.SetActive(false);
        }
    }
}