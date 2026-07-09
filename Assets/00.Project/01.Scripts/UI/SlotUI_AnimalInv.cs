using KAY;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class SlotUI_AnimalInv : SlotUIBase
{
        
    [Header("동물 인벤토리 슬롯 UI")]
    [Tooltip("현재 레벨의 별 모양 이미지")]
    [SerializeField] private Image levelImage;
    [Tooltip("현재 보유중인 수량 / 업그레이드 수량 텍스트")]
    [SerializeField] private TMP_Text upgradeCountText;
    private int countText;  // 현재 보유중인 수량
    private int needCountText;  // 다음 레벨업에 필요한 수량

    [Header("레벨업 이미지")]
    [Tooltip("1성 ~ 5성 이미지. 단일 컬러")]
    [SerializeField] private Sprite[] levelSprites;


    [Tooltip("1성 ~ 5성 이미지를 등급에 따라 나누는 경우 사용.\n" +
        "ex. Normal 등급은 별 색상 노랑, Rare 등급에서는 녹색, Epic 등급은 파랑 등")]
    [SerializeField] private List<GradelevelIamge> levelIamges = new List<GradelevelIamge>();
    
    [System.Serializable]
    public class GradelevelIamge
    {
        [SerializeField] private Image Levle_1;
        [SerializeField] private Image Levle_2;
        [SerializeField] private Image Levle_3;
        [SerializeField] private Image Levle_4;
        [SerializeField] private Image Levle_5;
    }


    /// <summary>
    /// 동물 슬롯 정보 갱신
    /// </summary>
    public void SetAnimalSlot(string id, string displayName, Sprite icon, int level, int currentCount, int needCount)
    {
        SetBaseInfo(id, displayName, icon);

        UpdateLevelImage(level);

        if (upgradeCountText != null)
            upgradeCountText.text = UpGrageCountText(currentCount, needCount);

    }

    public string UpGrageCountText(int currentCount, int needCount)
    {
        currentCount = countText;
        needCount = needCountText;

        return $"{currentCount} / {needCount}";
    }

    /// <summary>
    /// 레벨 이미지 변경
    /// </summary>
    private void UpdateLevelImage(int level)
    {
        if (levelImage == null)
            return;

        if (levelSprites == null || levelSprites.Length == 0)
            return;

        int index = Mathf.Clamp(level - 1, 0, levelSprites.Length - 1);

        levelImage.sprite = levelSprites[index];
    }

    /// <summary>
    /// 업그레이드 수량 텍스트 변경
    /// </summary>
    private void UpdataUpgradeCountText()
    {

    }


    public void AnimalLevelUpButtonClick()
    {
        Debug.Log("동물 레벨업 로직 실행. 이후 버튼 비활성화");
    }



    public override void Clear()
    {
        base.Clear();

        if (upgradeCountText != null)
            upgradeCountText.text = string.Empty;


        if (levelImage != null)
            levelImage.sprite = null;
    }


}

