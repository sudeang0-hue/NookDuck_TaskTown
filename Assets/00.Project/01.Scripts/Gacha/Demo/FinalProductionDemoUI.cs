using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TaskTown.Gacha.Demo
{
    // 처음에는 동물/도구가 없는 상태에서 시작해서, 가챠(AnimalGachaManager/ToolGachaManager)로
    // 동물이나 도구 중 하나만 얻어도 그 자체 생산량만으로 초당 코인이 생산되기 시작하고,
    // 둘 다 얻으면 합산(및 특화 보너스)이 반영되는지 확인하는 테스트 전용 데모입니다.
    // 배치 UI가 아직 없으므로, 뽑은 동물/도구 중 가장 최근 것을 "장착 중"으로 보고 생산량을 계산하되,
    // 각 동물/도구별 레벨과 중복 개수는 ID별로 계속 누적 보관합니다(다른 걸 뽑아도 이전 기록이 사라지지 않음).
    public class FinalProductionDemoUI : MonoBehaviour
    {
        [SerializeField] private AnimalGachaManager animalGachaManager;
        [SerializeField] private ToolGachaManager toolGachaManager;
        [SerializeField] private DifficultyProductionTable difficultyTable;

        [Tooltip("GachaDemoUI와 같은 DemoTownLevelProvider를 연결하면, 실제 마을 레벨업 결과가 생산 효율 버프에 반영됩니다.")]
        [SerializeField] private DemoTownLevelProvider townLevelProvider;

        [Tooltip("마을 레벨당 전체 생산량에 곱해지는 효율 버프입니다.")]
        [SerializeField] private TownUpgradeEffectConfig townUpgradeEffectConfig = new TownUpgradeEffectConfig();

        [Tooltip("ICoinWallet를 구현한 컴포넌트를 연결합니다(예: CoinManager). 비워두면 코인 누적 없이 수치만 표시합니다.")]
        [SerializeField] private MonoBehaviour coinWalletSource;

        [Header("Buttons")]
        [SerializeField] private Button resetOwnedButton;
        [SerializeField] private Button nextDifficultyButton;
        [SerializeField] private Button animalLevelUpButton;
        [SerializeField] private Button toolLevelUpButton;

        [Header("Texts")]
        [SerializeField] private Text statusText;

        // 동물/도구 ID별 레벨, 중복 보유 개수를 기억합니다. 다른 동물/도구를 뽑아도 여기 기록은 지워지지 않습니다.
        private class OwnedProgress
        {
            public int level = 1;
            public int duplicateCount = 1;
        }

        private ICoinWallet coinWallet;

        private readonly Dictionary<string, AnimalData> animalsById = new Dictionary<string, AnimalData>();
        private readonly Dictionary<string, OwnedProgress> animalProgressById = new Dictionary<string, OwnedProgress>();
        private string activeAnimalId;

        private readonly Dictionary<string, ToolData> toolsById = new Dictionary<string, ToolData>();
        private readonly Dictionary<string, OwnedProgress> toolProgressById = new Dictionary<string, OwnedProgress>();
        private string activeToolId;

        private DifficultyType difficulty = DifficultyType.Normal;
        private string levelUpFailureMessage;

        // 초당 생산량의 소수점 이하를 보관하다가 1 이상 쌓이면 정수만큼 코인으로 반영합니다.
        private float productionBuffer;

        // 마을 레벨업(GachaDemoUI 쪽 버튼)이 실제로 반영되도록, 매번 townLevelProvider의 현재 레벨을 기준으로 계산합니다.
        private float TownUpgradeMultiplier => townUpgradeEffectConfig.GetProductionMultiplier(townLevelProvider != null ? townLevelProvider.CurrentTownLevel : 1);

        private AnimalData ActiveAnimal => activeAnimalId != null && animalsById.TryGetValue(activeAnimalId, out AnimalData animal) ? animal : null;
        private ToolData ActiveTool => activeToolId != null && toolsById.TryGetValue(activeToolId, out ToolData tool) ? tool : null;
        private OwnedProgress ActiveAnimalProgress => activeAnimalId != null ? animalProgressById[activeAnimalId] : null;
        private OwnedProgress ActiveToolProgress => activeToolId != null ? toolProgressById[activeToolId] : null;

        private void Awake()
        {
            if (resetOwnedButton != null) resetOwnedButton.onClick.AddListener(ResetOwned);
            if (nextDifficultyButton != null) nextDifficultyButton.onClick.AddListener(NextDifficulty);
            if (animalLevelUpButton != null) animalLevelUpButton.onClick.AddListener(AnimalLevelUp);
            if (toolLevelUpButton != null) toolLevelUpButton.onClick.AddListener(ToolLevelUp);

            coinWallet = coinWalletSource as ICoinWallet;

            if (animalGachaManager != null) animalGachaManager.OnGachaResolved += HandleAnimalGachaResolved;
            if (toolGachaManager != null) toolGachaManager.OnGachaResolved += HandleToolGachaResolved;
        }

        private void OnDestroy()
        {
            if (animalGachaManager != null) animalGachaManager.OnGachaResolved -= HandleAnimalGachaResolved;
            if (toolGachaManager != null) toolGachaManager.OnGachaResolved -= HandleToolGachaResolved;
        }

        private void Start()
        {
            RefreshStatusText();
        }

        private void Update()
        {
            AnimalData animal = ActiveAnimal;
            ToolData tool = ActiveTool;
            if (animal == null && tool == null) return;

            float coinPerSecond = FinalProductionCalculator.CalculateCoinPerSecond(
                animal, tool, ActiveAnimalProgress?.level ?? 1, ActiveToolProgress?.level ?? 1, difficulty, difficultyTable, TownUpgradeMultiplier);

            productionBuffer += coinPerSecond * Time.deltaTime;
            if (productionBuffer >= 1f && coinWallet != null)
            {
                int wholeCoins = Mathf.FloorToInt(productionBuffer);
                productionBuffer -= wholeCoins;
                coinWallet.Add(wholeCoins);
            }

            RefreshStatusText();
        }

        // 이미 보유한 동물/도구면 중복 개수만 늘리고, 처음 보는 동물/도구면 새로 등록합니다.
        // 어느 쪽이든 "장착 중" 대상은 방금 뽑은 것으로 바뀌지만, 기존 기록은 지워지지 않습니다.
        private void HandleAnimalGachaResolved(GachaResult result)
        {
            AnimalData rolled = result.Entry as AnimalData;
            if (rolled == null) return;

            if (animalProgressById.TryGetValue(rolled.Id, out OwnedProgress progress))
            {
                progress.duplicateCount++;
            }
            else
            {
                animalsById[rolled.Id] = rolled;
                animalProgressById[rolled.Id] = new OwnedProgress();
            }

            activeAnimalId = rolled.Id;
            RefreshStatusText();
        }

        private void HandleToolGachaResolved(GachaResult result)
        {
            ToolData rolled = result.Entry as ToolData;
            if (rolled == null) return;

            if (toolProgressById.TryGetValue(rolled.Id, out OwnedProgress progress))
            {
                progress.duplicateCount++;
            }
            else
            {
                toolsById[rolled.Id] = rolled;
                toolProgressById[rolled.Id] = new OwnedProgress();
            }

            activeToolId = rolled.Id;
            RefreshStatusText();
        }

        // 테스트 편의용: 지금까지 모은 모든 동물/도구 기록을 지우고 다시 "아무것도 없는" 초기 상태로 되돌립니다.
        private void ResetOwned()
        {
            animalsById.Clear();
            animalProgressById.Clear();
            activeAnimalId = null;

            toolsById.Clear();
            toolProgressById.Clear();
            activeToolId = null;

            productionBuffer = 0f;
            levelUpFailureMessage = null;
            RefreshStatusText();
        }

        private void NextDifficulty()
        {
            int valueCount = System.Enum.GetValues(typeof(DifficultyType)).Length;
            difficulty = (DifficultyType)(((int)difficulty + 1) % valueCount);
            RefreshStatusText();
        }

        // 중복 개수(4^레벨)와 코인 비용을 모두 충족해야 레벨업됩니다. 장착 중인(가장 최근에 뽑은) 동물/도구 기준입니다.
        private void AnimalLevelUp()
        {
            AnimalData animal = ActiveAnimal;
            OwnedProgress progress = ActiveAnimalProgress;
            if (animal == null || progress == null) return;

            int required = LevelUpRequirementCalculator.GetRequiredDuplicateCount(progress.level);
            if (progress.duplicateCount < required)
            {
                ShowLevelUpFailure($"동물 레벨업 실패: 중복 {progress.duplicateCount}/{required}개 필요");
                return;
            }

            long cost = animal.CalculateLevelUpCoinCost(progress.level);
            if (coinWallet == null || !coinWallet.TrySpend(cost))
            {
                ShowLevelUpFailure($"동물 레벨업 실패: 코인 부족 (필요 {cost})");
                return;
            }

            progress.duplicateCount -= required;
            progress.level++;
            levelUpFailureMessage = null;
            RefreshStatusText();
        }

        private void ToolLevelUp()
        {
            ToolData tool = ActiveTool;
            OwnedProgress progress = ActiveToolProgress;
            if (tool == null || progress == null) return;

            int required = LevelUpRequirementCalculator.GetRequiredDuplicateCount(progress.level);
            if (progress.duplicateCount < required)
            {
                ShowLevelUpFailure($"도구 레벨업 실패: 중복 {progress.duplicateCount}/{required}개 필요");
                return;
            }

            long cost = tool.CalculateLevelUpCoinCost(progress.level);
            if (coinWallet == null || !coinWallet.TrySpend(cost))
            {
                ShowLevelUpFailure($"도구 레벨업 실패: 코인 부족 (필요 {cost})");
                return;
            }

            progress.duplicateCount -= required;
            progress.level++;
            levelUpFailureMessage = null;
            RefreshStatusText();
        }

        private void ShowLevelUpFailure(string message)
        {
            levelUpFailureMessage = message;
            RefreshStatusText();
        }

        private void RefreshStatusText()
        {
            if (statusText == null) return;

            AnimalData animal = ActiveAnimal;
            ToolData tool = ActiveTool;
            OwnedProgress animalProgress = ActiveAnimalProgress;
            OwnedProgress toolProgress = ActiveToolProgress;

            if (animal == null && tool == null)
            {
                statusText.text = "Animal: 없음   Tool: 없음\n동물 또는 도구를 뽑으면 생산이 시작됩니다.";
                return;
            }

            float coinPerSecond = FinalProductionCalculator.CalculateCoinPerSecond(
                animal, tool, animalProgress?.level ?? 1, toolProgress?.level ?? 1, difficulty, difficultyTable, TownUpgradeMultiplier);

            long balance = coinWallet != null ? coinWallet.Balance : 0;

            string animalLabel = "없음";
            if (animal != null && animalProgress != null)
            {
                int animalRequired = LevelUpRequirementCalculator.GetRequiredDuplicateCount(animalProgress.level);
                long animalLevelUpCost = animal.CalculateLevelUpCoinCost(animalProgress.level);
                animalLabel =
                    $"{animal.DisplayName} (Lv.{animalProgress.level}, 중복 {animalProgress.duplicateCount}/{animalRequired}, 비용 {animalLevelUpCost})   " +
                    $"[보유 종류 {animalsById.Count}]";
            }

            string toolLabel = "없음";
            if (tool != null && toolProgress != null)
            {
                int toolRequired = LevelUpRequirementCalculator.GetRequiredDuplicateCount(toolProgress.level);
                long toolLevelUpCost = tool.CalculateLevelUpCoinCost(toolProgress.level);
                toolLabel =
                    $"{tool.DisplayName} (Lv.{toolProgress.level}, 중복 {toolProgress.duplicateCount}/{toolRequired}, 비용 {toolLevelUpCost})   " +
                    $"[보유 종류 {toolsById.Count}]";
            }

            string failureLine = string.IsNullOrEmpty(levelUpFailureMessage) ? string.Empty : $"\n{levelUpFailureMessage}";

            statusText.text =
                $"Tool: {toolLabel}\nAnimal: {animalLabel}\n" +
                $"Difficulty: {difficulty}   Town Upgrade: x{TownUpgradeMultiplier:0.0}\n" +
                $"Coin/s: {coinPerSecond:0.##}\n" +
                $"Coin: {balance}" +
                failureLine;
        }
    }
}
