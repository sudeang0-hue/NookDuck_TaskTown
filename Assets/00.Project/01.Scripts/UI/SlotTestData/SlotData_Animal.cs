using Animal.Data;
using System;
using UnityEngine;

namespace Test.UI
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


    }
}