using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace TaskTown.Gacha.Tests
{
    public class TownUpgradeCostConfigTests
    {
        private static readonly FieldInfo LevelCostsField =
            typeof(TownUpgradeCostConfig).GetField("levelCosts", BindingFlags.NonPublic | BindingFlags.Instance);

        private static TownUpgradeCostConfig CreateConfig(params (int townLevelThreshold, long cost)[] entries)
        {
            TownUpgradeCostConfig config = new TownUpgradeCostConfig();
            List<LevelCostEntry> levelCosts = (List<LevelCostEntry>)LevelCostsField.GetValue(config);
            levelCosts.Clear();
            foreach ((int townLevelThreshold, long cost) in entries)
            {
                levelCosts.Add(new LevelCostEntry { townLevelThreshold = townLevelThreshold, cost = cost });
            }

            return config;
        }

        [Test]
        public void GetCostForTownLevel_정의된_레벨이면_해당_비용을_반환한다()
        {
            TownUpgradeCostConfig config = CreateConfig((1, 1000), (5, 16000));

            Assert.AreEqual(1000, config.GetCostForTownLevel(1));
            Assert.AreEqual(16000, config.GetCostForTownLevel(5));
        }

        [Test]
        public void GetCostForTownLevel_중간_레벨은_이전_구간_비용을_유지한다()
        {
            TownUpgradeCostConfig config = CreateConfig((1, 1000), (5, 16000));

            // 레벨 3은 5 미만이므로 여전히 레벨 1 구간(1000)이 적용되어야 함
            Assert.AreEqual(1000, config.GetCostForTownLevel(3));
        }

        [Test]
        public void GetCostForTownLevel_최고_구간_이후는_해당_비용을_그대로_유지한다()
        {
            TownUpgradeCostConfig config = CreateConfig((1, 1000), (5, 16000));

            Assert.AreEqual(16000, config.GetCostForTownLevel(10));
        }

        [Test]
        public void GetCostForTownLevel_기본값은_레벨이_오를수록_비용이_증가한다()
        {
            TownUpgradeCostConfig config = new TownUpgradeCostConfig();

            long level1Cost = config.GetCostForTownLevel(1);
            long level5Cost = config.GetCostForTownLevel(5);
            long level10Cost = config.GetCostForTownLevel(10);

            Assert.Greater(level5Cost, level1Cost);
            Assert.Greater(level10Cost, level5Cost);
        }
    }
}
