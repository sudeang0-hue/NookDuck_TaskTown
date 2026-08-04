using Animal.Data;
using System.Collections.Generic;
using TaskTown.Gacha;
using TaskTown.KDH;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace UI
{
    /* 동물 인벤토리 슬롯의 상세 페이지 (씬에 1개만 존재)
     * - 슬롯 클릭 시 SlotData_Animal 기반으로 UI를 갱신하고 패널을 오픈
     * - 오픈할 때마다 InventoryManager_Animal에서 최신 런타임 데이터를 재조회
     * - 패널이 열린 동안 OnAnimalSlotChanged로 동일 동물 슬롯 변경을 반영
     */
    public class UIController_AnimalInvPage : MonoBehaviour
    {
        [Header("동물 인벤토리 슬롯의 상세 페이지")]
        [FormerlySerializedAs("animalInventoryPanel")]
        [SerializeField] private GameObject animalInvPagePanel;

        [Header("동물 인벤토리 (비워두면 Instance 사용)")]
        [SerializeField] private InventoryManager_Animal animalInventory;

        [Header("해당 동물의 데이터")]
        [SerializeField] private Image animalIconImage;      // 동물 이미지
        [SerializeField] private Image animalLevelImage;     // 동물 레벨 이미지
        [SerializeField] private Image animalGradeImage;     // 동물 등급 이미지
        [Header(" ")]
        [SerializeField] private TMP_Text animalNameText;    // 동물 이름
        [SerializeField] private TMP_Text currentCountText;  // 보유 수량
        [SerializeField] private TMP_Text requireCountText;  // 레벨업 요구 수량
        [Header(" ")]
        [SerializeField] private TMP_Text currentProductCoin;  // 현재 기본 생산량
        [Header(" ")]
        [SerializeField] private Image usingToolIcon;  // 장착하고있는 도구 아이콘
        [Tooltip("장착 도구 이름. 현재 한글 폰트 깨짐으로 ToolDataSO.Id를 표시합니다.")]
        [SerializeField] private TMP_Text equipToolText;
        [SerializeField] private TMP_Text equipToolValeText;

        [Header("레벨 이미지")]
        [Tooltip("1성 ~ 5성 이미지. SlotUI_AnimalInv 와 동일한 배열 사용")]
        [SerializeField] private Sprite[] levelSprites;

        [Header("등급 이미지")]
        [Tooltip("Normal(0) ~ Legendary(4). ItemGrade enum 순서와 동일하게 배열")]
        [FormerlySerializedAs("gradelSprites")]
        [SerializeField] private Sprite[] gradeSprites;

        [Header("해당 동물의 설정 상호작용 버튼")]
        [SerializeField] private Button levelupButton;         // 레벨업 버튼
        [SerializeField] private Button settingToolButton;     // Set Tool - 도구 최초 장착
        [SerializeField] private Button changeToolButton;      // changeTool_btn - 장착 도구 변경
        [SerializeField] private Button setOffToolButton;      // setOffTool_btn - 장착 해제
        [SerializeField] private Button settingVilliageButton; // 마을 배치 버튼 (추후 구현)

        [Header("도구 장착 목록 패널")]
        [SerializeField]
        private UIController_ToolSetList toolSetListPanel;

        // 현재 페이지에 표시 중인 동물 ID. 갱신 시 이 ID로 최신 데이터를 재조회한다.
        private string currentAnimalId;

        public string CurrentAnimalId => currentAnimalId;

        // -----------------------------------------------------------------------------
        // [ 2026.08.03 - Choi - 튜토리얼 단계별 강조 연동 ]
        // 기능: Additive 튜토리얼 Scene에서 Set Tool 버튼을 강조 대상으로 사용할 수
        //       있도록 읽기 전용 참조만 공개합니다.
        // -----------------------------------------------------------------------------
        public Button SettingToolButton => settingToolButton;

        /// <summary>
        /// 상세 페이지 패널이 현재 열려 있는지 여부
        /// </summary>
        public bool IsOpen => animalInvPagePanel != null && animalInvPagePanel.activeSelf;

        private void Awake()
        {
            ResolveInventoryReference();

            if (levelupButton != null)
            {
                levelupButton.onClick.AddListener(OnClickLevelUp);
                levelupButton.gameObject.SetActive(false);
            }

            if (settingToolButton != null)
                settingToolButton.onClick.AddListener(OnClickSettingTool);

            if (changeToolButton != null)
            {
                changeToolButton.onClick.AddListener(OnClickChangeTool);
                changeToolButton.gameObject.SetActive(false);
            }

            if (setOffToolButton != null)
            {
                setOffToolButton.onClick.AddListener(OnClickSetOffTool);
                setOffToolButton.gameObject.SetActive(false);
            }

            if (animalInvPagePanel != null)
            {
                animalInvPagePanel.SetActive(false);
            }
        }

        private void OnEnable()
        {
            ResolveInventoryReference();

            if (animalInventory != null)
                animalInventory.OnAnimalSlotChanged += HandleAnimalSlotChanged;

            if (InventoryManager_Tool.Instance != null)
                InventoryManager_Tool.Instance.OnToolSlotChanged += HandleToolSlotChanged;
        }

        private void OnDisable()
        {
            if (animalInventory != null)
                animalInventory.OnAnimalSlotChanged -= HandleAnimalSlotChanged;

            if (InventoryManager_Tool.Instance != null)
                InventoryManager_Tool.Instance.OnToolSlotChanged -= HandleToolSlotChanged;
        }

        private void OnDestroy()
        {
            if (levelupButton != null)
                levelupButton.onClick.RemoveListener(OnClickLevelUp);

            if (settingToolButton != null)
                settingToolButton.onClick.RemoveListener(OnClickSettingTool);

            if (changeToolButton != null)
                changeToolButton.onClick.RemoveListener(OnClickChangeTool);

            if (setOffToolButton != null)
                setOffToolButton.onClick.RemoveListener(OnClickSetOffTool);
        }

        private void ResolveInventoryReference()
        {
            if (animalInventory == null)
                animalInventory = InventoryManager_Animal.Instance;
        }

        /// <summary>
        /// 인벤토리 슬롯 데이터가 변경되었을 때,
        /// 현재 열려 있는 상세 페이지와 같은 동물이면 UI를 재갱신합니다.
        /// </summary>
        private void HandleAnimalSlotChanged(SlotData_Animal slotData)
        {
            if (slotData == null)
                return;

            if (animalInvPagePanel == null || !animalInvPagePanel.activeSelf)
                return;

            if (string.IsNullOrEmpty(currentAnimalId))
                return;

            if (slotData.AnimalId != currentAnimalId)
                return;

            RefreshAnimalInvPage();
        }

        /// <summary>
        /// 도구 슬롯(장착/해제)이 변경되면, 열려 있는 상세 페이지의 도구 UI를 갱신합니다.
        /// </summary>
        private void HandleToolSlotChanged(SlotData_Tool slotData)
        {
            if (slotData == null)
                return;

            if (animalInvPagePanel == null || !animalInvPagePanel.activeSelf)
                return;

            if (string.IsNullOrEmpty(currentAnimalId))
                return;

            // 현재 동물이 관련되었거나, 방금 장착/해제된 도구일 수 있으므로 페이지를 재조회
            RefreshAnimalInvPage();
        }

        /// <summary>
        /// 인벤토리 슬롯 클릭 시 선택한 동물의 런타임 데이터로
        /// 상세 페이지를 갱신하고 패널을 엽니다.
        /// </summary>
        public void OpenAnimalInvPage(SlotData_Animal slotData)
        {
            if (slotData == null)
            {
                Debug.LogWarning("[UIController_AnimalInvPage] 표시할 슬롯 데이터가 없습니다.");
                return;
            }

            OpenAnimalInvPage(slotData.AnimalId);
        }

        /// <summary>
        /// 동물 ID로 상세 페이지를 엽니다.
        /// 항상 InventoryManager에서 최신 슬롯 데이터를 재조회합니다.
        /// </summary>
        public void OpenAnimalInvPage(string animalId)
        {
            if (string.IsNullOrEmpty(animalId))
            {
                Debug.LogWarning("[UIController_AnimalInvPage] 표시할 동물 ID가 비어 있습니다.");
                return;
            }

            if (animalInvPagePanel == null)
            {
                Debug.LogWarning("[UIController_AnimalInvPage] animalInvPagePanel이 연결되지 않았습니다.");
                return;
            }

            currentAnimalId = animalId;

            RefreshAnimalInvPage();

            animalInvPagePanel.SetActive(true);
        }

        /// <summary>
        /// 현재 동물 ID 기준으로 최신 슬롯 데이터를 재조회하여 페이지를 갱신합니다.
        /// SlotData_Animal 의 변경 사항(수량/레벨)이 반영됩니다.
        /// </summary>
        public void RefreshAnimalInvPage()
        {
            if (string.IsNullOrEmpty(currentAnimalId))
                return;

            ResolveInventoryReference();

            if (animalInventory == null)
            {
                Debug.LogWarning("[UIController_AnimalInvPage] InventoryManager_Animal이 연결되지 않았습니다.");
                return;
            }

            if (!animalInventory.TryGetAnimalSlot(currentAnimalId, out SlotData_Animal slotData))
            {
                Debug.LogWarning($"[UIController_AnimalInvPage] 보유하지 않은 동물입니다: {currentAnimalId}");
                return;
            }

            RefreshView(slotData);
        }

        /// <summary>
        /// 슬롯 데이터로 페이지의 각 UI 요소를 갱신합니다.
        /// </summary>
        private void RefreshView(SlotData_Animal slotData)
        {
            AnimalDataSO data = slotData.AnimalData;

            if (data == null)
            {
                Debug.LogWarning($"[UIController_AnimalInvPage] AnimalDataSO가 비어 있습니다: {currentAnimalId}");
                return;
            }

            ApplyStaticInfo(data);
            ApplyRuntimeInfo(slotData, data);
            UpdateLevelUpButton(slotData);
        }

        /// <summary>
        /// AnimalDataSO 기반 정적 정보 표시 (아이콘, 이름, 등급)
        /// </summary>
        private void ApplyStaticInfo(AnimalDataSO data)
        {
            if (animalIconImage != null)
            {
                animalIconImage.sprite = data.Icon;
                animalIconImage.enabled = data.Icon != null;
            }

            if (animalNameText != null)
                animalNameText.text = data.DisplayName;

            ApplyGradeImage(data.Grade);
        }

        /// <summary>
        /// ItemGrade에 맞는 등급 스프라이트를 animalGradeImage에 표시합니다.
        /// gradeSprites 배열 순서: Normal(0), Rare(1), Epic(2), Unique(3), Legendary(4)
        /// </summary>
        private void ApplyGradeImage(ItemGrade grade)
        {
            if (animalGradeImage == null)
                return;

            if (gradeSprites == null || gradeSprites.Length == 0)
            {
                animalGradeImage.enabled = false;
                return;
            }

            int gradeIndex = Mathf.Clamp((int)grade, 0, gradeSprites.Length - 1);
            Sprite gradeSprite = gradeSprites[gradeIndex];

            animalGradeImage.sprite = gradeSprite;
            animalGradeImage.enabled = gradeSprite != null;
        }

        /// <summary>
        /// SlotData_Animal 기반 런타임 정보 표시 (수량, 레벨, 생산량)
        /// </summary>
        private void ApplyRuntimeInfo(SlotData_Animal slotData, AnimalDataSO data)
        {
            // 본체 1마리를 제외한 재료 수량 표시 (SlotUI_AnimalInv 와 동일 규칙)
            if (currentCountText != null)
                currentCountText.text = "현재 수량: " + Mathf.Max(0, slotData.CurrentCount - 1).ToString();

            if (requireCountText != null)
                requireCountText.text = "레벨업 필요 수량: " + $" {slotData.RequiredUpgradeCount}";

            ApplyLevelImage(slotData.Level);

            // 현재 레벨 기준 초당 생산량 (기본 생산량 x 레벨 배율)
            if (currentProductCoin != null)
            {
                float coinPerSecond = data.BaseCoinPerSecond * data.CalculateLevelMultiplier(slotData.Level);
                currentProductCoin.text = "초당 생산량: " + $"{coinPerSecond:0.#}/s";
            }


            ApplyUsingToolIcon(slotData.AnimalId);
        }

        /// <summary>
        /// 현재 동물이 장착되어 있는 도구가 있으면 아이콘·이름·초당 생산량을 표시하고, 없으면 숨깁니다.
        /// 장착 여부에 따라 Set Tool / Change / SetOff 버튼 상태도 갱신합니다.
        /// </summary>
        private void ApplyUsingToolIcon(string animalId)
        {
            SlotData_Tool equippedTool = FindToolEquippedWithAnimal(animalId);
            bool hasEquippedTool = equippedTool != null;

            if (usingToolIcon != null)
            {
                Sprite toolIcon = hasEquippedTool && equippedTool.ToolData != null
                    ? equippedTool.ToolData.Icon
                    : null;

                usingToolIcon.sprite = toolIcon;
                usingToolIcon.enabled = toolIcon != null;
            }

            // 현재 단계: 한글 폰트 깨짐 때문에 DisplayName 대신 ToolId 표시
            if (equipToolText != null)
            {
                if (hasEquippedTool && equippedTool.ToolData != null)
                    equipToolText.text = equippedTool.ToolData.DisplayName;
                else if (hasEquippedTool)
                    equipToolText.text = equippedTool.ToolId;
                else
                    equipToolText.text = "배치된 도구가 없어요.";//string.Empty;
            }

            // 장착 도구 초당 생산량 표시. 미장착이면 숨김
            if (equipToolValeText != null)
            {
                if (hasEquippedTool && equippedTool.ToolData != null)
                {
                    float toolCoinPerSecond = equippedTool.ToolData.BaseCoinPerSecond
                        * equippedTool.ToolData.CalculateLevelMultiplier(equippedTool.Level);
                    equipToolValeText.text = $"+ {toolCoinPerSecond:0.#}/s";
                    equipToolValeText.gameObject.SetActive(true);
                }
                else
                {
                    equipToolValeText.gameObject.SetActive(false);
                }
            }

            UpdateToolEquipButtons(hasEquippedTool);
        }

        /// <summary>
        /// 도구 장착 여부에 따라 버튼 표시 상태를 갱신합니다.
        /// - 미장착: Set Tool 표시, Change/SetOff 숨김
        /// - 장착: Set Tool 숨김, Change/SetOff 표시
        /// </summary>
        private void UpdateToolEquipButtons(bool hasEquippedTool)
        {
            if (settingToolButton != null)
            {
                settingToolButton.gameObject.SetActive(!hasEquippedTool);
                settingToolButton.interactable = !hasEquippedTool;
            }

            if (changeToolButton != null)
                changeToolButton.gameObject.SetActive(hasEquippedTool);

            if (setOffToolButton != null)
                setOffToolButton.gameObject.SetActive(hasEquippedTool);
        }

        /// <summary>
        /// 도구 슬롯 목록에서 이 동물이 장착된 슬롯을 찾습니다(없으면 null).
        /// </summary>
        private static SlotData_Tool FindToolEquippedWithAnimal(string animalId)
        {
            if (string.IsNullOrEmpty(animalId) || InventoryManager_Tool.Instance == null)
                return null;

            IReadOnlyList<SlotData_Tool> toolSlots = InventoryManager_Tool.Instance.ToolSlotsList;

            if (toolSlots == null)
                return null;

            for (int i = 0; i < toolSlots.Count; i++)
            {
                SlotData_Tool toolSlot = toolSlots[i];

                if (toolSlot != null && toolSlot.CurrentAnimalSet && toolSlot.CurrentAnimalId == animalId)
                    return toolSlot;
            }


            return null;
        }

        /// <summary>
        /// 현재 레벨에 맞는 별 이미지를 animalLevelImage에 표시합니다.
        /// </summary>
        private void ApplyLevelImage(int level)
        {
            if (animalLevelImage == null)
                return;

            if (levelSprites == null || levelSprites.Length == 0)
            {
                animalLevelImage.enabled = false;
                return;
            }

            int levelIndex = Mathf.Clamp(level - 1, 0, levelSprites.Length - 1);
            Sprite levelSprite = levelSprites[levelIndex];

            animalLevelImage.sprite = levelSprite;
            animalLevelImage.enabled = levelSprite != null;
        }

        /// <summary>
        /// 레벨업 가능 여부에 따라 버튼을 표시합니다.
        /// </summary>
        private void UpdateLevelUpButton(SlotData_Animal slotData)
        {
            if (levelupButton == null)
                return;

            bool canLevelUp = animalInventory != null &&
                animalInventory.CanLevelUpAnimal(slotData.AnimalId);

            levelupButton.gameObject.SetActive(canLevelUp);
        }

        /// <summary>
        /// 페이지 내 레벨업 버튼. 성공 여부와 관계없이 최신 데이터로 재갱신합니다.
        /// </summary>
        private void OnClickLevelUp()
        {
            if (string.IsNullOrEmpty(currentAnimalId))
                return;

            ResolveInventoryReference();

            if (animalInventory == null)
            {
                Debug.LogWarning("[UIController_AnimalInvPage] InventoryManager_Animal이 연결되지 않았습니다.");
                return;
            }

            animalInventory.TryLevelUpAnimal(currentAnimalId);

            RefreshAnimalInvPage();
        }

        /// <summary>
        /// "Set Tool" 버튼. 장착 가능 도구 목록만 엽니다.
        /// 실제 장착은 tool_set_slot의 cover_btn → UIController_ToolSetList에서 처리합니다.
        /// </summary>
        private void OnClickSettingTool()
        {
            OpenToolSetListPanel();
        }

        /// <summary>
        /// "changeTool_btn" 버튼. 장착 가능 도구 목록만 다시 엽니다.
        /// 실제 장착/변경은 cover_btn 선택 시 처리됩니다.
        /// </summary>
        private void OnClickChangeTool()
        {
            OpenToolSetListPanel();
        }

        /// <summary>
        /// "setOffTool_btn" 버튼. 현재 동물의 도구 장착을 해제합니다.
        /// </summary>
        private void OnClickSetOffTool()
        {
            if (string.IsNullOrEmpty(currentAnimalId))
                return;

            SlotData_Tool equippedTool = FindToolEquippedWithAnimal(currentAnimalId);

            if (equippedTool == null)
            {
                UpdateToolEquipButtons(false);
                return;
            }

            InventoryManager_Tool toolInventory = InventoryManager_Tool.Instance;

            if (toolInventory == null)
            {
                Debug.LogWarning("[UIController_AnimalInvPage] InventoryManager_Tool이 연결되지 않았습니다.");
                return;
            }

            toolInventory.TryRemoveAnimalFromTool(equippedTool.ToolId);
            RefreshAnimalInvPage();
        }

        /// <summary>
        /// 현재 동물을 대상으로 도구 장착 목록 패널을 엽니다.
        /// </summary>
        private void OpenToolSetListPanel()
        {
            if (string.IsNullOrEmpty(currentAnimalId))
                return;

            if (toolSetListPanel == null)
            {
                Debug.LogWarning("[UIController_AnimalInvPage] toolSetListPanel이 연결되지 않았습니다.");
                return;
            }

            toolSetListPanel.Open(currentAnimalId, RefreshAnimalInvPage);
        }

        /// <summary>
        /// 상세 페이지 닫기 버튼용
        /// </summary>
        public void CloseAnimalInvPage()
        {
            currentAnimalId = string.Empty;

            if (toolSetListPanel != null && toolSetListPanel.IsOpen)
                toolSetListPanel.Close();

            if (animalInvPagePanel == null)
                return;

            animalInvPagePanel.SetActive(false);
        }
    }
}
