using Animal.Data;
using TaskTown.Gacha;
using Tool.Data;
using UnityEngine;

namespace TaskTown.KDH
{
    public static class InventoryProductionCalculator
    {//나중에 AnimalDataSO에 baseCoinPerSecond, levelBonusRatePerLevel을 추가하면 DefaultAnimalCoinPerSecond 대신 SO 값을 쓰면 됩니다.

        private const float DefaultLevelBonusPerLevel = 0.2f; 
        private const float DefaultAnimalCoinPerSecond = 1f;

        // 함수 이거 하나가 끝입니다.
        public static float CalculateCoinPerSecond(
            ToolDataSO toolData,
            int toolLevel,
            AnimalDataSO animalData,
            int animalLevel,
            DifficultyProductionTable difficultyTable = null,
            DifficultyType difficulty = DifficultyType.Normal,
            float townUpgradeMultiplier = 1f)
        {
            if (toolData == null && animalData == null) return 0f;

            float toolProduction = 0f;

            if (toolData != null)
            {
                float toolMultiplier = 1f + Mathf.Max(0, toolLevel - 1) * DefaultLevelBonusPerLevel;
                toolProduction = toolData.BaseCoinPerSecond * toolMultiplier;
            }

            float animalProduction = 0f;

            if (animalData != null)
            {
                // AnimalDataSO에 baseCoinPerSecond 추가 전까지 임시 기본값
                float animalMultiplier = 1f + Mathf.Max(0, animalLevel - 1) * DefaultLevelBonusPerLevel;
                animalProduction = DefaultAnimalCoinPerSecond * animalMultiplier;
            }

            float specialBonus = 1f;

            if (toolData != null && animalData != null
                && !string.IsNullOrEmpty(toolData.SpecialAnimalId)
                && toolData.SpecialAnimalId == animalData.Id)
            {
                specialBonus = 1f + toolData.SpecialAnimalBonusRate;
            }

            float combined = (animalProduction + toolProduction) * specialBonus;

            float difficultyMultiplier = difficultyTable != null ? difficultyTable.GetMultiplier(difficulty) : 1f;

            return combined * difficultyMultiplier * townUpgradeMultiplier;
        }
    }
}