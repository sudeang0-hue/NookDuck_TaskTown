/* 개별 동물 슬롯의 화면 표시를 담당함
 * 현재 수량 표시
 * 필요 수량 표시
 * 레벨업 가능시 버튼 활성화
 * 불가능하면 버튼 비활성화
 * 슬롯 클릭 시 Animal_Inv_Page 오픈
 * [2026.07.27 나범 수정] 5종 고유 카드 배경 스프라이트 통스왑 로직 적용
 */

using System.Collections.Generic;
using Animal.Data;
using TaskTown.KDH;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

public class SlotUI_AnimalInv : SlotUIBase
{
    [Header("References")]
    [SerializeField] private InventoryManager_Animal animalInventory;

    [Header("해당 동물의 데이터")]
    [SerializeField] private Image animalIconImage;
    [SerializeField] private TMP_Text animalNameText;
    [SerializeField] private TMP_Text currentCountText;
    [SerializeField] private TMP_Text requireCountText;

    // ------------------------------------------------------------------------------------------
    // [2026.07.27 업데이트] 5가지 개별 카드 배경 스프라이트 에셋 바인딩
    // ------------------------------------------------------------------------------------------
    [Header("카드 배경 (등급별 5종 개별 이미지)")]
    [Tooltip("슬롯 카드의 바탕이 되는 UI Image 컴포넌트")]
    [SerializeField] private Image cardBackgroundImage;

    [Tooltip("Size를 5로 설정하고 각 등급별 전용 배경 스프라이트를 드래그 앤 드롭하세요.\n[0]: 노말, [1]: 레어, [2]: 에픽, [3]: 유니크, [4]: 레전더리")]
    [SerializeField] private Sprite[] gradeBackgroundSprites = new Sprite[5];
    // ------------------------------------------------------------------------------------------

    [Header("동물 인벤토리 슬롯")]
    [SerializeField] private Image hasTool;
    [SerializeField] private Image setVillage;

    [Tooltip("현재 레벨의 별 모양 이미지")]
    [SerializeField] private Image levelImage;

    [Header("레벨업 버튼")]
    [SerializeField] private Button levelupButton;

    [Header("레벨업 이미지")]
    [Tooltip("1성 ~ 5성 이미지. 단일 컬러")]
    [SerializeField] private Sprite[] levelSprites;

    [Header("클릭 범위 버튼")]
    [SerializeField] private Button coverButton;

    private SlotData_Animal currentSlotData;
    private UIController_AnimalInvPage animalInvPageController;

    // ------------------------------------------------------------------------------------------
    // [2026.07.27 업데이트] O(N) 씬 탐색을 피하기 위한 Placement UI 캐싱 변수
    private UIController_ToolPlacement cachedPlacementUi;
    // ------------------------------------------------------------------------------------------

    // 마을 확정 배치 조회용 캐시
    private VillageAnimalSetUI_Manager cachedVillageAnimalSet;

    // -----------------------------------------------------------------------------
    // [ 2026.08.03 - Choi - 튜토리얼 단계별 강조 연동 ]
    // 기능: 동물 인벤토리의 특정 슬롯을 손가락 강조 대상으로 사용할 수 있도록
    //       슬롯의 클릭 버튼을 읽기 전용으로 공개합니다.
    // -----------------------------------------------------------------------------
    public Button CoverButton => coverButton;

    private void Awake()
    {
        ResolveInventoryReference();

        if (coverButton == null)
            coverButton = GetComponentInChildren<Button>(true);

        if (levelupButton != null)
        {
            levelupButton.onClick.AddListener(OnClickLevelUp);
            levelupButton.gameObject.SetActive(false);
        }

        if (animalInventory == null)
            animalInventory = InventoryManager_Animal.Instance;
    }

    private void OnEnable()
    {
        if (coverButton != null)
            coverButton.onClick.AddListener(HandleSlotClicked);
    }

    private void OnDisable()
    {
        if (coverButton != null)
            coverButton.onClick.RemoveListener(HandleSlotClicked);
    }

