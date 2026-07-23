using TMPro;
using UnityEngine;

namespace UI
{

    public class UIController_VillageUpgrade : MonoBehaviour
    {
        [Header("필수 레벨업 패널의 갱신되어야 하는 항목")]
        [Header("레벨 텍스트")]
        [SerializeField] private TMP_Text clickCoinLevel;
        [SerializeField] private TMP_Text typingCoinLevel;
        [SerializeField] private TMP_Text toolProductLevel;

        [Header("레벨업 코인 가격")]
        [SerializeField] private TMP_Text clickCoinLevelUpCost;
        [SerializeField] private TMP_Text typingCoinLevelUpCost;
        [SerializeField] private TMP_Text toolProductLevelUpCost;

        [Header("마을 레벨업 패널의 갱신되어야 하는 항목")]
        [Header("마을 레벨 텍스트")]
        [SerializeField] private TMP_Text villageLevel;

        [Header("마을 레벨업 코인 가격")]
        [SerializeField] private TMP_Text villageLevelUpCost;

    }
}