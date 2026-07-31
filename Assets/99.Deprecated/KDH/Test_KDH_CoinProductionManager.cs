using System.Collections;
using Animal.Data;
using TaskTown.Gacha;
using UnityEngine;

namespace TaskTown.KDH
{
    public class CoinProductionManager : MonoBehaviour
    {
        [SerializeField] private DifficultyProductionTable difficultyTable;
        [SerializeField] private DifficultyType difficulty = DifficultyType.Normal;
        [SerializeField] private float townUpgradeMultiplier = 1f;

        private float productionBuffer;
        private Coroutine productionRoutine;

        private void OnEnable()
        {
            if (InventoryManager_Tool.Instance != null)
            {
                InventoryManager_Tool.Instance.OnToolInventoryChanged += RefreshProduction;
                InventoryManager_Tool.Instance.OnToolSlotChanged += HandleToolSlotChanged;
            }

            productionRoutine = StartCoroutine(ProduceCoinEverySecond());
        }
        private void OnDisable()
        {
            if (InventoryManager_Tool.Instance != null)
            {
                InventoryManager_Tool.Instance.OnToolInventoryChanged -= RefreshProduction;
                InventoryManager_Tool.Instance.OnToolSlotChanged -= HandleToolSlotChanged;
            }

            if (productionRoutine != null) StopCoroutine(productionRoutine);
        }

        // 배치 변경 시 즉시 재계산 (폴링 Update 불필요)
        private void HandleToolSlotChanged(SlotData_Tool slot) => RefreshProduction();
        private void RefreshProduction() { /* UIController_AnimalInvPage 갱신용으로 coin/s 표시 가능 */ }

        private IEnumerator ProduceCoinEverySecond()
        {
            var wait = new WaitForSeconds(1f); // 1초 고정 주기

            while (true)
            {
                yield return wait;
                TickProduction(1f);
            }
        }
        private void TickProduction(float deltaSeconds)
        {
            if (InventoryManager_Tool.Instance == null || CoinManager.Instance == null) return;

            float totalCoinPerSecond = 0f;

            foreach (SlotData_Tool toolSlot in InventoryManager_Tool.Instance.ToolSlotsList)
            {
                if (toolSlot == null || !toolSlot.CurrentSet) continue;

                AnimalDataSO animalData = null;
                int animalLevel = 1;

                if (toolSlot.CurrentAnimalSet
                    && InventoryManager_Animal.Instance != null
                    && InventoryManager_Animal.Instance.TryGetAnimalSlot(
                        toolSlot.CurrentAnimalId, out SlotData_Animal animalSlot))
                {
                    animalData = animalSlot.AnimalData;
                    animalLevel = animalSlot.Level;
                }

                totalCoinPerSecond += InventoryProductionCalculator.CalculateCoinPerSecond(
                    toolSlot.ToolData,
                    toolSlot.Level,
                    animalData,
                    animalLevel,
                    difficultyTable,
                    difficulty,
                    townUpgradeMultiplier);
            }

            productionBuffer += totalCoinPerSecond * deltaSeconds;

            // 소수점 누적 후 정수만 지급 (FinalProductionDemoUI와 동일 패턴)
            if (productionBuffer >= 1f)
            {
                int wholeCoins = Mathf.FloorToInt(productionBuffer);
                productionBuffer -= wholeCoins;
                CoinManager.Instance.Add(wholeCoins);
            }
        }
    }
}
