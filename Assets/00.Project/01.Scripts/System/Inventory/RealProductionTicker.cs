using System;
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

        // -----------------------------------------------------------------------------
        // [ 2026.07.27 - Choi - 튜토리얼 기능 업데이트 ]
        // 기능: 자동 생산 코인이 실제 지급된 시점을 튜토리얼 진행 판정에 전달합니다.
        // -----------------------------------------------------------------------------
        /// <summary>
        /// 자동 생산 코인이 지갑에 실제 지급된 경우에만 발생합니다.
        /// </summary>
        public event Action<int> ProductionCoinGranted;

        [SerializeField] private DifficultyProductionTable difficultyTable;
        [SerializeField] private DifficultyType difficulty = DifficultyType.Normal;

        [Tooltip("ICoinWallet을 구현한 컴포넌트(CoinManager)를 연결합니다.")]
        [SerializeField] private MonoBehaviour coinWalletSource;

        private float productionBuffer;

        // 현재 초당 생산량. UIController_Coin 등 시간당 획득량 표시용 UI가 참조합니다.
        public float CurrentCoinPerSecond { get; private set; }

        private ICoinWallet CoinWallet => coinWalletSource as ICoinWallet;

        private void Awake()
        {
            Instance = this;
        }

        // 이슈 #72: 예전에는 마을 레벨이 오르면 자동으로 전체 생산량에 배율이 붙었지만(TownUpgradeEffectConfig),
        // 이제는 플레이어가 직접 구매하는 도구 효율 업그레이드(TownUpgradeManager)만큼만 배율이 오릅니다.
        private float ToolEfficiencyMultiplier =>
            TownUpgradeManager.Instance != null ? TownUpgradeManager.Instance.ToolEfficiencyMultiplier : 1f;

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

            // -----------------------------------------------------------------------------
            // [ 2026.07.27 - Choi - 튜토리얼 기능 업데이트 ]
            // 기능: 유효한 지갑에 정수 코인이 지급된 경우에만 자동 생산 이벤트를 보냅니다.
            // -----------------------------------------------------------------------------
            ICoinWallet coinWallet = CoinWallet;
            if (coinWallet == null)
                return;

            coinWallet.Add(wholeCoins);
            ProductionCoinGranted?.Invoke(wholeCoins);
        }

        // 프레임을 기다리지 않고 즉시 현재 생산량을 계산합니다(오프라인 보상 등 앱 시작 직후에 필요).
        public float CalculateTotalCoinPerSecond()
        {
            if (InventoryManager_Tool.Instance == null) return 0f;

            float total = 0f;
            float townUpgradeMultiplier = ToolEfficiencyMultiplier;

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
