using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TaskTown.Gacha.Tests
{
    // 생산량/레벨업 계산 로직은 GachaEntryData(공통 베이스)에 있어서, 동물/도구 어느 쪽이든 동일하게
    // 적용됩니다. 이전에는 AnimalDataTests/ToolDataTests로 나뉘어 같은 로직을 중복 검증했지만,
    // 로직이 공통 베이스로 합쳐지면서 이 파일 하나로 통합했습니다.
    public class GachaEntryDataTests
    {
        private static TestAnimalEntry CreateEntry(float baseCoinPerSecond, float levelBonusRatePerLevel, long levelUpBaseCost = 100, float levelUpCostIncreaseRate = 1.5f)
        {
            TestAnimalEntry data = ScriptableObject.CreateInstance<TestAnimalEntry>();
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
            TestAnimalEntry entry = CreateEntry(2f, 0.2f);

            Assert.AreEqual(1f, entry.CalculateLevelMultiplier(1), 0.0001f);
        }

        [Test]
        public void CalculateLevelMultiplier_레벨이_오를수록_배율이_증가한다()
        {
            TestAnimalEntry entry = CreateEntry(2f, 0.2f);

            // 레벨 3 = 1 + (3-1)*0.2 = 1.4
            Assert.AreEqual(1.4f, entry.CalculateLevelMultiplier(3), 0.0001f);
        }

        [Test]
        public void CalculateLevelUpCoinCost_레벨1이면_기본비용_그대로다()
        {
            TestAnimalEntry entry = CreateEntry(2f, 0.2f, levelUpBaseCost: 100, levelUpCostIncreaseRate: 1.5f);

            Assert.AreEqual(100, entry.CalculateLevelUpCoinCost(1));
        }

        [Test]
        public void CalculateLevelUpCoinCost_레벨이_오를수록_비용이_증가한다()
        {
            TestAnimalEntry entry = CreateEntry(2f, 0.2f, levelUpBaseCost: 100, levelUpCostIncreaseRate: 1.5f);

            // 레벨 3 = 100 * 1.5^2 = 225
            Assert.AreEqual(225, entry.CalculateLevelUpCoinCost(3));
        }
    }
}
