/* 개별 동물 슬롯의 화면 표시를 담당함
 * 현재 수량 표시
 * 필요 수량 표시
 * 레벨업 가능시 버튼 활성화
 * 불가능하면 버튼 비활성화
 */

using Animal.Data;
using UI;
using Test;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class SlotUI_AnimalInv : SlotUIBase
{
    [Header("References")]
    [SerializeField] private TestInventory_Animal animalInventory;


    [Header("해당 동물의 데이터")]
    [SerializeField] private AnimalDataSO animalData;
    [SerializeField] private Image animalIconImage;
    [SerializeField] private TMP_Text animalNameText;
    [SerializeField] private TMP_Text currentCountText;
    [SerializeField] private TMP_Text requireCountText;


    [Header("동물 인벤토리 슬롯 UI")]
    [Tooltip("현재 레벨의 별 모양 이미지")]
    [SerializeField] private Image levelImage;
    [Header("레벨업 버튼")]
    [SerializeField] private Button levelupButton;

    private SlotData_Animal currentSlotData; // 이 UI 슬롯이 표시하고 있는 런타임 슬롯 데이터
    private int countText;  // 현재 보유중인 수량
    private int needCountText;  // 다음 레벨업에 필요한 수량

    [Header("레벨업 이미지")]
    [Tooltip("1성 ~ 5성 이미지. 단일 컬러")]
    [SerializeField] private Sprite[] levelSprites;

    //[Tooltip("1성 ~ 5성 이미지를 등급에 따라 나누는 경우 사용.\n" +
    //    "ex. Normal 등급은 별 색상 노랑, Rare 등급에서는 녹색, Epic 등급은 파랑 등")]
    //[SerializeField] private List<GradelevelIamge> levelIamges = new List<GradelevelIamge>();

    //[System.Serializable]
    //public class GradelevelIamge
    //{
    //    [SerializeField] private Image Levle_1;
    //    [SerializeField] private Image Levle_2;
    //    [SerializeField] private Image Levle_3;
    //    [SerializeField] private Image Levle_4;
    //    [SerializeField] private Image Levle_5;
    //}

    private void Awake()
    {
        if(animalInventory == null)
        {
            animalInventory = FindAnyObjectByType<TestInventory_Animal>();
        }

        if (levelupButton != null)
        {
            levelupButton.onClick.AddListener(OnClickLevelUp);
        }
    }

    public void Initialize(SlotData_Animal slotData)
    {
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
            Debug.LogWarning("[SlotUI_AnimalInv] 초기화할 슬롯 데이터가 없습니다.", this);
            return;
        }

        currentSlotData = slotData;
        RefreshView();
    }


    private void RefreshView()
    {
        if (currentSlotData == null)
            return;

        AnimalDataSO animalData = currentSlotData.AnimalData;

        if (animalData == null)
            return;

        if (animalIconImage != null)
        {
            animalIconImage.sprite = animalData.Icon;
        }

        if (animalNameText != null)
        {
            animalNameText.text = animalData.DisplayName;
        }

        if (currentCountText != null)
        {
            currentCountText.text = $"{currentSlotData.CurrentCount}";
        }

        if (requireCountText != null)
        {
            requireCountText.text = $"/ {currentSlotData.RequiredUpgradeCount}";
        }

        if (levelImage != null && levelSprites != null && levelSprites.Length > 0)
        {
            int levelIndex = Mathf.Clamp(currentSlotData.Level - 1, 0, levelSprites.Length - 1);

            levelImage.sprite = levelSprites[levelIndex];
        }

        if (levelupButton != null)
        {
            bool canLevelUp =
                !currentSlotData.IsMaxLevel &&
                currentSlotData.RequiredUpgradeCount > 0 &&
                currentSlotData.CurrentCount >= currentSlotData.RequiredUpgradeCount;

            levelupButton.gameObject.SetActive(canLevelUp);
        }

    }

    /// <summary>
    /// 현재 동물의 레벨업을 요청합니다.
    /// 실제 레벨업 처리는 TestInventory_Animal에서 수행합니다.
    /// </summary>
    private void OnClickLevelUp()
    {
        if (currentSlotData == null)
        {
            Debug.LogWarning("[SlotUI_AnimalInv] 레벨업할 슬롯 데이터가 없습니다..");
            return;
        }

        if (animalInventory == null)
        {
            Debug.LogWarning("[SlotUI_AnimalInv] TestInventory_Animal이 연결되지 않았습니다.");
            return;
        }

        animalInventory.TryLevelUpAnimal(currentSlotData.AnimalId);
    }


    private void OnDestroy()
    {
        if (levelupButton != null)
        {
            levelupButton.onClick.RemoveListener(OnClickLevelUp);
        }
    }


    public override void Clear()
    {
        base.Clear();

        if (currentCountText != null)
            currentCountText.text = string.Empty;

        if (requireCountText != null)
            requireCountText.text = string.Empty;

        if (levelImage != null)
            levelImage.sprite = null;

        if (levelupButton != null)
            levelupButton.gameObject.SetActive(false);


    }

}

