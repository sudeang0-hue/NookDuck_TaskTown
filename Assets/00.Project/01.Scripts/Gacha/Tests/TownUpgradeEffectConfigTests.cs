using NUnit.Framework;

namespace TaskTown.Gacha.Tests
{
    public class TownUpgradeEffectConfigTests
    {
        [Test]
        public void GetProductionMultiplier_레벨1은_배율1이다()
        {
            TownUpgradeEffectConfig config = new TownUpgradeEffectConfig();

            Assert.AreEqual(1f, config.GetProductionMultiplier(1));
        }

        [Test]
        public void GetProductionMultiplier_레벨당_10퍼센트씩_증가한다()
        {
            TownUpgradeEffectConfig config = new TownUpgradeEffectConfig();

            Assert.AreEqual(1.5f, config.GetProductionMultiplier(6), 0.0001f);
            Assert.AreEqual(1.9f, config.GetProductionMultiplier(10), 0.0001f);
        }

        [Test]
        public void GetProductionMultiplier_레벨0이하는_레벨1_기준으로_계산한다()
        {
            TownUpgradeEffectConfig config = new TownUpgradeEffectConfig();

            Assert.AreEqual(1f, config.GetProductionMultiplier(0));
        }
    }
}