    /// <summary>
    /// 슬롯 데이터와 상세 페이지 Controller를 연결합니다.
    /// </summary>
    public void Initialize(
        SlotData_Animal slotData,
        InventoryManager_Animal inventory = null,
        UIController_AnimalInvPage pageController = null)
    {
        if (inventory != null)
            animalInventory = inventory;

        if (pageController != null)
            animalInvPageController = pageController;

        ResolveInventoryReference();

        if (slotData == null)
        {
            Debug.LogWarning("[SlotUI_AnimalInv] 초기화할 슬롯 데이터가 없습니다.", this);
            return;
        }

        if (coverButton == null)
            Debug.LogWarning("[SlotUI_AnimalInv] coverButton이 연결되지 않았습니다.", this);

        currentSlotData = slotData;
        RefreshView();
    }

    public void Refresh(SlotData_Animal slotData)
    {
        if (slotData == null)
        {
            Debug.LogWarning("[SlotUI_AnimalInv] 갱신할 슬롯 데이터가 없습니다.", this);
            return;
        }

        currentSlotData = slotData;
        RefreshView();
    }

    private void ResolveInventoryReference()
    {
        if (animalInventory == null)
            animalInventory = InventoryManager_Animal.Instance;
    }

    private void RefreshView()
    {
        if (currentSlotData == null)
            return;

        AnimalDataSO data = currentSlotData.AnimalData;

        if (data == null)
            return;

        SetBaseInfo(data.Id, data.DisplayName, data.Icon);
        ApplyIconAndName(data);

        // ------------------------------------------------------------------------------------------
        // [2026.07.27 업데이트 ]동물의 등급에 따른 5종 고유 배경 스프라이트 적용
        ApplyGradeBackground(data);
        // ------------------------------------------------------------------------------------------


        // 본체 1마리를 제외한 재료 수량을 UI에 표시 (예: 내부 1 → 0/4)
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
    /// 도구 장착 / 마을 확정 배치 상태 아이콘만 갱신합니다.
    /// </summary>
    public void RefreshStatusIcons()
    {
        UpdateStatusIcons();
    }

    private void UpdateStatusIcons()
    {
        string animalId = currentSlotData != null ? currentSlotData.AnimalId : string.Empty;

        bool toolEquipped = HasToolEquipped(animalId);
        if (hasTool != null)
            hasTool.gameObject.SetActive(toolEquipped);

        bool villagePlaced = IsConfirmedVillagePlaced(animalId);
        if (setVillage != null)
            setVillage.gameObject.SetActive(villagePlaced);
    }

    private static bool HasToolEquipped(string animalId)
    {
        if (string.IsNullOrEmpty(animalId) || InventoryManager_Tool.Instance == null)
            return false;

        IReadOnlyList<SlotData_Tool> toolSlots = InventoryManager_Tool.Instance.ToolSlotsList;
        if (toolSlots == null)
            return false;

        for (int i = 0; i < toolSlots.Count; i++)
        {
            SlotData_Tool toolSlot = toolSlots[i];
            if (toolSlot != null && toolSlot.CurrentAnimalSet && toolSlot.CurrentAnimalId == animalId)
                return true;
        }

        return false;
    }

    private bool IsConfirmedVillagePlaced(string animalId)
    {
        if (string.IsNullOrEmpty(animalId))
            return false;

        if (cachedVillageAnimalSet == null)
            cachedVillageAnimalSet = FindFirstObjectByType<VillageAnimalSetUI_Manager>();

        if (cachedVillageAnimalSet == null)
            return false;

        return cachedVillageAnimalSet.IsConfirmedPlaced(animalId);
    }

    private void ApplyIconAndName(AnimalDataSO data)
    {
        if (animalIconImage != null)
        {
            animalIconImage.sprite = data.Icon;
            animalIconImage.enabled = data.Icon != null;
        }

        if (animalNameText != null)
            animalNameText.text = data.DisplayName;
    }

    // ------------------------------------------------------------------------------------------
    // [2026.07.27 업데이트 ] 등급에 따라 5개의 고유 배경 스프라이트 중 하나를 1:1 교체
    // ------------------------------------------------------------------------------------------
    private void ApplyGradeBackground(AnimalDataSO data)
    {
        if (cardBackgroundImage == null)
        {
            Debug.LogWarning("[SlotUI_AnimalInv] cardBackgroundImage가 할당되지 않았습니다.", this);
            return;
        }

        if (gradeBackgroundSprites == null || gradeBackgroundSprites.Length == 0)
        {
            Debug.LogWarning("[SlotUI_AnimalInv] gradeBackgroundSprites 배열이 비어있습니다.", this);
            return;
        }

        // 1. Grade Enum/int 값을 인덱스로 변환 (0: Normal, 1: Rare, 2: Epic, 3: Unique, 4: Legendary)
        int rawIndex = (int)data.Grade;

        // 2. 배열 범위를 안전하게 클램핑 ($0 \le Index \le \text{Length}-1$)
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
            Debug.LogWarning($"[SlotUI_AnimalInv] {safeIndex}번 등급에 해당하는 배경 스프라이트가 Inspector에 할당되지 않았습니다.", this);
        }
    }

