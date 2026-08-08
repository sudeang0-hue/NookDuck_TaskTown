using Animal.Data;
using TaskTown.Gacha;
using TaskTown.KDH;
using TMPro;
using Tool.Data;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// 도구 인벤토리 슬롯의 상세 페이지 (씬에 1개만 존재).
    /// - 슬롯 클릭 시 SlotData_Tool 기반으로 UI를 갱신하고 패널을 오픈
    /// - 오픈할 때마다 InventoryManager_Tool에서 최신 런타임 데이터를 재조회
    /// - 패널이 열린 동안 OnToolSlotChanged로 동일 도구 슬롯 변경을 반영
    /// - Set(장착) 버튼은 없음. 장착 동물 아이콘은 CurrentAnimalId 기준으로 표시(동물 Inv에서 장착 연동 후 데이터가 채워지면 갱신)
    /// </summary>
    public class UIController_ToolInvPage : MonoBehaviour
    {
        [Header("도구 인벤토리 슬롯의 상세 페이지")]
        [FormerlySerializedAs("ToolInventoryPanel")]
        [SerializeField] private GameObject toolInvPagePanel;

        [Header("도구 인벤토리 (비워두면 Instance 사용)")]
        [SerializeField] private InventoryManager_Tool toolInventory;

        [Header("해당 도구의 데이터")]
        [SerializeField] private Image toolIconImage;
        [SerializeField] private Image toolLevelImage;
        [SerializeField] private Image toolGradeImage;
        [Header(" ")]
        [FormerlySerializedAs("animalNameText")]
        [SerializeField] private TMP_Text toolNameText;
        [SerializeField] private TMP_Text currentCountText;
        [SerializeField] private TMP_Text requireCountText;
        [Header(" ")]
        [SerializeField] private TMP_Text currentProductCoin;
        [Header(" ")]
        [Tooltip("이 도구에 장착된 동물 아이콘. SlotData_Tool.CurrentAnimalId 기준으로 표시됩니다.")]
        [SerializeField] private Image usingAnimalIcon;
        [SerializeField] private TMP_Text equipAnimalText;
        [SerializeField] private Button levelupButton;
        [Tooltip("이 도구의 특화 동물 이름")]
        [SerializeField] private TMP_Text specialAnimal;
        [SerializeField] private TMP_Text equipAnimalValeText;

        [Header("레벨 이미지")]
        [Tooltip("1성 ~ 5성 이미지. SlotUI_ToolInv 와 동일한 배열 사용")]
        [SerializeField] private Sprite[] levelSprites;

        [Header("등급 이미지")]
        [Tooltip("Normal(0) ~ Legendary(4). ItemGrade enum 순서와 동일하게 배열")]
        [FormerlySerializedAs("gradelSprites")]
        [SerializeField] private Sprite[] gradeSprites;

        private string currentToolId;

        public string CurrentToolId => currentToolId;

        /// <summary>
        /// 상세 페이지 패널이 현재 열려 있는지 여부
        /// </summary>
        public bool IsOpen => toolInvPagePanel != null && toolInvPagePanel.activeSelf;

        private void Awake()
        {
            ResolveInventoryReference();

            if (levelupButton != null)
            {
                levelupButton.onClick.AddListener(OnClickLevelUp);
                levelupButton.gameObject.SetActive(false);
            }

            if (toolInvPagePanel != null)
                toolInvPagePanel.SetActive(false);
        }

        private void OnEnable()
        {
            ResolveInventoryReference();

            if (toolInventory != null)
                toolInventory.OnToolSlotChanged += HandleToolSlotChanged;

            // 장착 동물의 레벨/수량 변경 시 equipAnimalValeText 등도 갱신
            if (InventoryManager_Animal.Instance != null)
                InventoryManager_Animal.Instance.OnAnimalSlotChanged += HandleAnimalSlotChanged;
        }

        private void OnDisable()
        {
            if (toolInventory != null)
                toolInventory.OnToolSlotChanged -= HandleToolSlotChanged;

            if (InventoryManager_Animal.Instance != null)
                InventoryManager_Animal.Instance.OnAnimalSlotChanged -= HandleAnimalSlotChanged;
        }

        private void OnDestroy()
        {
            if (levelupButton != null)
                levelupButton.onClick.RemoveListener(OnClickLevelUp);
        }

        private void ResolveInventoryReference()
        {
            if (toolInventory == null)
                toolInventory = InventoryManager_Tool.Instance;
        }

        /// <summary>
        /// 인벤토리 슬롯 데이터가 변경되었을 때,
        /// 현재 열려 있는 상세 페이지와 같은 도구면 UI를 재갱신합니다.
        /// </summary>
        private void HandleToolSlotChanged(SlotData_Tool slotData)
        {
            if (slotData == null)
                return;

            // 상세 페이지가 닫혀 있어도 특화 동물 장착 해금은 보정합니다.
            // (장착 시 Manager에서 이미 해금하지만, 구 세이브/예외 경로 대비)
            TryRevealSpecialAnimalIfMatched(slotData);

            if (toolInvPagePanel == null || !toolInvPagePanel.activeSelf)
                return;

            if (string.IsNullOrEmpty(currentToolId))
                return;

            if (slotData.ToolId != currentToolId)
                return;

            RefreshToolInvPage();
        }

        /// <summary>
        /// 동물 슬롯(레벨업 등)이 변경되면, 열려 있는 도구 상세의 장착 동물 생산량을 갱신합니다.
        /// UIController_AnimalInvPage.HandleToolSlotChanged와 대칭되는 흐름입니다.
        /// </summary>
        private void HandleAnimalSlotChanged(SlotData_Animal slotData)
        {
            if (slotData == null)
                return;

            if (toolInvPagePanel == null || !toolInvPagePanel.activeSelf)
                return;

            if (string.IsNullOrEmpty(currentToolId))
                return;

            RefreshToolInvPage();
        }

        /// <summary>
        /// 인벤토리 슬롯 클릭 시 선택한 도구의 런타임 데이터로
        /// 상세 페이지를 갱신하고 패널을 엽니다.
        /// </summary>
        public void OpenToolInvPage(SlotData_Tool slotData)
        {
            if (slotData == null)
            {
                Debug.LogWarning("[UIController_ToolInvPage] 표시할 슬롯 데이터가 없습니다.");
                return;
            }

            OpenToolInvPage(slotData.ToolId);
        }

        /// <summary>
        /// 도구 ID로 상세 페이지를 엽니다.
        /// 항상 InventoryManager_Tool에서 최신 슬롯 데이터를 재조회합니다.
        /// </summary>
        public void OpenToolInvPage(string toolId)
        {
            if (string.IsNullOrEmpty(toolId))
            {
                Debug.LogWarning("[UIController_ToolInvPage] 표시할 도구 ID가 비어 있습니다.");
                return;
            }

            if (toolInvPagePanel == null)
            {
                Debug.LogWarning("[UIController_ToolInvPage] toolInvPagePanel이 연결되지 않았습니다.");
                return;
            }

            currentToolId = toolId;

            RefreshToolInvPage();
            toolInvPagePanel.SetActive(true);
            BringPanelCanvasToFront(toolInvPagePanel);
        }

        /// <summary>
        /// 상세 페이지 Canvas를 UIWindowLayerManager로 최상단 sortingOrder에 올립니다.
        /// </summary>
        private static void BringPanelCanvasToFront(GameObject panel)
        {
            if (panel == null)
                return;

            if (panel.TryGetComponent(out UIPanelWindow panelWindow))
            {
                panelWindow.BringCanvasToFront();
                return;
            }

            Canvas canvas = panel.GetComponentInParent<Canvas>();
            if (canvas == null || UIWindowLayerManager.Instance == null)
                return;

            UIWindowLayerManager.Instance.BringToFront(canvas);
        }

        /// <summary>
        /// 현재 도구 ID 기준으로 최신 슬롯 데이터를 재조회하여 페이지를 갱신합니다.
        /// </summary>
        public void RefreshToolInvPage()
        {
            if (string.IsNullOrEmpty(currentToolId))
                return;

            ResolveInventoryReference();

            if (toolInventory == null)
            {
                Debug.LogWarning("[UIController_ToolInvPage] InventoryManager_Tool이 연결되지 않았습니다.");
                return;
            }

            if (!toolInventory.TryGetToolSlot(currentToolId, out SlotData_Tool slotData))
            {
                Debug.LogWarning($"[UIController_ToolInvPage] 보유하지 않은 도구입니다: {currentToolId}");
                return;
            }

            RefreshView(slotData);
        }

        private void RefreshView(SlotData_Tool slotData)
        {
            ToolDataSO data = slotData.ToolData;

            if (data == null)
            {
                Debug.LogWarning($"[UIController_ToolInvPage] ToolDataSO가 비어 있습니다: {currentToolId}");
                return;
            }

            // 현재 장착이 특화 동물이면 해금 보정 후 이름 표시에 반영
            TryRevealSpecialAnimalIfMatched(slotData);

            ApplyStaticInfo(slotData, data);
            ApplyRuntimeInfo(slotData, data);
            UpdateLevelUpButton(slotData);
        }

        /// <summary>
        /// ToolDataSO 기반 정적 정보 표시 (아이콘, 이름, 등급, 특화 동물)
        /// 현재 텍스트 깨짐 현상으로 인해 도구의 ID 로 표시하는 중
        /// </summary>
        private void ApplyStaticInfo(SlotData_Tool slotData, ToolDataSO data)
        {
            if (toolIconImage != null)
            {
                toolIconImage.sprite = data.Icon;
                toolIconImage.enabled = data.Icon != null;
            }

            if (toolNameText != null)
              toolNameText.text = data.DisplayName;
                /*toolNameText.text = data.Id;*/
            ApplyGradeImage(data.Grade);
            ApplySpecialAnimalName(slotData, data);
        }

        private void ApplyGradeImage(ItemGrade grade)
        {
            if (toolGradeImage == null)
                return;

            if (gradeSprites == null || gradeSprites.Length == 0)
            {
                toolGradeImage.enabled = false;
                return;
            }

            int gradeIndex = Mathf.Clamp((int)grade, 0, gradeSprites.Length - 1);
            Sprite gradeSprite = gradeSprites[gradeIndex];

            toolGradeImage.sprite = gradeSprite;
            toolGradeImage.enabled = gradeSprite != null;
        }

        /// <summary>
        /// ToolDataSO.SpecialAnimalId 기준으로 특화 동물 이름을 표시합니다.
        /// SlotData_Tool.HasRevealedSpecialAnimal이 false면 ???, true면 동물 이름을 표시합니다.
        /// </summary>
        private void ApplySpecialAnimalName(SlotData_Tool slotData, ToolDataSO data)
        {
            if (specialAnimal == null)
                return;

            string specialId = data.SpecialAnimalId;
            if (string.IsNullOrEmpty(specialId))
            {
                specialAnimal.text = string.Empty;
                return;
            }

            if (slotData == null || !slotData.HasRevealedSpecialAnimal)
            {
                specialAnimal.text = "특화 동물: ???";
                return;
            }

            AnimalDataSO animalData = null;
            if (InventoryManager_Animal.Instance != null)
                animalData = InventoryManager_Animal.Instance.GetAnimalData(specialId);

            string animalName = animalData != null && !string.IsNullOrEmpty(animalData.DisplayName)
                ? animalData.DisplayName
                : specialId;

            specialAnimal.text = $"특화 동물: {animalName}";
        }

        /// <summary>
        /// 현재 장착 동물이 이 도구의 특화 동물이면 이름 해금을 보정합니다.
        /// 정상 경로는 InventoryManager_Tool.TryAssignAnimalToTool에서 해금합니다.
        /// </summary>
        private void TryRevealSpecialAnimalIfMatched(SlotData_Tool slotData)
        {
            if (slotData == null || slotData.ToolData == null)
                return;

            if (slotData.HasRevealedSpecialAnimal)
                return;

            if (!slotData.CurrentAnimalSet || string.IsNullOrEmpty(slotData.CurrentAnimalId))
                return;

            string specialId = slotData.ToolData.SpecialAnimalId;
            if (string.IsNullOrEmpty(specialId))
                return;

            if (slotData.CurrentAnimalId != specialId)
                return;

            slotData.RevealSpecialAnimal();
        }

        /// <summary>
        /// SlotData_Tool 기반 런타임 정보 표시 (수량, 레벨, 생산량, 장착 동물 아이콘)
        /// </summary>
        private void ApplyRuntimeInfo(SlotData_Tool slotData, ToolDataSO data)
        {
            // 본체 1개 제외한 재료 수량 표시 (SlotUI_ToolInv 와 동일 규칙)
            if (currentCountText != null)
                currentCountText.text = "현재 수량 : " + Mathf.Max(0, slotData.CurrentCount - 1).ToString();

            if (requireCountText != null)
                requireCountText.text = "레벨업 필요 수량 : " + $"{slotData.RequiredUpgradeCount}";

            ApplyLevelImage(slotData.Level);

            if (currentProductCoin != null)
            {
                float coinPerSecond = data.BaseCoinPerSecond * data.CalculateLevelMultiplier(slotData.Level);
                currentProductCoin.text = "초당 생산량 : " + $"{coinPerSecond:0.#}/s";
            }

            // 장착 동물 아이콘은 CurrentAnimalId로 조회해 표시, 없으면 숨김
            ApplyUsingAnimalIcon(slotData);
        }

        /// <summary>
        /// 이 도구에 장착된 동물 아이콘·이름·초당 생산량을 표시합니다.
        /// CurrentAnimalSet / CurrentAnimalId가 채워지면(동물 Inv에서 도구 장착 후) 갱신됩니다.
        /// 생산량 표시는 UIController_AnimalInvPage.ApplyUsingToolIcon의 equipToolValeText와 동일 규칙입니다.
        /// </summary>
        private void ApplyUsingAnimalIcon(SlotData_Tool slotData)
        {
            Sprite animalIcon = null;
            string animalDisplayName = string.Empty;
            AnimalDataSO animalData = null;
            SlotData_Animal animalSlot = null;

            bool hasEquippedAnimal = slotData != null &&
                slotData.CurrentAnimalSet &&
                !string.IsNullOrEmpty(slotData.CurrentAnimalId) &&
                InventoryManager_Animal.Instance != null;

            if (hasEquippedAnimal)
            {
                InventoryManager_Animal.Instance.TryGetAnimalSlot(
                    slotData.CurrentAnimalId, out animalSlot);

                animalData = animalSlot != null
                    ? animalSlot.AnimalData
                    : InventoryManager_Animal.Instance.GetAnimalData(slotData.CurrentAnimalId);

                if (animalData != null)
                {
                    animalIcon = animalData.Icon;
                    animalDisplayName = animalData.DisplayName;
                }

                // DisplayName이 비어 있으면 Id로 대체
                if (string.IsNullOrEmpty(animalDisplayName))
                    animalDisplayName = slotData.CurrentAnimalId;
            }

            if (usingAnimalIcon != null)
            {
                usingAnimalIcon.sprite = animalIcon;
                usingAnimalIcon.enabled = animalIcon != null;
            }

            if (equipAnimalText != null)
            {
                equipAnimalText.text = hasEquippedAnimal
                    ? "배치된 주민: " + animalDisplayName
                    : "도구를 사용중인 주민이 없어요";
            }


            // 장착 동물 초당 생산량 표시. 미장착이면 숨김 (equipToolValeText와 동일)
            if (equipAnimalValeText != null)
            {
                if (hasEquippedAnimal && animalData != null && animalSlot != null)
                {
                    float animalCoinPerSecond = animalData.BaseCoinPerSecond
                        * animalData.CalculateLevelMultiplier(animalSlot.Level);
                    equipAnimalValeText.text = $"+ {animalCoinPerSecond:0.#}/s";
                    equipAnimalValeText.gameObject.SetActive(true);
                }
                else
                {
                    equipAnimalValeText.gameObject.SetActive(false);
                }
            }
        }

        private void ApplyLevelImage(int level)
        {
            if (toolLevelImage == null)
                return;

            if (levelSprites == null || levelSprites.Length == 0)
            {
                toolLevelImage.enabled = false;
                return;
            }

            int levelIndex = Mathf.Clamp(level - 1, 0, levelSprites.Length - 1);
            Sprite levelSprite = levelSprites[levelIndex];

            toolLevelImage.sprite = levelSprite;
            toolLevelImage.enabled = levelSprite != null;
        }

        /// <summary>
        /// 레벨업 요구 조건을 만족할 때 버튼을 표시합니다. (Animal Inv Page와 동일)
        /// </summary>
        private void UpdateLevelUpButton(SlotData_Tool slotData)
        {
            if (levelupButton == null)
                return;

            bool canLevelUp = toolInventory != null &&
                toolInventory.CanLevelUpTool(slotData.ToolId);

            levelupButton.gameObject.SetActive(canLevelUp);
        }

        private void OnClickLevelUp()
        {
            if (string.IsNullOrEmpty(currentToolId))
                return;

            ResolveInventoryReference();

            if (toolInventory == null)
            {
                Debug.LogWarning("[UIController_ToolInvPage] InventoryManager_Tool이 연결되지 않았습니다.");
                return;
            }

            toolInventory.TryLevelUpTool(currentToolId);
            RefreshToolInvPage();
        }

        /// <summary>
        /// 상세 페이지 닫기 버튼용
        /// </summary>
        public void CloseToolInvPage()
        {
            currentToolId = string.Empty;

            if (toolInvPagePanel == null)
                return;

            toolInvPagePanel.SetActive(false);
        }
    }
}