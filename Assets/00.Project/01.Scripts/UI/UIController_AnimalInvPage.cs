using Animal.Data;
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
        [SerializeField] private Image usingToolIcon;  // 장착하고있는 도구 아이콘 (도구 연동 단계에서 갱신 예정)

        [Header("레벨 이미지")]
        [Tooltip("1성 ~ 5성 이미지. SlotUI_AnimalInv 와 동일한 배열 사용")]
        [SerializeField] private Sprite[] levelSprites;

        [Header("등급 이미지")]
        [Tooltip("Normal(0) ~ Legendary(4). ItemGrade enum 순서와 동일하게 배열")]
        [FormerlySerializedAs("gradelSprites")]
        [SerializeField] private Sprite[] gradeSprites;

        [Header("해당 동물의 설정 상호작용 버튼")]
        [SerializeField] private Button levelupButton;         // 레벨업 버튼
        [SerializeField] private Button settingToolButton;     // 도구 배치 버튼 (추후 구현)
        [SerializeField] private Button settingVilliageButton; // 마을 배치 버튼 (추후 구현)

        // 현재 페이지에 표시 중인 동물 ID. 갱신 시 이 ID로 최신 데이터를 재조회한다.
        private string currentAnimalId;

        public string CurrentAnimalId => currentAnimalId;

        private void Awake()
        {
            ResolveInventoryReference();

            if (levelupButton != null)
            {
                levelupButton.onClick.AddListener(OnClickLevelUp);
                levelupButton.gameObject.SetActive(false);
            }

            if (animalInvPagePanel != null)
            {
                animalInvPagePanel.SetActive(false);
            }
        }

        private void OnEnable()
        {
            ResolveInventoryReference();

            if (animalInventory == null)
                return;

            animalInventory.OnAnimalSlotChanged += HandleAnimalSlotChanged;
        }

        private void OnDisable()
        {
            if (animalInventory == null)
                return;

            animalInventory.OnAnimalSlotChanged -= HandleAnimalSlotChanged;
        }

        private void OnDestroy()
        {
            if (levelupButton != null)
                levelupButton.onClick.RemoveListener(OnClickLevelUp);
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
                currentCountText.text = Mathf.Max(0, slotData.CurrentCount - 1).ToString();

            if (requireCountText != null)
                requireCountText.text = $"/ {slotData.RequiredUpgradeCount}";

            ApplyLevelImage(slotData.Level);

            // 현재 레벨 기준 초당 생산량 (기본 생산량 x 레벨 배율)
            if (currentProductCoin != null)
            {
                float coinPerSecond = data.BaseCoinPerSecond * data.CalculateLevelMultiplier(slotData.Level);
                currentProductCoin.text = $"{coinPerSecond:0.#}/s";
            }

            // TODO: usingToolIcon 은 도구 장착 시스템 연동 단계에서 갱신
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
        /// 상세 페이지 닫기 버튼용
        /// </summary>
        public void CloseAnimalInvPage()
        {
            currentAnimalId = string.Empty;

            if (animalInvPagePanel == null)
                return;

            animalInvPagePanel.SetActive(false);
        }
    }
}