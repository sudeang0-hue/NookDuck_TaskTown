using NUnit.Framework;

namespace TaskTown.Gacha.Tests
{
    public class GachaCostCalculatorTests
    {
        private static GachaCostConfig CreateConfig()
        {
            return new GachaCostConfig
            {
                baseCost = 100,
                costIncreaseRate = 1.1f,
                townLevelCostMultiplierPerLevel = 0.5f
            };
        }

        [Test]
        public void CalculateCost_뽑기0회_마을레벨1이면_기본비용과_같다()
        {
            long cost = GachaCostCalculator.CalculateCost(CreateConfig(), gachaCount: 0, townLevel: 1);

            Assert.AreEqual(100, cost);
        }

        [Test]
        public void CalculateCost_뽑기횟수가_늘어날수록_비용이_증가한다()
        {
            GachaCostConfig config = CreateConfig();

            long costAtStart = GachaCostCalculator.CalculateCost(config, gachaCount: 0, townLevel: 1);
            long costAfterFiveRolls = GachaCostCalculator.CalculateCost(config, gachaCount: 5, townLevel: 1);

            Assert.Greater(costAfterFiveRolls, costAtStart);
        }

        [Test]
        public void CalculateCost_마을레벨이_높을수록_비용이_증가한다()
        {
            GachaCostConfig config = CreateConfig();

            long costAtLevel1 = GachaCostCalculator.CalculateCost(config, gachaCount: 0, townLevel: 1);
            long costAtLevel3 = GachaCostCalculator.CalculateCost(config, gachaCount: 0, townLevel: 3);

            // townLevelCostMultiplierPerLevel = 0.5, 레벨3 → 1 + (3-1)*0.5 = 2배
            Assert.AreEqual(costAtLevel1 * 2, costAtLevel3);
        }
    }
}
