using Animal.Data;
using System;
using UnityEngine;

namespace Test
{
    [Serializable]
    public class SlotData_Animal
    {
        [Header("저장 데이터")]
        [SerializeField] private AnimalDataSO animalData;
        [SerializeField] private int level;
        [SerializeField] private int currentCount;

        [Header("레벨업 계산 데이터")]
        [SerializeField] private int requiredUpgradeCount;
        [SerializeField] private long levelUpCost;
        [SerializeField] private bool isMaxLevel;


        public AnimalDataSO AnimalData => animalData;
        public string AnimalId => animalData != null ? animalData.Id : string.Empty;
        public int Level => level;
        public int CurrentCount => currentCount;

        public int RequiredUpgradeCount => requiredUpgradeCount;
        public long LevelUpCost => levelUpCost;
        public bool IsMaxLevel => isMaxLevel;


        public SlotData_Animal(AnimalDataSO animalData, int level, int currentCount)
        {
            this.animalData = animalData;
            this.level = Mathf.Max(1, level);
            this.currentCount = Mathf.Max(1, currentCount);

        }

        public void AddCount(int amount)
        {
            if (amount <= 0)
                return;

            currentCount += amount;
        }

        /// <summary>
        /// 요구 수량, 레벨업 비용, 최대 레벨 여부 설정
        /// </summary>
        public void ApplyGrowthData(int requiredCount, long cost, bool maxLevel)
        {
            requiredUpgradeCount = Mathf.Max(0, requiredCount);
            levelUpCost = Math.Max(0L, cost);
            isMaxLevel = maxLevel;
        }


        /// <summary>
        /// 레벨업 가능 여부.
        /// 현재 총 보유 수량이 레벨업 요구 수량 이상인지 확인합니다.
        /// </summary>
        public bool CanLevelUp()
        {
            if (isMaxLevel)
                return false;

            if (requiredUpgradeCount <= 0)
                return false;

            return currentCount >= requiredUpgradeCount;
        }

        /// <summary>
        /// 레벨업 필요 총수량에서 강화되어 남는 동물 한 마리를 제외한
        /// 수량을 소비합니다.
        /// </summary>
        public bool TryConsumeForLevelUp()
        {
            if (!CanLevelUp())
                return false;

            int consumeCount =
                Mathf.Max(0, requiredUpgradeCount - 1);

            currentCount -= consumeCount;

            return true;
        }

        /// <summary>
        /// 현재 동물의 레벨을 1 증가시킵니다.
        /// </summary>
        public void IncreaseLevel()
        {
            if (isMaxLevel)
                return;

            level++;
        }

    }
}