using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace TaskTown.Gacha.Demo
{
    // 유니티 Play 모드에서 버튼으로 실제 뽑기를 눌러볼 수 있게 해주는 테스트용 UI입니다.
    // 실제 서비스용 UI(김아영 담당)가 만들어지면 이 스크립트는 참고용/테스트용으로만 남습니다.
    public class GachaDemoUI : MonoBehaviour
    {
        [Header("Gacha")]
        [SerializeField] private AnimalGachaManager animalGachaManager;
        [SerializeField] private ToolGachaManager toolGachaManager;
        [SerializeField] private DemoTownLevelProvider townLevelProvider;

        [Tooltip("마을 레벨업 비용 테이블입니다. 비워두면 무료로 레벨업됩니다.")]
        [SerializeField] private TownUpgradeCostConfig townUpgradeCostConfig;

        [Tooltip("ICoinWallet를 구현한 컴포넌트를 연결합니다(예: CoinManager). 비워두면 코스트 차감 없이 뽑기를 진행합니다.")]
        [SerializeField] private MonoBehaviour coinWalletSource;

        private ICoinWallet coinWallet;

        [Header("Buttons")]
        [SerializeField] private Button animalGachaButton;
        [SerializeField] private Button toolGachaButton;
        [SerializeField] private Button animalGachaX10Button;
        [SerializeField] private Button toolGachaX10Button;
        [SerializeField] private Button increaseTownLevelButton;
        [SerializeField] private Button addTestCoinButton;

        [Header("Texts")]
        [SerializeField] private Text resultText;
        [SerializeField] private Text costText;
        [SerializeField] private Text townLevelText;

        private const int MultiRollCount = 10;

        private void Awake()
        {
            if (animalGachaButton != null) animalGachaButton.onClick.AddListener(RollAnimal);
            if (toolGachaButton != null) toolGachaButton.onClick.AddListener(RollTool);
            if (animalGachaX10Button != null) animalGachaX10Button.onClick.AddListener(RollAnimalMulti);
            if (toolGachaX10Button != null) toolGachaX10Button.onClick.AddListener(RollToolMulti);
            if (increaseTownLevelButton != null) increaseTownLevelButton.onClick.AddListener(IncreaseTownLevel);
            if (addTestCoinButton != null) addTestCoinButton.onClick.AddListener(AddTestCoin);

            coinWallet = coinWalletSource as ICoinWallet;

            if (animalGachaManager != null) animalGachaManager.OnGachaResolved += HandleAnimalResult;
            if (toolGachaManager != null) toolGachaManager.OnGachaResolved += HandleToolResult;
        }

        private void Start()
        {
            RefreshInfoTexts();
        }

        private void OnDestroy()
        {
            if (animalGachaManager != null) animalGachaManager.OnGachaResolved -= HandleAnimalResult;
            if (toolGachaManager != null) toolGachaManager.OnGachaResolved -= HandleToolResult;
        }

        private void RollAnimal()
        {
            if (animalGachaManager == null) return;
            if (!TrySpendCost(animalGachaManager.CurrentCost)) return;
            animalGachaManager.Roll();
            RefreshInfoTexts();
        }

        private void RollTool()
        {
            if (toolGachaManager == null) return;
            if (!TrySpendCost(toolGachaManager.CurrentCost)) return;
            toolGachaManager.Roll();
            RefreshInfoTexts();
        }

        private void RollAnimalMulti()
        {
            if (animalGachaManager == null) return;
            if (!TrySpendCost(animalGachaManager.GetCost(MultiRollCount))) return;
            List<GachaResult> results = animalGachaManager.RollMulti(MultiRollCount);
            ShowMultiResult("Animal Gacha", results);
            RefreshInfoTexts();
        }

        private void RollToolMulti()
        {
            if (toolGachaManager == null) return;
            if (!TrySpendCost(toolGachaManager.GetCost(MultiRollCount))) return;
            List<GachaResult> results = toolGachaManager.RollMulti(MultiRollCount);
            ShowMultiResult("Tool Gacha", results);
            RefreshInfoTexts();
        }

        // 임시 재화 소비 처리입니다. 정식 재화 차감/저장 흐름은 UI·저장 담당(김아영)이 별도로 구현합니다.
        private bool TrySpendCost(long cost)
        {
            if (coinWallet == null)
            {
                Debug.LogWarning("ICoinWallet가 연결되지 않아 코스트 차감 없이 뽑기를 진행합니다.");
                return true;
            }

            if (!coinWallet.TrySpend(cost))
            {
                if (resultText != null)
                {
                    resultText.text = $"코인이 부족합니다. (필요: {cost}, 보유: {coinWallet.Balance})";
                }

                return false;
            }

            return true;
        }

        // 마을 업그레이드는 돈으로 하는 소비처이므로, 레벨업 전에 코스트를 먼저 확인합니다.
        private void IncreaseTownLevel()
        {
            if (townLevelProvider == null) return;

            long cost = townUpgradeCostConfig != null ? townUpgradeCostConfig.GetCostForTownLevel(townLevelProvider.CurrentTownLevel) : 0;
            if (!TrySpendCost(cost)) return;

            townLevelProvider.IncreaseLevel();
            RefreshInfoTexts();
        }

        // 테스트 편의용 임시 코인 지급 버튼입니다. 실제 획득 흐름(클릭/타이핑)이 붙기 전까지 뽑기 테스트 용도로 사용합니다.
        private const int TestCoinGrant = 1000;

        private void AddTestCoin()
        {
            if (coinWallet == null) return;
            coinWallet.Add(TestCoinGrant);
            RefreshInfoTexts();
        }

        private void HandleAnimalResult(GachaResult result)
        {
            ShowResult("Animal Gacha", result);
        }

        private void HandleToolResult(GachaResult result)
        {
            ShowResult("Tool Gacha", result);
        }

        private void ShowResult(string gachaName, GachaResult result)
        {
            if (resultText == null) return;

            string entryName = result.Entry != null ? result.Entry.DisplayName : "(No entry registered for this grade)";
            resultText.text = $"{gachaName} Result : [{result.Grade}] {entryName}";
        }

        private void ShowMultiResult(string gachaName, List<GachaResult> results)
        {
            if (resultText == null) return;

            StringBuilder builder = new StringBuilder();
            builder.Append(gachaName).Append(" x").Append(results.Count).Append(" Result : ");
            for (int i = 0; i < results.Count; i++)
            {
                if (i > 0) builder.Append(", ");
                string entryName = results[i].Entry != null ? results[i].Entry.DisplayName : "(No entry)";
                builder.Append('[').Append(results[i].Grade).Append("] ").Append(entryName);
            }

            resultText.text = builder.ToString();
        }

        private void RefreshInfoTexts()
        {
            if (costText != null)
            {
                long animalCost = animalGachaManager != null ? animalGachaManager.CurrentCost : 0;
                long toolCost = toolGachaManager != null ? toolGachaManager.CurrentCost : 0;
                long animalCostX10 = animalGachaManager != null ? animalGachaManager.GetCost(MultiRollCount) : 0;
                long toolCostX10 = toolGachaManager != null ? toolGachaManager.GetCost(MultiRollCount) : 0;
                string coinLine = coinWallet != null ? $"Coin : {coinWallet.Balance}   /   " : string.Empty;
                costText.text = $"{coinLine}Animal Gacha Cost : {animalCost} (x10: {animalCostX10})   /   Tool Gacha Cost : {toolCost} (x10: {toolCostX10})";
            }

            if (townLevelText != null && townLevelProvider != null)
            {
                long upgradeCost = townUpgradeCostConfig != null ? townUpgradeCostConfig.GetCostForTownLevel(townLevelProvider.CurrentTownLevel) : 0;
                townLevelText.text = $"Current Town Level : {townLevelProvider.CurrentTownLevel}   (Upgrade Cost : {upgradeCost})";
            }
        }
    }
}
