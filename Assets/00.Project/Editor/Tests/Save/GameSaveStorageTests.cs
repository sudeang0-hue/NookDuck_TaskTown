using System;
using System.IO;
using NUnit.Framework;
using TaskTown.KDH;
using TaskTown.Tutorial;

namespace TaskTown.EditorTests.Save
{
    public class GameSaveStorageTests
    {
        private string testDirectory;
        private string testSavePath;

        [SetUp]
        public void SetUp()
        {
            testDirectory = Path.Combine(
                Path.GetTempPath(),
                "TaskTownSaveTests",
                Guid.NewGuid().ToString("N"));
            testSavePath = Path.Combine(testDirectory, "gamesave-test.json");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(testDirectory))
                Directory.Delete(testDirectory, true);
        }

        [Test]
        public void LoadFromPath_파일이없으면_NotFound를반환한다()
        {
            GameSaveLoadStatus status = GameSaveStorage.LoadFromPath(
                testSavePath,
                out GameSaveData data,
                out string errorMessage);

            Assert.AreEqual(GameSaveLoadStatus.NotFound, status);
            Assert.IsNull(data);
            Assert.IsEmpty(errorMessage);
        }

        [Test]
        public void SaveAndLoad_튜토리얼진행상태를보존한다()
        {
            GameSaveData source = new GameSaveData
            {
                coins = 250,
                tutorial = new TutorialSaveData
                {
                    currentStep = TutorialStep.EarnManualCoin,
                    dialogueIndex = 3,
                    manualEarnedCoin = 75,
                    progressFlags =
                        (int)TutorialProgressFlags.TownWindowMinimized,
                    rewardFlags =
                        (int)TutorialProgressFlags.TownWindowRewardGranted
                }
            };

            bool saved = GameSaveStorage.SaveToPath(
                testSavePath,
                source,
                out string saveError);
            GameSaveLoadStatus status = GameSaveStorage.LoadFromPath(
                testSavePath,
                out GameSaveData loaded,
                out string loadError);

            Assert.IsTrue(saved, saveError);
            Assert.AreEqual(GameSaveLoadStatus.Success, status, loadError);
            Assert.AreEqual(250L, loaded.coins);
            Assert.AreEqual(TutorialStep.EarnManualCoin, loaded.tutorial.currentStep);
            Assert.AreEqual(3, loaded.tutorial.dialogueIndex);
            Assert.AreEqual(75L, loaded.tutorial.manualEarnedCoin);
            Assert.AreEqual(
                (int)TutorialProgressFlags.TownWindowMinimized,
                loaded.tutorial.progressFlags);
            Assert.AreEqual(
                (int)TutorialProgressFlags.TownWindowRewardGranted,
                loaded.tutorial.rewardFlags);
            Assert.IsFalse(loaded.tutorial.IsCompleted);
        }

        [Test]
        public void LoadFromPath_빈파일이면_Failed를반환한다()
        {
            Directory.CreateDirectory(testDirectory);
            File.WriteAllText(testSavePath, string.Empty);

            GameSaveLoadStatus status = GameSaveStorage.LoadFromPath(
                testSavePath,
                out GameSaveData data,
                out string errorMessage);

            Assert.AreEqual(GameSaveLoadStatus.Failed, status);
            Assert.IsNull(data);
            Assert.IsNotEmpty(errorMessage);
        }

        [Test]
        public void LoadFromPath_튜토리얼필드가없는저장_미완료진행을생성한다()
        {
            Directory.CreateDirectory(testDirectory);
            File.WriteAllText(testSavePath, "{\"coins\":500,\"townLevel\":3}");

            GameSaveLoadStatus status = GameSaveStorage.LoadFromPath(
                testSavePath,
                out GameSaveData loaded,
                out string errorMessage);

            Assert.AreEqual(GameSaveLoadStatus.Success, status, errorMessage);
            Assert.NotNull(loaded.tutorial);
            Assert.AreEqual(TutorialStep.IntroDialogue, loaded.tutorial.currentStep);
            Assert.IsFalse(loaded.tutorial.IsCompleted);
        }
    }
}
