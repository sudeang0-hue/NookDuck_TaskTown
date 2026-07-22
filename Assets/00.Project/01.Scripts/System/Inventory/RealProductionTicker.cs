using Animal.Data;
using TaskTown.Gacha;
using UnityEngine;

namespace TaskTown.KDH
{
    // 동물이 도구에 장착된(CurrentAnimalSet=true) 슬롯 기준으로 1초마다 실제 생산량
    // (FinalProductionCalculator, 뽑기/밸런스 쪽과 동일한 계산식)을 계산해서 CoinManager에
    // 적립합니다. Test_KDH_CoinProductionManager/InventoryProductionCalculator는 AnimalDataSO에
    // baseCoinPerSecond가 없던 시절 만든 임시 버전이라 이 클래스로 대체합니다.
    //
    // 도구 자체의 "마을 배치"(CurrentSet/TrySetTool) UI가 아직 없어서, 생산 여부는 도구 배치가
    // 아니라 "동물이 그 도구에 장착됐는지"(CurrentAnimalSet)로 판단합니다(사용자 확인). 동물이
    // 장착된 도구는 도구 자체 생산 + 동물 생산(+특화 보너스)을 함께 계산합니다(InventoryManager_Animal
    // 쪽에는 별도의 "배치" 개념이 없음 - 도구 슬롯의 CurrentAnimalId로만 동물이 연결됩니다).
    public class RealProductionTicker : MonoBehaviour
    {
        public static RealProductionTicker Instance { get; private set; }

        [SerializeField] private DifficultyProductionTable difficultyTable;
        [SerializeField] private DifficultyType difficulty = DifficultyType.Normal;

        [Tooltip("ITownLevelProvider를 구현한 컴포넌트를 연결합니다. 비워두면 마을 레벨 1로 취급합니다.")]
        [SerializeField] private MonoBehaviour townLevelProviderSource;
        [SerializeField] private TownUpgradeEffectConfig townUpgradeEffectConfig;

        [Tooltip("ICoinWallet을 구현한 컴포넌트(CoinManager)를 연결합니다.")]
        [SerializeField] private MonoBehaviour coinWalletSource;

        private float productionBuffer;

        // 현재 초당 생산량. UIController_Coin 등 시간당 획득량 표시용 UI가 참조합니다.
        public float CurrentCoinPerSecond { get; private set; }

        private ITownLevelProvider TownLevelProvider => townLevelProviderSource as ITownLevelProvider;
        private ICoinWallet CoinWallet => coinWalletSource as ICoinWallet;

        private void Awake()
        {
            Instance = this;
        }

        private float TownUpgradeMultiplier
        {
            get
            {
                int townLevel = TownLevelProvider?.CurrentTownLevel ?? 1;
                return townUpgradeEffectConfig != null
                    ? townUpgradeEffectConfig.GetProductionMultiplier(townLevel)
                    : 1f;
            }
        }

        public void SetDifficulty(DifficultyType newDifficulty)
        {
            difficulty = newDifficulty;
        }

        private void Update()
        {
            CurrentCoinPerSecond = CalculateTotalCoinPerSecond();
            productionBuffer += CurrentCoinPerSecond * Time.deltaTime;

            if (productionBuffer < 1f) return;

            int wholeCoins = Mathf.FloorToInt(productionBuffer);
            productionBuffer -= wholeCoins;
            CoinWallet?.Add(wholeCoins);
        }

        private float CalculateTotalCoinPerSecond()
        {
            if (InventoryManager_Tool.Instance == null) return 0f;

            float total = 0f;
            float townUpgradeMultiplier = TownUpgradeMultiplier;

            foreach (SlotData_Tool toolSlot in InventoryManager_Tool.Instance.ToolSlotsList)
            {
                if (toolSlot == null || !toolSlot.CurrentAnimalSet) continue;

                AnimalDataSO animalData = null;
                int animalLevel = 1;

                if (InventoryManager_Animal.Instance != null
                    && InventoryManager_Animal.Instance.TryGetAnimalSlot(toolSlot.CurrentAnimalId, out SlotData_Animal animalSlot))
                {
                    animalData = animalSlot.AnimalData;
                    animalLevel = animalSlot.Level;
                }

                total += FinalProductionCalculator.CalculateCoinPerSecond(
                    animalData,
                    toolSlot.ToolData,
                    animalLevel,
                    toolSlot.Level,
                    difficulty,
                    difficultyTable,
                    townUpgradeMultiplier);
            }

            return total;
        }
    }
}
