using NUnit.Framework;
using TaskTown.KDH;
using TaskTown.Tutorial;

namespace TaskTown.EditorTests.Tutorial
{
    public class TutorialOverlayLoaderTests
    {
        [Test]
        public void ShouldLoad_저장데이터가없으면_로드한다()
        {
            Assert.IsTrue(TutorialOverlayLoader.ShouldLoad(null));
        }

        [Test]
        public void ShouldLoad_미완료진행이면_로드한다()
        {
            TutorialSaveData progress = TutorialSaveData.CreateDefault();

            Assert.IsTrue(TutorialOverlayLoader.ShouldLoad(progress));
        }

        [Test]
        public void ShouldLoad_완료진행이면_로드하지않는다()
        {
            TutorialSaveData progress = TutorialSaveData.CreateDefault();
            progress.currentStep = TutorialStep.Completed;

            Assert.IsFalse(TutorialOverlayLoader.ShouldLoad(progress));
        }
    }
}
