using NUnit.Framework;
using TaskTown.KDH;
using TaskTown.Tutorial;

namespace TaskTown.EditorTests.Tutorial
{
    public class TutorialOverlayLoaderTests
    {
        [Test]
        public void ShouldLoad_저장데이터가없으면로드한다()
        {
            Assert.IsTrue(TutorialOverlayLoader.ShouldLoad(null));
        }

        [Test]
        public void ShouldLoad_미완료진행이면로드한다()
        {
            TutorialSaveData progress = TutorialSaveData.CreateDefault();

            Assert.IsTrue(TutorialOverlayLoader.ShouldLoad(progress));
        }

        [Test]
        public void ShouldLoad_완료진행이면로드하지않는다()
        {
            TutorialSaveData progress = TutorialSaveData.CreateDefault();
            progress.currentStep = TutorialStep.Completed;

            Assert.IsFalse(TutorialOverlayLoader.ShouldLoad(progress));
        }

        [TestCase(TutorialOverlayLoadState.Skipped)]
        [TestCase(TutorialOverlayLoadState.Active)]
        [TestCase(TutorialOverlayLoadState.Failed)]
        public void IsStartupReadyState_종료상태이면입력을허용할수있다(
            TutorialOverlayLoadState state)
        {
            Assert.IsTrue(TutorialOverlayLoader.IsStartupReadyState(state));
        }

        [TestCase(TutorialOverlayLoadState.Idle)]
        [TestCase(TutorialOverlayLoadState.Loading)]
        [TestCase(TutorialOverlayLoadState.Unloading)]
        public void IsStartupReadyState_준비중상태이면입력을계속차단한다(
            TutorialOverlayLoadState state)
        {
            Assert.IsFalse(TutorialOverlayLoader.IsStartupReadyState(state));
        }
    }
}
