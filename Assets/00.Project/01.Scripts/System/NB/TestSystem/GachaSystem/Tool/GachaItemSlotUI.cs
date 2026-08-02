//NB

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TaskTown.Gacha;
using Tool.Data;

// 도구 가챠 결과물 단일 슬롯 UI 컴포넌트
public class GachaItemSlotUI : MonoBehaviour
{
    [Header("기본 UI 컴포넌트")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Image gradeImage; //  등급 스프라이트를 표출할 Image 컴포넌트
    [SerializeField] private TextMeshProUGUI nameText;

    [Header("★ 등급별 스프라이트 리소스 (5종) ★")]
    [Tooltip("Enum 순서(0: Common, 1: Rare, 2: Epic, 3: Unique, 4: Legendary 등)에 맞게 5개의 스프라이트를 할당하세요.")]
    [SerializeField] private Sprite[] gradeSprites = new Sprite[5];

    [Header("도구 전용 추가 UI")]
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private GameObject specialBonusBadge;

    public GachaEntryData CurrentEntryData { get; private set; }

    public void SetItem(GachaEntryData entryData)
    {
        if (entryData == null)
        {
            ClearSlot();
            return;
        }

        CurrentEntryData = entryData;

        // 1. 아이콘 렌더링 및 원본 비율(Preserve Aspect) 보정
        if (iconImage == null) iconImage = GetComponentInChildren<Image>();
        if (iconImage != null)
        {
            iconImage.sprite = entryData.Icon;
            iconImage.enabled = (entryData.Icon != null);

            if (entryData.Icon != null)
            {
                iconImage.type = Image.Type.Simple;
                iconImage.preserveAspect = true;
            }
        }

        // 2. 등급 스프라이트 바인딩 ($O(1)$ Direct Lookup) ★
        UpdateGradeUI(entryData);

        // 3. 텍스트 정보 바인딩
        if (nameText != null) nameText.text = entryData.DisplayName;

        // 4. ToolDataSO 다형성 처리
        if (entryData is ToolDataSO toolData)
        {
            if (descriptionText != null) descriptionText.text = toolData.ToolDescription;
            if (specialBonusBadge != null)
            {
                bool hasSpecialBonus = !string.IsNullOrEmpty(toolData.SpecialAnimalId) && toolData.SpecialAnimalBonusRate > 0f;
                specialBonusBadge.SetActive(hasSpecialBonus);
            }
        }
        else
        {
            if (descriptionText != null) descriptionText.text = string.Empty;
            if (specialBonusBadge != null) specialBonusBadge.SetActive(false);
        }
    }
    // 등급 Enum 값을 안전하게 인덱싱하여 스프라이트 및 텍스트 업데이트
    private void UpdateGradeUI(GachaEntryData entryData)
    {
        int gradeIndex = (int)entryData.Grade;

        // 등급 스프라이트 업데이트
        if (gradeImage != null)
        {
            if (gradeSprites != null && gradeSprites.Length > 0)
            {
                // 인덱스 범위 초과 방지 램프 수식 ($0 \le i < N_{\text{sprites}}$)
                int clampedIndex = Mathf.Clamp(gradeIndex, 0, gradeSprites.Length - 1);
                Sprite targetSprite = gradeSprites[clampedIndex];

                gradeImage.sprite = targetSprite;
                gradeImage.enabled = (targetSprite != null);
            }
            else
            {
                gradeImage.enabled = false;
            }
        }

    }

    public void ClearSlot()
    {
        CurrentEntryData = null;

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        if (gradeImage != null)
        {
            gradeImage.sprite = null;
            gradeImage.enabled = false;
        }

        if (nameText != null) nameText.text = string.Empty;
        if (descriptionText != null) descriptionText.text = string.Empty;
        if (specialBonusBadge != null) specialBonusBadge.SetActive(false);
    }
}