using NUnit.Framework;
using TaskTown.SceneFlow;

namespace TaskTown.EditorTests.Tutorial
{
    public class GameInputGateTests
    {
        private const string FirstOwner = "GameInputGateTests.First";
        private const string SecondOwner = "GameInputGateTests.Second";

        [SetUp]
        public void SetUp()
        {
            GameInputGate.Release(FirstOwner);
            GameInputGate.Release(SecondOwner);
        }

        [TearDown]
        public void TearDown()
        {
            GameInputGate.Release(FirstOwner);
            GameInputGate.Release(SecondOwner);
        }

        [Test]
        public void Block_한소유자가차단하면입력이비활성화된다()
        {
            GameInputGate.Block(FirstOwner);

            Assert.IsFalse(GameInputGate.IsInputAllowed);
            Assert.IsTrue(GameInputGate.IsBlockedBy(FirstOwner));
        }

        [Test]
        public void Release_모든소유자가해제해야입력이활성화된다()
        {
            GameInputGate.Block(FirstOwner);
            GameInputGate.Block(SecondOwner);

            GameInputGate.Release(FirstOwner);
            Assert.IsFalse(GameInputGate.IsInputAllowed);

            GameInputGate.Release(SecondOwner);
            Assert.IsTrue(GameInputGate.IsInputAllowed);
        }

        [Test]
        public void Block_같은소유자의중복차단은한번만등록된다()
        {
            int beforeCount = GameInputGate.BlockerCount;

            Assert.IsTrue(GameInputGate.Block(FirstOwner));
            Assert.IsFalse(GameInputGate.Block(FirstOwner));
            Assert.AreEqual(beforeCount + 1, GameInputGate.BlockerCount);
        }
    }
}
