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
        [Header("레벨 텍스트")]
        [SerializeField] private TMP_Text clickCoinLevel;
        [SerializeField] private TMP_Text typingCoinLevel;
        [SerializeField] private TMP_Text toolProductLevel;

        [Header("레벨업 코인 가격")]
        [SerializeField] private TMP_Text clickCoinLevelUpCost;
        [SerializeField] private TMP_Text typingCoinLevelUpCost;
        [SerializeField] private TMP_Text toolProductLevelUpCost;

        [Header("3종 업그레이드 버튼")]
        [SerializeField] private Button clickCoinUpButton;
        [SerializeField] private Button typingCoinUpButton;
        [SerializeField] private Button toolProductUpButton;

        [Header("3종의 현재 수치")]
        [SerializeField] private TMP_Text clickCoinValue;
        [SerializeField] private TMP_Text typingCoinValue;
        [SerializeField] private TMP_Text toolProductValue;

        [Header("완료 덮개 (complete_img1~3)")]
        [Tooltip("clickcoin_pan 하위 complete_img1")]
        [SerializeField] private GameObject clickCompleteImg;
        [Tooltip("typingcoin_pan 하위 complete_img2")]
        [SerializeField] private GameObject typingCompleteImg;
        [Tooltip("toolprocoin_pan 하위 complete_img3")]
        [SerializeField] private GameObject toolCompleteImg;

        [Header("Required Upgrade 텍스트")]
        [SerializeField] private TMP_Text requiredUpgradeText;

        [Header("마을 레벨업 패널의 갱신되어야 하는 항목")]
        [Header("마을 레벨 텍스트")]
        [SerializeField] private TMP_Text villageLevel;

        [Header("마을 레벨업 코인 가격")]
        [Header("마을 레벨업 버튼 / 비용 텍스트")]
        [SerializeField] private Button villageLevelUpButton;
        [Tooltip("VillageLevelUp_btn 자식 Text")]
        [SerializeField] private TMP_Text villageLevelUpCost;

        public Button ClickCoinUpButton => clickCoinUpButton;
        public Button TypingCoinUpButton => typingCoinUpButton;
        public Button ToolProductUpButton => toolProductUpButton;
        public Button VillageLevelUpButton => villageLevelUpButton;

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
        /// 이번 마을 레벨 사이클에서 완료된 트랙에 complete_img를 덮습니다.
        /// </summary>
        public void SetTrackComplete(bool clickDone, bool typingDone, bool toolDone)
        {
            SetActive(clickCompleteImg, clickDone);
            SetActive(typingCompleteImg, typingDone);
            SetActive(toolCompleteImg, toolDone);
        }

        public void RefreshRequiredUpgrade(int completedCount)
        {
            int clamped = Mathf.Clamp(completedCount, 0, RequiredUpgradeTotal);
            SetText(requiredUpgradeText, "Required Upgrade\n" + clamped + " / " + RequiredUpgradeTotal);
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
            SetText(villageLevelUpCost, "VillageLevelUp\n" + cost.ToString("N0") + " coin");
        }

        public void SetVillageLevelUpVisible(bool visible)
        {
            if (villageLevelUpButton == null)
                return;

            villageLevelUpButton.gameObject.SetActive(visible);
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