    private void UpdateLevelUpButton()
    {
        if (levelupButton == null)
            return;

        bool canLevelUp = animalInventory != null &&
            animalInventory.CanLevelUpAnimal(currentSlotData.AnimalId);

        levelupButton.gameObject.SetActive(canLevelUp);
    }

    /// <summary>
    /// 슬롯 클릭 시 동물 상세 페이지를 토글합니다.
    /// 같은 동물이 이미 열려 있으면 Close, 아니면 Open합니다.
    /// </summary>
    private void HandleSlotClicked()
    {
        if (currentSlotData == null)
        {
            Debug.LogWarning("[SlotUI_AnimalInv] 현재 슬롯에 동물 데이터가 없습니다.", this);
            return;
        }

        if (animalInvPageController == null)
        {
            Debug.LogWarning("[SlotUI_AnimalInv] UIController_AnimalInvPage가 연결되지 않았습니다.", this);
            return;
        }

        if (animalInvPageController.IsOpen &&
            animalInvPageController.CurrentAnimalId == currentSlotData.AnimalId)
        {
            animalInvPageController.CloseAnimalInvPage();
            return;
        }

        // ------------------------------------------------------------------------------------------
        // [2026.07.27 업데이트 ] 지연 캐싱(Lazy Caching)을 통해 O(N) 씬 탐색 연산을 최초 1회로 단축
        // ------------------------------------------------------------------------------------------
        if (cachedPlacementUi == null)
        {
            cachedPlacementUi = FindFirstObjectByType<UIController_ToolPlacement>();
        }

        if (cachedPlacementUi != null)
        {
            cachedPlacementUi.SetSelectedAnimal(currentSlotData.AnimalId);
        }
        // ------------------------------------------------------------------------------------------

        animalInvPageController.OpenAnimalInvPage(currentSlotData);
    }

    private void OnClickLevelUp()
    {
        if (currentSlotData == null)
        {
            Debug.LogWarning("[SlotUI_AnimalInv] 레벨업할 슬롯 데이터가 없습니다.", this);
            return;
        }

        ResolveInventoryReference();

        if (animalInventory == null)
        {
            Debug.LogWarning("[SlotUI_AnimalInv] InventoryManager_Animal이 연결되지 않았습니다.", this);
            return;
        }

        animalInventory.TryLevelUpAnimal(currentSlotData.AnimalId);
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

        if (animalIconImage != null)
        {
            animalIconImage.sprite = null;
            animalIconImage.enabled = false;
        }

        if (animalNameText != null)
            animalNameText.text = string.Empty;

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
        // [2026.07.27 업데이트 ] 슬롯 초기화 시 카드 배경 스프라이트도 깔끔하게 리셋
        if (cardBackgroundImage != null)
        {
            cardBackgroundImage.sprite = null;
            cardBackgroundImage.enabled = false;
        }
        // ------------------------------------------------------------------------------------------

        if (levelupButton != null)
            levelupButton.gameObject.SetActive(false);

        if (hasTool != null)
            hasTool.gameObject.SetActive(false);

        if (setVillage != null)
            setVillage.gameObject.SetActive(false);
    }
}
