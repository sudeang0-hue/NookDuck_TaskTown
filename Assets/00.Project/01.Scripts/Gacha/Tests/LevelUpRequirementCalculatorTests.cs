using NUnit.Framework;

namespace TaskTown.Gacha.Tests
{
    public class LevelUpRequirementCalculatorTests
    {
        [Test]
        public void GetRequiredDuplicateCount_레벨1에서_2로_가려면_4개_필요하다()
        {
            Assert.AreEqual(4, LevelUpRequirementCalculator.GetRequiredDuplicateCount(1));
        }

        [Test]
        public void GetRequiredDuplicateCount_레벨2에서_3으로_가려면_16개_필요하다()
        {
            Assert.AreEqual(16, LevelUpRequirementCalculator.GetRequiredDuplicateCount(2));
        }

        [Test]
        public void GetRequiredDuplicateCount_레벨3에서_4로_가려면_64개_필요하다()
        {
            Assert.AreEqual(64, LevelUpRequirementCalculator.GetRequiredDuplicateCount(3));
        }

        [Test]
        public void GetRequiredDuplicateCount_레벨0이하는_레벨1_기준으로_계산한다()
        {
            Assert.AreEqual(4, LevelUpRequirementCalculator.GetRequiredDuplicateCount(0));
        }
    }
}
