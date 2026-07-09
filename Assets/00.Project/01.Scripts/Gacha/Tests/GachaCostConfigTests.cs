using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace TaskTown.Gacha.Tests
{
    public class GachaCostConfigTests
    {
        private static readonly FieldInfo LevelCostsField =
            typeof(GachaCostConfig).GetField("levelCosts", BindingFlags.NonPublic | BindingFlags.Instance);

        private static GachaCostConfig CreateConfig(params (int townLevelThreshold, long cost)[] entries)
        {
            GachaCostConfig config = new GachaCostConfig();
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
            GachaCostConfig config = CreateConfig((1, 100), (5, 300));

            Assert.AreEqual(100, config.GetCostForTownLevel(1));
            Assert.AreEqual(300, config.GetCostForTownLevel(5));
        }

        [Test]
        public void GetCostForTownLevel_중간_레벨은_이전_구간_비용을_유지한다()
        {
            GachaCostConfig config = CreateConfig((1, 100), (5, 300));

            // 레벨 3은 5 미만이므로 여전히 레벨 1 구간(100)이 적용되어야 함
            Assert.AreEqual(100, config.GetCostForTownLevel(3));
        }

        [Test]
        public void GetCostForTownLevel_최고_구간_이후는_해당_비용을_그대로_유지한다()
        {
            GachaCostConfig config = CreateConfig((1, 100), (5, 300));

            Assert.AreEqual(300, config.GetCostForTownLevel(10));
        }
    }
}
