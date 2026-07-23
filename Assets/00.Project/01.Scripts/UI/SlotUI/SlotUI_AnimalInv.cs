/* 개별 동물 슬롯의 화면 표시를 담당함
 * 현재 수량 표시
 * 필요 수량 표시
 * 레벨업 가능시 버튼 활성화
 * 불가능하면 버튼 비활성화
 * 슬롯 클릭 시 Animal_Inv_Page 오픈
 */

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

    [Header("동물 인벤토리 슬롯")]
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


        // ------------------------------------26.07.22 KDH 추가------------------------------------
        UIController_ToolPlacement placementUi = FindFirstObjectByType<UIController_ToolPlacement>();
        if (placementUi != null) placementUi.SetSelectedAnimal(currentSlotData.AnimalId);
        //------------------------------------------------------------------------------------------

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

        if (levelupButton != null)
            levelupButton.gameObject.SetActive(false);
    }
}