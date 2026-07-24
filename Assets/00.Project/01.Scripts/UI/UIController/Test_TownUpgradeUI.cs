using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    // 이슈 #72(클릭/타이핑/도구효율 업그레이드) 기능을 눈으로 확인하기 위한 테스트 전용 UI입니다.
    // 기존 UIController_VillageInfo/VillageInfoUI_Manager 등 팀원 작업 중인 UI 파일은 건드리지 않고,
    // 별도 패널로 TownUpgradeManager를 직접 호출합니다. 실제 플레이어용 UI가 아닙니다.
    public class Test_TownUpgradeUI : MonoBehaviour
    {
        [Header("버튼")]
        [SerializeField] private Button buyClickButton;
        [SerializeField] private Button buyTypingButton;
        [SerializeField] private Button buyToolEffButton;
        [Tooltip("3개 업그레이드 레벨만 0으로 되돌립니다(코인/인벤토리는 그대로).")]
        [SerializeField] private Button resetButton;

        [Header("텍스트")]
        [SerializeField] private TMP_Text clickInfoText;
        [SerializeField] private TMP_Text typingInfoText;
        [SerializeField] private TMP_Text toolEffInfoText;
        [SerializeField] private TMP_Text statusText;

        private void Awake()
        {
            if (buyClickButton != null) buyClickButton.onClick.AddListener(OnBuyClick);
            if (buyTypingButton != null) buyTypingButton.onClick.AddListener(OnBuyTyping);
            if (buyToolEffButton != null) buyToolEffButton.onClick.AddListener(OnBuyToolEff);
            if (resetButton != null) resetButton.onClick.AddListener(OnResetLevels);
        }

        private void OnDestroy()
        {
            if (buyClickButton != null) buyClickButton.onClick.RemoveListener(OnBuyClick);
            if (buyTypingButton != null) buyTypingButton.onClick.RemoveListener(OnBuyTyping);
            if (buyToolEffButton != null) buyToolEffButton.onClick.RemoveListener(OnBuyToolEff);
            if (resetButton != null) resetButton.onClick.RemoveListener(OnResetLevels);
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        private void OnBuyClick()
        {
            if (TownUpgradeManager.Instance == null) return;
            TownUpgradeManager.Instance.TryUpgradeClick();
        }

        private void OnBuyTyping()
        {
            if (TownUpgradeManager.Instance == null) return;
            TownUpgradeManager.Instance.TryUpgradeTyping();
        }

        private void OnBuyToolEff()
        {
            if (TownUpgradeManager.Instance == null) return;
            TownUpgradeManager.Instance.TryUpgradeToolEfficiency();
        }

        // 코인/인벤토리는 그대로 두고 업그레이드 3종 레벨만 0으로 되돌립니다(반복 테스트용).
        private void OnResetLevels()
        {
            if (TownUpgradeManager.Instance == null) return;
            TownUpgradeManager.Instance.LoadLevels(0, 0, 0);
        }

        private void Refresh()
        {
            // TMP에 한글 폰트(SDF)가 아직 없어서(프로젝트 전반의 알려진 제약) 테스트 UI 텍스트는
            // 영어로 표시합니다.
            TownUpgradeManager tum = TownUpgradeManager.Instance;
            if (tum == null)
            {
                SetText(statusText, "TownUpgradeManager not found");
                return;
            }

            SetText(clickInfoText,
                $"Click Coin Lv.{tum.ClickLevel}\nNext cost: {(tum.IsClickUpgradeMaxLevel ? "MAX" : tum.ClickUpgradeNextCost.ToString("N0"))}");

            SetText(typingInfoText,
                $"Typing Coin Lv.{tum.TypingLevel}\nNext cost: {(tum.IsTypingUpgradeMaxLevel ? "MAX" : tum.TypingUpgradeNextCost.ToString("N0"))}");

            SetText(toolEffInfoText,
                $"Tool Efficiency Lv.{tum.ToolEfficiencyLevel}\nNext cost: {(tum.IsToolEfficiencyUpgradeMaxLevel ? "MAX" : tum.ToolEfficiencyUpgradeNextCost.ToString("N0"))}");

            long coin = CoinManager.Instance != null ? CoinManager.Instance.totalCoin : 0;
            int clickMult = EarnProcessor.Instance != null ? EarnProcessor.Instance.ClickMultiplier : 1;
            int typingMult = EarnProcessor.Instance != null ? EarnProcessor.Instance.TypingMultiplier : 1;
            int maxPerHour = EarnProcessor.Instance != null ? EarnProcessor.Instance.MaxCoinPerHour : 0;

            SetText(statusText,
                $"Coins: {coin:N0}\nClick x{clickMult}   Typing x{typingMult}\n" +
                $"Tool Efficiency x{tum.ToolEfficiencyMultiplier:0.00}\nHourly cap: {maxPerHour:N0}/h");
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target == null) return;
            target.text = value;
        }
    }
}
