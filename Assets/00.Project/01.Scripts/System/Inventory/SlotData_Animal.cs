using Animal.Data;
using System;
using TaskTown.Gacha;
using UnityEngine;

namespace TaskTown.KDH
{
    [System.Serializable]
    public class SlotData_Animal
    {
        [Header("동물 슬롯 정보")]
        [SerializeField] private AnimalDataSO animalData;
        [SerializeField] private int level = 1;
        [SerializeField] private int currentCount = 0;

        [Header("레벨업 요구 데이터")]
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
        /// 동물 슬롯을 획득했을 때 개수를 늘립니다.
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

        /// <summary>
        /// 다음 레벨로 가기 위해 이번 단계에서 필요한 중복 개수입니다 (표시용).
        /// LevelUpRequirementCalculator(TaskTown.Gacha) 기준값을 그대로 씁니다.
        /// </summary>
        public int GetLevelUpCost()
        {
            int neededCost = LevelUpRequirementCalculator.GetRequiredDuplicateCount(level);
            levelUpCost = neededCost;
            return levelUpCost;
        }

        /// <summary>
        /// 요구 개수, 레벨업 비용, 최대 레벨 여부를 설정합니다.
        /// </summary>
        public void ApplyGrowthData(int requiredCount, int cost, bool maxLevel)
        {
            requiredUpgradeCount = Mathf.Max(0, requiredCount);
            levelUpCost = Math.Max(0, cost);
            isMaxLevel = maxLevel;
        }

        /// <summary>
        /// 레벨업 가능 여부.
        /// 본체 1개를 제외한 나머지 개수가 요구 개수 이상인지 확인합니다.
        /// </summary>
        public bool CanLevelUp()
        {
            if (isMaxLevel)
                return false;

            if (requiredUpgradeCount <= 0)
                return false;

            // UIController_AnimalInvPage 표시 개수(CurrentCount - 1)가 요구 개수를 충족했는지 판정
            return (currentCount - 1) >= requiredUpgradeCount;
        }

        /// <summary>
        /// 레벨업에 필요한 개수를 소비합니다.
        /// 본체 1개는 남기고, 요구 개수만큼만 소모합니다.
        /// </summary>
        public bool TryConsumeForLevelUp()
        {
            if (!CanLevelUp())
                return false;

            currentCount -= requiredUpgradeCount;

            // 본체 1개는 항상 유지
            if (currentCount < 1)
                currentCount = 1;

            return true;
        }

        /// <summary>
        /// 다음 레벨로 올리는 데 필요한 코인 비용입니다 (GachaEntryData 공통 계산식 기준).
        /// </summary>
        public long GetLevelUpCoinCost()
        {
            return animalData != null ? animalData.CalculateLevelUpCoinCost(level) : 0L;
        }


        /// <summary>
        /// 디버그 전용: 재료/코인 없이 레벨을 바로 설정합니다. (최대 5)  26.07.24 KDH 추가
        /// </summary>
        public void DebugSetLevel(int targetLevel)
        {
            level = Mathf.Clamp(targetLevel, 1, 5);
            isMaxLevel = level >= 5;
        }
    }
}
