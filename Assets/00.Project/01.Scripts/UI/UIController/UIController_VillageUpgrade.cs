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
        private const int RequiredUpgradeTotal = 4;
        /// <summary>일반 모드 마을 레벨 상한 (레벨40 확장 철회 후 10으로 확정).</summary>
        private const int MaxTownLevel = 10;


        [Header("마을 패널의 갱신 항목")]
        [Tooltip("마을 레벨 텍스트")]
        [SerializeField] private TMP_Text villageLevel;
        [Tooltip("마을 레벨업 버튼")]
        [SerializeField] private Button villageLevelUpButton;
        
        [Header("레벨업 보상 미리보기")]
        [Tooltip("0:도구 상한 / 1:동물 배치 상한 / 2:마을 장식 / 3:섬 모양 변경")]
        [SerializeField] private TMP_Text[] RewardTexts;
        [Tooltip("10레벨 도달 후 보상없음 텍스트")]
        [SerializeField] private TMP_Text RewardEnd_txt;



        [Header("요구 패널의 갱신 항목")]
        [SerializeField] private TMP_Text requiredUpgradeText;
        [Tooltip("마을 레벨업에 필요한 코인")]
        [SerializeField] private TMP_Text villageLevelUpCost;
        [Tooltip("완료 이미지(요구칸) - 0:클릭 / 1:타이핑 / 2:도구 효율 / 3:보유 코인")]
        [SerializeField] private GameObject[] checkImg;
        [Tooltip("요구 문구 목록. 0=requiredUpgradeText. 최대 레벨/재건 완료 시 0만 유지합니다.")]
        [SerializeField] private TMP_Text[] RequireTexts;
        [SerializeField, TextArea(2, 4)]
        [Tooltip("마을 레벨업 필수 조건 requiredUpgradeText에 표시")]
        private string requireConditionMessage = "마을 레벨업 조건: ";
        [SerializeField, TextArea(2, 4)]
        [Tooltip("10레벨 도달(재건 전) 시 requiredUpgradeText에 비용 문구와 함께 표시")]
        private string finishUpGradeMessage = "마을 재건을 완료하기";
        [SerializeField, TextArea(2, 4)]
        [Tooltip("재건 완료/엔드리스 후 requiredUpgradeText에 표시")]
        private string endUpGradeMessage = "마을 레벨이 최대치에 도달했어요\n자유롭게 성장시켜보세요!";

        /// <summary>RequireTexts[0] — requiredUpgradeText 오브젝트.</summary>
        private const int RequireTextTitleIndex = 0;


        [Header("필수 레벨업 패널의 갱신되어야 하는 항목")]

        [Header("클릭 획득량 부분")]
        [SerializeField] private TMP_Text clickCoinLevel;
        [SerializeField] private TMP_Text clickCoinValue;
        [SerializeField] private Button clickCoinUpButton;

        [Header("타이핑 획득량 부분")]
        [SerializeField] private TMP_Text typingCoinLevel;
        [SerializeField] private TMP_Text typingCoinValue;
        [SerializeField] private Button typingCoinUpButton;

        [Header("도구 효율 생산보너스 부분")]
        [SerializeField] private TMP_Text toolProductLevel;
        [SerializeField] private TMP_Text toolProductValue;
        [SerializeField] private Button toolProductUpButton;

        [Tooltip("완료 이미지(업그레이드칸) - 0:클릭 / 1:타이핑 / 2:도구 효율")]
        [SerializeField] private Image[] completeImgs;
        [Tooltip("버튼 텍스트 - 0:클릭 / 1:타이핑 / 2:도구 효율.\n미완료시=비용, 완료시=completeTextMessage")]
        [SerializeField] private TMP_Text[] buttonText;
        [SerializeField] private string completeTextMessage = "업그레이드 완료";

        [Tooltip("최대 레벨 도달 후 출력할 마을 재건 버튼")]
        [SerializeField] private Button complteVillageEndButton;


        [Header("창 닫기 버튼")]
        [SerializeField] private Button windowClose;

        [Header("보상 수치 (시스템 값과 동기화 유지)")]
        [Tooltip("InventoryManager_Tool.toolCapacityPerLevel와 동일")]
        [SerializeField] private int toolCapacityPerLevel = 2;
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
        public Button ComplteVillageEndButton => complteVillageEndButton;
        public Button WindowClose => windowClose;

        public void RefreshTrackLevels(int clickLevel, int typingLevel, int toolLevel)
        {
            SetText(clickCoinLevel, $"Lv.{clickLevel + 1}");
            SetText(typingCoinLevel, $"Lv.{typingLevel + 1}");
            SetText(toolProductLevel, $"Lv.{toolLevel + 1}");
        }

        /// <summary>
        /// 미완료 트랙의 buttonText에 업그레이드 필요 코인(또는 MAX)을 표시합니다.
        /// 완료 트랙 문구는 이후 SetTrackComplete에서 completeTextMessage로 덮어씁니다.
        /// </summary>
        public void RefreshTrackCosts(
            long clickCost,
            long typingCost,
            long toolCost,
            bool clickMax,
            bool typingMax,
            bool toolMax)
        {
            SetButtonText(0, clickMax ? "MAX" : clickCost.ToString("N0"));
            SetButtonText(1, typingMax ? "MAX" : typingCost.ToString("N0"));
            SetButtonText(2, toolMax ? "MAX" : toolCost.ToString("N0"));
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
        /// 이번 마을 레벨 사이클에서 완료된 트랙에 completeImgs / checkImg(0~2)를 덮습니다.
        /// 완료된 트랙의 buttonText는 completeTextMessage로 갱신하고, 업그레이드 버튼은 비활성화합니다.
        /// </summary>
        public void SetTrackComplete(bool clickDone, bool typingDone, bool toolDone)
        {
            SetCompleteImg(0, clickDone);
            SetCompleteImg(1, typingDone);
            SetCompleteImg(2, toolDone);

            // 완료 트랙만 completeTextMessage로 덮어씀 (미완료는 RefreshTrackCosts 비용 유지)
            if (clickDone)
                SetButtonText(0, completeTextMessage);
            if (typingDone)
                SetButtonText(1, completeTextMessage);
            if (toolDone)
                SetButtonText(2, completeTextMessage);

            SetCheckImg(0, clickDone);
            SetCheckImg(1, typingDone);
            SetCheckImg(2, toolDone);

            // 업그레이드 완료 시 버튼 오브젝트 자체 비활성 (미완료면 다시 표시)
            SetButtonObjectActive(clickCoinUpButton, !clickDone);
            SetButtonObjectActive(typingCoinUpButton, !typingDone);
            SetButtonObjectActive(toolProductUpButton, !toolDone);
        }

        /// <summary>
        /// checkImg[3] 보유 코인: 마을 레벨업 필요 코인 이상일 때 표시합니다.
        /// </summary>
        public void SetCoinCheckImg(bool coinReady)
        {
            SetCheckImg(3, coinReady);
        }

        //------------------26.08.06 KAY 수정 (requiredUpgradeText 비용+문구 통합)-----------------
        /// <summary>
        /// 요구 패널 갱신:
        /// - 일반: "필수 조건: n / 4" (클릭/타이핑/생산 + 보유 코인), RequireTexts 전부 표시
        /// - 최대 레벨(재건 전): "{completionCost} 을 사용하여\n" + finishUpGradeMessage, [0]만 활성
        /// - 재건 완료/엔드리스: endUpGradeMessage, [0]만 활성
        /// villageLevelUpCost는 별도 출력하지 않습니다(비활성 유지).
        /// completedCount는 호출부에서 트랙 완료 + 코인 충족을 합산해 전달합니다.
        /// </summary>
        public void RefreshRequiredUpgrade(
            int completedCount,
            bool isMaxLevel = false,
            bool isEndless = false,
            long completionCost = 0L,
            bool reconstructionCompleted = false)
        {
            // 재건 Yes 또는 엔드리스: endUpGradeMessage만 표시
            if (isEndless || reconstructionCompleted)
            {
                SetRequiredUpgradeTitle(endUpGradeMessage);
                SetRequireTextsVisibleKeeping(RequireTextTitleIndex);
                return;
            }

            if (isMaxLevel)
            {
                SetRequiredUpgradeTitle(completionCost + " 을 사용하여\n" + finishUpGradeMessage);
                SetRequireTextsVisibleKeeping(RequireTextTitleIndex);
                return;
            }

            int clamped = Mathf.Clamp(completedCount, 0, RequiredUpgradeTotal);
            SetRequiredUpgradeTitle(requireConditionMessage + clamped + " / " + RequiredUpgradeTotal);
            SetAllRequireTextsVisible(true);
        }

        /// <summary>
        /// requiredUpgradeText 필드와 RequireTexts[0]에 동일한 제목 문구를 반영합니다.
        /// </summary>
        private void SetRequiredUpgradeTitle(string message)
        {
            SetText(requiredUpgradeText, message);
            SetRequireText(RequireTextTitleIndex, message);
        }

        /// <summary>RequireTexts 전부 활성/비활성.</summary>
        private void SetAllRequireTextsVisible(bool visible)
        {
            if (RequireTexts == null)
                return;

            for (int i = 0; i < RequireTexts.Length; i++)
            {
                TMP_Text target = RequireTexts[i];
                if (target == null)
                    continue;

                SetActive(target.gameObject, visible);
            }
        }

        /// <summary>
        /// keepIndices에 포함된 인덱스만 활성, 나머지는 비활성합니다.
        /// </summary>
        private void SetRequireTextsVisibleKeeping(params int[] keepIndices)
        {
            if (RequireTexts == null)
                return;

            for (int i = 0; i < RequireTexts.Length; i++)
            {
                TMP_Text target = RequireTexts[i];
                if (target == null)
                    continue;

                bool keep = false;
                if (keepIndices != null)
                {
                    for (int k = 0; k < keepIndices.Length; k++)
                    {
                        if (keepIndices[k] == i)
                        {
                            keep = true;
                            break;
                        }
                    }
                }

                SetActive(target.gameObject, keep);
            }
        }

        private void SetRequireText(int index, string value)
        {
            if (RequireTexts == null || index < 0 || index >= RequireTexts.Length)
                return;

            TMP_Text target = RequireTexts[index];
            if (target == null)
                return;

            SetActive(target.gameObject, true);
            target.text = value ?? string.Empty;
        }
        //-----------------------------------------------------------------------------

        /// <summary>
        /// 다음 레벨업(현재 Lv → Lv+1) 시 얻는 보상을 RewardTexts에 미리보기로 출력합니다.
        /// 더 이상 레벨업할 수 없으면 RewardTexts를 숨기고,
        /// 최종 레벨(10)이면 RewardEnd_txt를 표시합니다.
        /// 섬 모양 변경은 다음 레벨이 5/10일 때만 표시합니다.
        /// decoBuildingName은 VillageSystemManager.villageDecoBuilding에서 조회한 값입니다.
        /// </summary>
        public void RefreshReward(int currentTownLevel, bool canLevelUpFurther, string decoBuildingName)
        {
            if (!canLevelUpFurther)
            {
                SetRewardTextsVisible(false);
                // 최종 레벨(10) 도달 + 출력할 보상 목록 없음 → RewardEnd_txt 표시
                SetRewardEndVisible(currentTownLevel >= MaxTownLevel);
                return;
            }

            SetRewardEndVisible(false);

            if (RewardTexts == null || RewardTexts.Length == 0)
                return;

            int nextLevel = Mathf.Max(1, currentTownLevel) + 1;
            int toolIncrease = Mathf.Max(0, toolCapacityPerLevel);
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
            return nextLevel == 5 || nextLevel == 10;
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

        private void SetCompleteImg(int index, bool visible)
        {
            if (completeImgs == null || index < 0 || index >= completeImgs.Length)
                return;

            Image target = completeImgs[index];
            if (target == null)
                return;

            SetActive(target.gameObject, visible);
        }

        /// <summary>
        /// buttonText[index]에 비용 또는 완료 문구를 넣습니다. (0:클릭 / 1:타이핑 / 2:도구)
        /// </summary>
        private void SetButtonText(int index, string value)
        {
            if (buttonText == null || index < 0 || index >= buttonText.Length)
                return;

            TMP_Text target = buttonText[index];
            if (target == null)
                return;

            SetActive(target.gameObject, true);
            target.text = value ?? string.Empty;
        }

        public void RefreshVillageLevel(int townLevel)
        {
            SetText(villageLevel, "Town Level : " + townLevel);
        }

        /// <summary>
        /// VillageLevelUp_btn 안 Text에 필요 코인을 출력합니다.
        /// </summary>
        /// <summary>
        /// 마을 레벨업(또는 재건)에 필요한 코인 수치를 표시합니다.
        /// 보유 코인 충족 여부는 checkImg로 별도 표시합니다.
        /// </summary>
        public void RefreshVillageLevelUpCost(long cost)
        {
            SetVillageLevelUpCostVisible(true);
            SetText(villageLevelUpCost, cost.ToString("N0") + " 코인 보유");
        }

        //------------------26.08.06 KAY (villageLevelUpCost 표시 토글)-----------------------------
        /// <summary>
        /// villageLevelUpCost 오브젝트 활성/비활성.
        /// </summary>
        public void SetVillageLevelUpCostVisible(bool visible)
        {
            if (villageLevelUpCost == null)
                return;

            SetActive(villageLevelUpCost.gameObject, visible);
        }
        //-----------------------------------------------------------------------------

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

        //------------------26.08.05 KAY 추가 / 26.08.06 수정 (최대 레벨 엔드 버튼)---------------
        /// <summary>
        /// 마을 액션 버튼 표시:
        /// - isEndless: 레벨업/엔드 버튼 모두 Active(false)
        /// - isMaxLevel: 엔드 버튼만 표시
        /// - 그 외: 레벨업 버튼만 표시
        /// </summary>
        public void SetMaxLevelEndButtons(bool isMaxLevel, bool isEndless = false)
        {
            if (isEndless)
            {
                SetButtonObjectActive(villageLevelUpButton, false);
                SetButtonObjectActive(complteVillageEndButton, false);
                return;
            }

            bool showEndButton = isMaxLevel;
            SetButtonObjectActive(villageLevelUpButton, !showEndButton);
            SetButtonObjectActive(complteVillageEndButton, showEndButton);
        }

        public void SetCompleteVillageEndInteractable(bool enabled)
        {
            SetButtonInteractable(complteVillageEndButton, enabled);
        }
        //-----------------------------------------------------------------------------

        private static void SetButtonInteractable(Button button, bool enabled)
        {
            if (button == null)
                return;

            button.interactable = enabled;
        }

        private static void SetButtonObjectActive(Button button, bool active)
        {
            if (button == null)
                return;

            SetActive(button.gameObject, active);
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
