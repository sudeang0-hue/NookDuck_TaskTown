/* 개별 도구 슬롯의 화면 표시를 담당함
 * 현재 수량 표시
 * 필요 수량 표시
 * 레벨업 가능시 버튼 활성화
 * 불가능하면 버튼 비활성화
 */

using TaskTown.KDH;
using Tool.Data;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

public class SlotUI_ToolInv : SlotUIBase
{
    [Header("References")]
    [SerializeField] private InventoryManager_Tool toolInventory;

    [Header("해당 도구 데이터")]
    [SerializeField] private Image toolIconImage;
    [SerializeField] private TMP_Text toolNameText;
    [SerializeField] private TMP_Text currentCountText;
    [SerializeField] private TMP_Text requireCountText;

    [Header("도구 인벤토리 슬롯 UI")]
    [Tooltip("현재 레벨의 별 모양 이미지")]
    [SerializeField] private Image levelImage;

    [Header("레벨업 버튼")]
    [SerializeField] private Button levelupButton;

    [Tooltip("현재 이 도구에 배치된 동물 아이콘")]
    [SerializeField] private Image curentAnimalicon;
    [Tooltip("이 도구의 특화 동물 아이콘")]
    [SerializeField] private Image specialAnimalicon;
    [Tooltip("이 도구의 시간당 생산량 텍스트")]
    [SerializeField] private TMP_Text outoCoinPerHourText;

    [Header("레벨업 이미지")]
    [Tooltip("1성 ~ 5성 이미지. 단일 컬러")]
    [SerializeField] private Sprite[] levelSprites;

    private SlotData_Tool currentSlotData;

    private void Awake()
    {
        ResolveInventoryReference();

        if (levelupButton != null)
        {
            levelupButton.onClick.AddListener(OnClickLevelUp);
            levelupButton.gameObject.SetActive(false);
        }
    }

    public void Initialize(SlotData_Tool slotData, InventoryManager_Tool inventory = null)
    {
        if (inventory != null)
            toolInventory = inventory;

        ResolveInventoryReference();

        if (slotData == null)
        {
            Debug.LogWarning("[SlotUI_ToolInv] 초기화할 슬롯 데이터가 없습니다.", this);
            return;
        }

        currentSlotData = slotData;
        RefreshView();
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

    private void ApplyIconAndName(ToolDataSO data)
    {
        if (toolIconImage != null)
        {
            toolIconImage.sprite = data.Icon;
            toolIconImage.enabled = data.Icon != null;
        }

        if (toolNameText != null)
            toolNameText.text = data.DisplayName;
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
