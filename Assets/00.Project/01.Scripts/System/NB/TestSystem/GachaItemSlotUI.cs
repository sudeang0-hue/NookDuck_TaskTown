//NB

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TaskTown.Gacha;
using Tool.Data;

public class GachaItemSlotUI : MonoBehaviour
{
    [Header("기본 UI 컴포넌트 ")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI gradeText;

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

        // 1. 아이콘 렌더링 및 비율 보정
        if (iconImage == null) iconImage = GetComponentInChildren<Image>();
        if (iconImage != null)
        {
            iconImage.sprite = entryData.Icon;
            iconImage.enabled = (entryData.Icon != null);

            if (entryData.Icon != null)
            {
                // 코드로 원본 비율 유지(Preserve Aspect) 강제 설정!
                iconImage.type = Image.Type.Simple;
                iconImage.preserveAspect = true;
            }
        }

        // 2. 텍스트 정보 바인딩
        if (nameText != null) nameText.text = entryData.DisplayName;
        if (gradeText != null) gradeText.text = entryData.Grade.ToString();

        // 3. ToolDataSO 다형성 처리
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

    public void ClearSlot()
    {
        CurrentEntryData = null;
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }
        if (nameText != null) nameText.text = string.Empty;
        if (gradeText != null) gradeText.text = string.Empty;
        if (descriptionText != null) descriptionText.text = string.Empty;
        if (specialBonusBadge != null) specialBonusBadge.SetActive(false);
    }
}