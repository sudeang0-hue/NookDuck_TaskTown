using Animal.Data;
using System;
using UnityEngine;

namespace TaskTown.KDH
{
    [System.Serializable]
    public class SlotData_Animal
    {
        [Header("저장 데이터")]
        [SerializeField] private AnimalDataSO animalData;
        [SerializeField] private int level = 1;
        [SerializeField] private int currentCount = 0;

        [Header("레벨업 계산 데이터")]
        [SerializeField] private bool isMaxLevel;
        [SerializeField] private int levelUpCost;
        [SerializeField] private int requiredUpgradeCount; 
        //[SerializeField] private long levelUpCost;


        public AnimalDataSO AnimalData => animalData;
        public string AnimalId => animalData != null ? animalData.Id : string.Empty;
        public int Level => level;
        public int CurrentCount => currentCount;
        public bool IsMaxLevel => isMaxLevel;
        public int LevelUpCost => levelUpCost;
        public int RequiredUpgradeCount => requiredUpgradeCount;


        public SlotData_Animal(AnimalDataSO animalData, int level, int currentCount)
        {
            this.animalData = animalData;
            this.level = Mathf.Max(1, level);
            this.currentCount = Mathf.Max(1, currentCount);
        }

        /// <summary>
        /// 동일한 동물을 획득했을 때 보유 수량을 증가.
        /// </summary>
        public void AddCount()
        {
            currentCount++;
        }

        /// <summary>
        /// 동물 레벨을 1 증가시킵니다.
        /// </summary>
        public void AnimalLevelUp()
        {
            if (IsMaxLevel) return;

            level++;

            if (level == 5) isMaxLevel = true;
        }

        // 임시 기능(대체 가능)
        /// <summary>
        /// 레벨업에 필요한 코스트를 구합니다.
        /// </summary>
        public int GetLevelUpCost()
        {
            if (level <= 0) return 99999;

            int neededCost;

            if (level == 1) neededCost = 4;
            else if (level == 2) neededCost = 16;
            else if (level == 3) neededCost = 64;
            else if (level == 4) neededCost = 256;
            else neededCost = 99999;

            levelUpCost = neededCost;

            return levelUpCost;
        }

        /// <summary>
        /// 요구 수량, 레벨업 비용, 최대 레벨 여부 설정
        /// </summary>
        public void ApplyGrowthData(int requiredCount, int cost, bool maxLevel)
        {
            requiredUpgradeCount = Mathf.Max(0, requiredCount);
            levelUpCost = Math.Max(0, cost);
            isMaxLevel = maxLevel;
        }

        /// <summary>
        /// 레벨업 가능 여부.
        /// 본체 1마리를 제외한 재료 수량이 요구 수량 이상인지 확인합니다.
        /// </summary>
        public bool CanLevelUp()
        {
            if (isMaxLevel)
                return false;

            if (requiredUpgradeCount <= 0)
                return false;

            // UI 표시 수량(CurrentCount - 1)이 요구 수량에 도달했는지 판정
            return (currentCount - 1) >= requiredUpgradeCount;
        }

        /// <summary>
        /// 레벨업 재료 수량을 소비합니다.
        /// 본체 1마리는 남기고, 요구 수량만큼만 차감합니다.
        /// </summary>
        public bool TryConsumeForLevelUp()
        {
            if (!CanLevelUp())
                return false;

            currentCount -= requiredUpgradeCount;

            // 본체 1마리는 항상 유지
            if (currentCount < 1)
                currentCount = 1;

            return true;
        }
    }
}
