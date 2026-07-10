using UnityEngine;
using UnityEngine.UI;

namespace TaskTown.Gacha.Demo
{
    // 처음에는 동물/도구가 없는 상태에서 시작해서, 가챠(AnimalGachaManager/ToolGachaManager)로
    // 동물을 하나 얻으면 그때부터 동물 자체 생산량만으로 초당 코인이 생산되기 시작하고,
    // 도구까지 얻으면 도구 추가 생산량(및 특화 보너스)이 합쳐지는지 확인하는 테스트 전용 데모입니다.
    // 배치 UI가 아직 없으므로, 가장 최근에 뽑힌 동물/도구 조합으로 생산량을 계산합니다.
    public class FinalProductionDemoUI : MonoBehaviour
    {
        [SerializeField] private AnimalGachaManager animalGachaManager;
        [SerializeField] private ToolGachaManager toolGachaManager;
        [SerializeField] private DifficultyProductionTable difficultyTable;

        [Tooltip("ICoinWallet를 구현한 컴포넌트를 연결합니다(예: CoinManager). 비워두면 코인 누적 없이 수치만 표시합니다.")]
        [SerializeField] private MonoBehaviour coinWalletSource;

        [Header("Buttons")]
        [SerializeField] private Button resetOwnedButton;
        [SerializeField] private Button nextDifficultyButton;
        [SerializeField] private Button animalLevelUpButton;
        [SerializeField] private Button toolLevelUpButton;
        [SerializeField] private Button townUpgradeUpButton;

        [Header("Texts")]
        [SerializeField] private Text statusText;

        private ICoinWallet coinWallet;
        private AnimalData ownedAnimal;
        private ToolData ownedTool;
        private int animalLevel = 1;
        private int toolLevel = 1;
        private DifficultyType difficulty = DifficultyType.Normal;
        private float townUpgradeMultiplier = 1f;

        // 초당 생산량의 소수점 이하를 보관하다가 1 이상 쌓이면 정수만큼 코인으로 반영합니다.
        private float productionBuffer;

        private void Awake()
        {
            if (resetOwnedButton != null) resetOwnedButton.onClick.AddListener(ResetOwned);
            if (nextDifficultyButton != null) nextDifficultyButton.onClick.AddListener(NextDifficulty);
            if (animalLevelUpButton != null) animalLevelUpButton.onClick.AddListener(AnimalLevelUp);
            if (toolLevelUpButton != null) toolLevelUpButton.onClick.AddListener(ToolLevelUp);
            if (townUpgradeUpButton != null) townUpgradeUpButton.onClick.AddListener(TownUpgradeUp);

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
            if (ownedAnimal == null) return;

            float coinPerSecond = FinalProductionCalculator.CalculateCoinPerSecond(
                ownedAnimal, ownedTool, animalLevel, toolLevel, difficulty, difficultyTable, townUpgradeMultiplier);

            productionBuffer += coinPerSecond * Time.deltaTime;
            if (productionBuffer >= 1f && coinWallet != null)
            {
                int wholeCoins = Mathf.FloorToInt(productionBuffer);
                productionBuffer -= wholeCoins;
                coinWallet.Add(wholeCoins);
            }

            RefreshStatusText();
        }

        private void HandleAnimalGachaResolved(GachaResult result)
        {
            ownedAnimal = result.Entry as AnimalData;
            RefreshStatusText();
        }

        private void HandleToolGachaResolved(GachaResult result)
        {
            ownedTool = result.Entry as ToolData;
            RefreshStatusText();
        }

        // 테스트 편의용: 뽑은 동물/도구를 잊고 다시 "아무것도 없는" 초기 상태로 되돌립니다.
        private void ResetOwned()
        {
            ownedAnimal = null;
            ownedTool = null;
            productionBuffer = 0f;
            RefreshStatusText();
        }

        private void NextDifficulty()
        {
            int valueCount = System.Enum.GetValues(typeof(DifficultyType)).Length;
            difficulty = (DifficultyType)(((int)difficulty + 1) % valueCount);
            RefreshStatusText();
        }

        private void AnimalLevelUp()
        {
            animalLevel++;
            RefreshStatusText();
        }

        private void ToolLevelUp()
        {
            toolLevel++;
            RefreshStatusText();
        }

        private void TownUpgradeUp()
        {
            townUpgradeMultiplier += 0.1f;
            RefreshStatusText();
        }

        private void RefreshStatusText()
        {
            if (statusText == null) return;

            if (ownedAnimal == null)
            {
                statusText.text = "Animal: 없음   Tool: 없음\n동물을 뽑아야 생산이 시작됩니다.";
                return;
            }

            float coinPerSecond = FinalProductionCalculator.CalculateCoinPerSecond(
                ownedAnimal, ownedTool, animalLevel, toolLevel, difficulty, difficultyTable, townUpgradeMultiplier);

            long balance = coinWallet != null ? coinWallet.Balance : 0;

            string toolLabel = ownedTool != null ? $"{ownedTool.DisplayName} (Lv.{toolLevel})" : "없음";

            statusText.text =
                $"Tool: {toolLabel}   Animal: {ownedAnimal.DisplayName} (Lv.{animalLevel})\n" +
                $"Difficulty: {difficulty}   Town Upgrade: x{townUpgradeMultiplier:0.0}\n" +
                $"Coin/s: {coinPerSecond:0.##}\n" +
                $"Coin: {balance}";
        }
    }
}
