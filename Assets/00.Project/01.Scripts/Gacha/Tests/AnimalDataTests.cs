using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TaskTown.Gacha.Tests
{
    public class AnimalDataTests
    {
        private static AnimalData CreateAnimal(float baseCoinPerSecond, float levelBonusRatePerLevel, long levelUpBaseCost = 100, float levelUpCostIncreaseRate = 1.5f)
        {
            AnimalData data = ScriptableObject.CreateInstance<AnimalData>();
            SerializedObject so = new SerializedObject(data);
            so.FindProperty("baseCoinPerSecond").floatValue = baseCoinPerSecond;
            so.FindProperty("levelBonusRatePerLevel").floatValue = levelBonusRatePerLevel;
            so.FindProperty("levelUpBaseCost").longValue = levelUpBaseCost;
            so.FindProperty("levelUpCostIncreaseRate").floatValue = levelUpCostIncreaseRate;
            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }

        [Test]
        public void CalculateLevelMultiplier_레벨1이면_배율1이다()
        {
            AnimalData animal = CreateAnimal(2f, 0.2f);

            Assert.AreEqual(1f, animal.CalculateLevelMultiplier(1), 0.0001f);
        }

        [Test]
        public void CalculateLevelMultiplier_레벨이_오를수록_배율이_증가한다()
        {
            AnimalData animal = CreateAnimal(2f, 0.2f);

            // 레벨 3 = 1 + (3-1)*0.2 = 1.4
            Assert.AreEqual(1.4f, animal.CalculateLevelMultiplier(3), 0.0001f);
        }

        [Test]
        public void CalculateLevelUpCoinCost_레벨1이면_기본비용_그대로다()
        {
            AnimalData animal = CreateAnimal(2f, 0.2f, levelUpBaseCost: 100, levelUpCostIncreaseRate: 1.5f);

            Assert.AreEqual(100, animal.CalculateLevelUpCoinCost(1));
        }

        [Test]
        public void CalculateLevelUpCoinCost_레벨이_오를수록_비용이_증가한다()
        {
            AnimalData animal = CreateAnimal(2f, 0.2f, levelUpBaseCost: 100, levelUpCostIncreaseRate: 1.5f);

            // 레벨 3 = 100 * 1.5^2 = 225
            Assert.AreEqual(225, animal.CalculateLevelUpCoinCost(3));
        }
    }
}
