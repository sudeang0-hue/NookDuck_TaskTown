//NB

using TaskTown.Gacha;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Animal.Data;

// 동물 가챠 결과물 단일 슬롯 UI 컴포넌트
public class GachaAnimalSlotUI : MonoBehaviour
{
    [Header("기본 UI 컴포넌트")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Image gradeImage; // 등급 스프라이트를 표출할 Image 컴포넌트
    [SerializeField] private TextMeshProUGUI nameText;

    [Header("★ 등급별 스프라이트 리소스 (5종) ★")]
    [Tooltip("Enum 순서(0: Common, 1: Rare, 2: Epic, 3: Unique, 4: Legendary 등)에 맞게 5개의 스프라이트를 할당하세요.")]
    [SerializeField] private Sprite[] gradeSprites = new Sprite[5];

    public GachaEntryData CurrentEntryData { get; private set; }

    public void SetItem(GachaEntryData entryData)
    {
        if (entryData == null)
        {
            ClearSlot();
            return;
        }

        CurrentEntryData = entryData;

        // 1. 동물 전용 프로필 이미지(ProfileImage) 및 기본 아이콘(Icon) 추출 ($O(1)$)
        Sprite targetSprite = GetBestSprite(entryData);

        // 1-1. 아이콘 렌더링 및 비율 보정
        if (iconImage == null) iconImage = GetComponentInChildren<Image>();
        if (iconImage != null)
        {
            iconImage.sprite = targetSprite;
            iconImage.enabled = (targetSprite != null);

            if (targetSprite != null)
            {
                iconImage.type = Image.Type.Simple;
                iconImage.preserveAspect = true;
            }
        }

        // 2. 등급 스프라이트 바인딩
        UpdateGradeUI(entryData);

        // 3. 동물 전용 이름(AnimalDisplayName_) 바인딩
        UpdateNameText(entryData);
    }


    // AnimalDataSO의 AnimalDisplayName_을 우선 표출하고, 데이터가 없거나 비어있을 경우 기본 DisplayName으로 Fallback 처리
    private void UpdateNameText(GachaEntryData entryData)
    {
        if (nameText == null) return;

        // C# Pattern Matching 사용 (타입 검사 + 변수 할당을 1회 연산으로 최소화)
        if (entryData is AnimalDataSO animalData && !string.IsNullOrEmpty(animalData.AnimalDisplayName_))
        {
            nameText.text = animalData.AnimalDisplayName_;
        }
        else
        {
            // Fallback: 부모 클래스의 공통 DisplayName 사용
            nameText.text = entryData.DisplayName;
        }
    }

    // AnimalDataSO의 ProfileImage를 우선 탐색하고, 없으면 부모의 Icon을 반환
    private Sprite GetBestSprite(GachaEntryData entryData)
    {
        if (entryData is AnimalDataSO animalData && animalData.ProfileImage != null)
        {
            return animalData.ProfileImage;
        }

        return entryData.Icon;
    }

    // 등급 Enum 값을 안전하게 인덱싱하여 스프라이트 및 텍스트 업데이트
    private void UpdateGradeUI(GachaEntryData entryData)
    {
        int gradeIndex = (int)entryData.Grade;

        if (gradeImage != null)
        {
            if (gradeSprites != null && gradeSprites.Length > 0)
            {
                // 배열 바운스 방지 안전 매핑 ($0 \le i < N_{\text{sprites}}$)
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
    }
}