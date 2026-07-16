/* 개별 동물 슬롯의 화면 표시를 담당함
 * 현재 수량 표시
 * 필요 수량 표시
 * 레벨업 가능시 버튼 활성화
 * 불가능하면 버튼 비활성화
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

    [Header("동물 인벤토리 슬롯 UI")]
    [Tooltip("현재 레벨의 별 모양 이미지")]
    [SerializeField] private Image levelImage;

    [Header("레벨업 버튼")]
    [SerializeField] private Button levelupButton;

    [Header("레벨업 이미지")]
    [Tooltip("1성 ~ 5성 이미지. 단일 컬러")]
    [SerializeField] private Sprite[] levelSprites;

    private SlotData_Animal currentSlotData;

    private void Awake()
    {
        ResolveInventoryReference();

        if (levelupButton != null)
        {
            levelupButton.onClick.AddListener(OnClickLevelUp);
            levelupButton.gameObject.SetActive(false);
        }
    }

    public void Initialize(SlotData_Animal slotData, InventoryManager_Animal inventory = null)
    {
        if (inventory != null)
            animalInventory = inventory;

        ResolveInventoryReference();

        if (slotData == null)
        {
            Debug.LogWarning("[SlotUI_AnimalInv] 초기화할 슬롯 데이터가 없습니다.", this);
            return;
        }

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
