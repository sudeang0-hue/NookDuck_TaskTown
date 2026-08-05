using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// VillageUpgrade_root 표시 전담.
    /// 클릭/게이트/리셋 로직은 VillageUpgradeUI_Manager가 담당합니다.
    /// </summary>
    public class UIController_VillageUpgrade : MonoBehaviour
    {
        private const int RequiredUpgradeTotal = 3;

        [Header("필수 레벨업 패널의 갱신되어야 하는 항목")]

        [Header("클릭 획득량 부분")]
        [SerializeField] private TMP_Text clickCoinLevel;
        [SerializeField] private TMP_Text clickCoinValue;
        [SerializeField] private Button clickCoinUpButton;
        [SerializeField] private TMP_Text clickCoinLevelUpCost;
        [Tooltip("clickcoin_pan 하위 complete_img1")]
        [SerializeField] private GameObject clickCompleteImg;

        [Header("타이핑 획득량 부분")]
        [SerializeField] private TMP_Text typingCoinLevel;
        [SerializeField] private TMP_Text typingCoinValue;
        [SerializeField] private Button typingCoinUpButton;
        [SerializeField] private TMP_Text typingCoinLevelUpCost;
        [Tooltip("typingcoin_pan 하위 complete_img2")]
        [SerializeField] private GameObject typingCompleteImg;

        [Header("도구 효율 생산보너스 부분")]
        [SerializeField] private TMP_Text toolProductLevel;
        [SerializeField] private TMP_Text toolProductValue;
        [SerializeField] private Button toolProductUpButton;
        [SerializeField] private TMP_Text toolProductLevelUpCost;
        [Tooltip("toolprocoin_pan 하위 complete_img3")]
        [SerializeField] private GameObject toolCompleteImg;


        [Header("Required Upgrade 부분")]
        [SerializeField] private TMP_Text requiredUpgradeText;
        [Tooltip("0:클릭 / 1:타이핑 / 2:도구 효율 / 3:보유 코인")]
        [SerializeField] private GameObject[] checkImg;


        [Header("마을 패널의 갱신되어야 하는 항목")]
        [Tooltip("마을 레벨 텍스트")]
        [SerializeField] private TMP_Text villageLevel;
        [Tooltip("마을 레벨업 버튼")]
        [SerializeField] private Button villageLevelUpButton;
        [Tooltip("마을 레벨업 비용 텍스트 / VillageLevelUp_btn 자식 Text")]
        [SerializeField] private TMP_Text villageLevelUpCost;

        [Header("레벨업 보상 미리보기")]
        [Tooltip("0:도구 상한 / 1:동물 배치 상한 / 2:마을 장식 / 3:섬 모양 변경")]
        [SerializeField] private TMP_Text[] RewardTexts;
        [Tooltip("10레벨 도달 후 보상없음 텍스트")]
        [SerializeField] private TMP_Text RewardEnd_txt;


        [Header("창 닫기 버튼")]
        [SerializeField] private Button windowClose;

        [Header("보상 수치 (시스템 값과 동기화 유지)")]
        [Tooltip("InventoryManager_Tool.toolCapacityBase와 동일")]
        [SerializeField] private int toolCapacityBase = 5;
        [Tooltip("InventoryManager_Tool.toolCapacityAsymptoteRange와 동일")]
        [SerializeField] private float toolCapacityAsymptoteRange = 26.01f;
        [Tooltip("InventoryManager_Tool.toolCapacityDecayLevels와 동일")]
        [SerializeField] private float toolCapacityDecayLevels = 12f;
        [Tooltip("VillageSystemManager.maxPlacementCapacity와 동일")]
        [SerializeField] private int maxPlacementCapacity = 20;
        [Tooltip("VillageSystemManager.unlockedSlotsByTownLevel와 동일")]
        [SerializeField]
        private int[] unlockedSlotsByTownLevel =
        {
            5, 6, 7, 9, 10, 12, 13, 14, 16, 20
        };

        public Button ClickCoinUpButton => clickCoinUpButton;
        public Button TypingCoinUpButton => typingCoinUpButton;
        public Button ToolProductUpButton => toolProductUpButton;
        public Button VillageLevelUpButton => villageLevelUpButton;
        public Button WindowClose => windowClose;

        public void RefreshTrackLevels(int clickLevel, int typingLevel, int toolLevel)
        {
            SetText(clickCoinLevel, $"Lv.{clickLevel + 1}");
            SetText(typingCoinLevel, $"Lv.{typingLevel + 1}");
            SetText(toolProductLevel, $"Lv.{toolLevel + 1}");
        }

        public void RefreshTrackCosts(
            long clickCost,
            long typingCost,
            long toolCost,
            bool clickMax,
            bool typingMax,
            bool toolMax)
        {
            SetText(clickCoinLevelUpCost, clickMax ? "MAX" : clickCost.ToString("N0"));
            SetText(typingCoinLevelUpCost, typingMax ? "MAX" : typingCost.ToString("N0"));
            SetText(toolProductLevelUpCost, toolMax ? "MAX" : toolCost.ToString("N0"));
        }

        /// <summary>
        /// 각 _pan의 Value에 현재 상태 수치를 반영합니다.
        /// toolProductValue는 숫자 뒤에 %를 붙입니다. (예: 1.2 %)
        /// </summary>
        public void RefreshTrackValues(int clickValue, int typingValue, float toolProduct)
        {
            SetText(clickCoinValue, clickValue.ToString("N0"));
            SetText(typingCoinValue, typingValue.ToString("N0"));
            SetText(toolProductValue, toolProduct.ToString("0.#") + " %");
        }

        /// <summary>
        /// 이번 마을 레벨 사이클에서 완료된 트랙에 complete_img / checkImg(0~2)를 덮습니다.
        /// </summary>
        public void SetTrackComplete(bool clickDone, bool typingDone, bool toolDone)
        {
            SetActive(clickCompleteImg, clickDone);
            SetActive(typingCompleteImg, typingDone);
            SetActive(toolCompleteImg, toolDone);

            // CompleteImg와 동일 시점·동일 조건으로 checkImg 갱신
            SetCheckImg(0, clickDone);
            SetCheckImg(1, typingDone);
            SetCheckImg(2, toolDone);
        }

        /// <summary>
        /// checkImg[3] 보유 코인: 마을 레벨업 필요 코인 이상일 때 표시합니다.
        /// </summary>
        public void SetCoinCheckImg(bool coinReady)
        {
            SetCheckImg(3, coinReady);
        }

        public void RefreshRequiredUpgrade(int completedCount)
        {
            int clamped = Mathf.Clamp(completedCount, 0, RequiredUpgradeTotal);
            SetText(requiredUpgradeText, "필수 업그레이드 완료: " + clamped + " / " + RequiredUpgradeTotal);
        }

        /// <summary>
        /// 다음 레벨업(현재 Lv → Lv+1) 시 얻는 보상을 RewardTexts에 미리보기로 출력합니다.
        /// 더 이상 레벨업할 수 없으면 RewardTexts를 숨기고,
        /// 최종 레벨(10)이면 RewardEnd_txt를 표시합니다.
        /// 섬 모양 변경은 다음 레벨이 4/7/10일 때만 표시합니다.
        /// decoBuildingName은 VillageSystemManager.villageDecoBuilding에서 조회한 값입니다.
        /// </summary>
        public void RefreshReward(int currentTownLevel, bool canLevelUpFurther, string decoBuildingName)
        {
            if (!canLevelUpFurther)
            {
                SetRewardTextsVisible(false);
                // 최종 레벨 도달 + 출력할 보상 목록 없음 → RewardEnd_txt 표시
                SetRewardEndVisible(currentTownLevel >= 10);
                return;
            }

            SetRewardEndVisible(false);

            if (RewardTexts == null || RewardTexts.Length == 0)
                return;

            int nextLevel = Mathf.Max(1, currentTownLevel) + 1;
            int toolIncrease = Mathf.Max(0, GetToolCapacity(nextLevel) - GetToolCapacity(currentTownLevel));
            int animalIncrease = Mathf.Max(
                0,
                GetUnlockedPlacementSlotCount(nextLevel) - GetUnlockedPlacementSlotCount(currentTownLevel));
            bool showDecoBuilding = !string.IsNullOrWhiteSpace(decoBuildingName);
            bool showIslandChange = IsIslandChangeNextLevel(nextLevel);

            SetRewardText(0, $"도구 상한 증가 + {toolIncrease}", true);
            SetRewardText(1, $"동물 배치 상한 증가 + {animalIncrease}", true);
            SetRewardText(
                2,
                showDecoBuilding ? $"마을 장식 추가 : {decoBuildingName}" : string.Empty,
                showDecoBuilding);
            SetRewardText(3, "섬 모양 변경", showIslandChange);
        }

        private int GetToolCapacity(int townLevel)
        {
            float value = toolCapacityBase + toolCapacityAsymptoteRange
                * (1f - Mathf.Exp(-(Mathf.Max(1, townLevel) - 1) / toolCapacityDecayLevels));
            return Mathf.RoundToInt(value);
        }

        private int GetUnlockedPlacementSlotCount(int townLevel)
        {
            int level = Mathf.Max(1, townLevel);
            if (unlockedSlotsByTownLevel != null && unlockedSlotsByTownLevel.Length > 0)
            {
                int index = Mathf.Clamp(level - 1, 0, unlockedSlotsByTownLevel.Length - 1);
                return Mathf.Clamp(unlockedSlotsByTownLevel[index], 0, maxPlacementCapacity);
            }

            return 0;
        }

        private static bool IsIslandChangeNextLevel(int nextLevel)
        {
            return nextLevel == 4 || nextLevel == 7 || nextLevel == 10;
        }

        private void SetRewardText(int index, string value, bool visible)
        {
            if (RewardTexts == null || index < 0 || index >= RewardTexts.Length)
                return;

            TMP_Text target = RewardTexts[index];
            if (target == null)
                return;

            SetActive(target.gameObject, visible);
            if (visible)
                target.text = value;
        }

        private void SetRewardTextsVisible(bool visible)
        {
            if (RewardTexts == null)
                return;

            for (int i = 0; i < RewardTexts.Length; i++)
            {
                TMP_Text target = RewardTexts[i];
                if (target == null)
                    continue;

                SetActive(target.gameObject, visible);
            }
        }

        private void SetRewardEndVisible(bool visible)
        {
            if (RewardEnd_txt == null)
                return;

            SetActive(RewardEnd_txt.gameObject, visible);
        }

        private void SetCheckImg(int index, bool visible)
        {
            if (checkImg == null || index < 0 || index >= checkImg.Length)
                return;

            SetActive(checkImg[index], visible);
        }

        public void RefreshVillageLevel(int townLevel)
        {
            SetText(villageLevel, "Town Level : " + townLevel);
        }

        /// <summary>
        /// VillageLevelUp_btn 안 Text에 필요 코인을 출력합니다.
        /// </summary>
        public void RefreshVillageLevelUpCost(long cost)
        {
            SetText(villageLevelUpCost, cost.ToString("N0") + " 코인 보유");
        }

        /// <summary>
        /// 마을 레벨업 버튼 오브젝트를 항상 표시합니다.
        /// 조건 미완료/코인 부족은 SetVillageLevelUpInteractable로 클릭만 막습니다.
        /// </summary>
        public void SetVillageLevelUpVisible(bool visible)
        {
            if (villageLevelUpButton == null)
                return;

            // 하위 호환을 위해 시그니처는 유지하고, 오브젝트는 항상 켭니다.
            _ = visible;
            if (!villageLevelUpButton.gameObject.activeSelf)
                villageLevelUpButton.gameObject.SetActive(true);
        }

        public void SetTrackButtonsInteractable(bool clickEnabled, bool typingEnabled, bool toolEnabled)
        {
            SetButtonInteractable(clickCoinUpButton, clickEnabled);
            SetButtonInteractable(typingCoinUpButton, typingEnabled);
            SetButtonInteractable(toolProductUpButton, toolEnabled);
        }

        public void SetVillageLevelUpInteractable(bool enabled)
        {
            SetButtonInteractable(villageLevelUpButton, enabled);
        }

        private static void SetButtonInteractable(Button button, bool enabled)
        {
            if (button == null)
                return;

            button.interactable = enabled;
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target == null)
                return;

            if (target.activeSelf != active)
                target.SetActive(active);
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target == null)
                return;

            target.text = value;
        }
    }
}
