using NUnit.Framework;
using TaskTown.KDH;
using TaskTown.Tutorial;
using UnityEngine;

namespace TaskTown.EditorTests.Save
{
    public class GameSaveDataTests
    {
        [Test]
        public void Normalize_튜토리얼필드가없는이전저장_기본미완료상태를생성한다()
        {
            const string legacyJson = "{\"coins\":100,\"townLevel\":2}";

            GameSaveData data = JsonUtility.FromJson<GameSaveData>(legacyJson);
            data.tutorial = null;

            data.Normalize();

            Assert.NotNull(data.tutorial);
            Assert.AreEqual(TutorialSaveData.CurrentVersion, data.tutorial.version);
            Assert.AreEqual(TutorialStep.IntroDialogue, data.tutorial.currentStep);
            Assert.IsFalse(data.tutorial.IsCompleted);
            Assert.AreEqual(0L, data.tutorial.manualEarnedCoin);
            Assert.AreEqual(0L, data.tutorial.autoProductionEarnedCoin);
        }

        [Test]
        public void Normalize_손상된튜토리얼진행값_안전한기본값으로보정한다()
        {
            GameSaveData data = new GameSaveData
            {
                animals = null,
                tools = null,
                tutorial = new TutorialSaveData
                {
                    version = 0,
                    currentStep = (TutorialStep)999,
                    dialogueIndex = -1,
                    manualEarnedCoin = -100,
                    autoProductionEarnedCoin = -50,
                    progressFlags = -1,
                    rewardFlags = -1
                }
            };

            data.Normalize();

            Assert.NotNull(data.animals);
            Assert.NotNull(data.tools);
            Assert.AreEqual(TutorialSaveData.CurrentVersion, data.tutorial.version);
            Assert.AreEqual(TutorialStep.IntroDialogue, data.tutorial.currentStep);
            Assert.AreEqual(0, data.tutorial.dialogueIndex);
            Assert.AreEqual(0L, data.tutorial.manualEarnedCoin);
            Assert.AreEqual(0L, data.tutorial.autoProductionEarnedCoin);
            Assert.AreEqual(0, data.tutorial.progressFlags);
            Assert.AreEqual(0, data.tutorial.rewardFlags);
        }

        [Test]
        public void IsCompleted_완료단계이면_참을반환한다()
        {
            TutorialSaveData progress = new TutorialSaveData
            {
                currentStep = TutorialStep.Completed
            };

            Assert.IsTrue(progress.IsCompleted);
        }

        [Test]
        public void Copy_원본과독립된진행데이터를반환한다()
        {
            TutorialSaveData original = new TutorialSaveData
            {
                currentStep = TutorialStep.EarnManualCoin,
                manualEarnedCoin = 45,
                autoProductionEarnedCoin = 20
            };

            TutorialSaveData copy = original.Copy();
            copy.manualEarnedCoin = 90;
            copy.autoProductionEarnedCoin = 40;

            Assert.AreEqual(45L, original.manualEarnedCoin);
            Assert.AreEqual(90L, copy.manualEarnedCoin);
            Assert.AreEqual(20L, original.autoProductionEarnedCoin);
            Assert.AreEqual(40L, copy.autoProductionEarnedCoin);
        }

        [Test]
        public void Normalize_축소확장보상지급플래그가있으면_동물뽑기로복구한다()
        {
            TutorialSaveData progress = new TutorialSaveData
            {
                version = 3,
                currentStep = TutorialStep.CollapseAndExpandTown,
                dialogueIndex = 3,
                rewardFlags =
                    (int)TutorialProgressFlags.TownWindowRewardGranted
            };

            progress.Normalize();

            Assert.AreEqual(TutorialStep.DrawAnimal, progress.currentStep);
            Assert.AreEqual(0, progress.dialogueIndex);
        }

        [Test]
        public void Normalize_도구뽑기보상지급플래그가있으면_도구뽑기로복구한다()
        {
            TutorialSaveData progress = new TutorialSaveData
            {
                version = 3,
                currentStep = TutorialStep.DrawAnimal,
                dialogueIndex = 2,
                rewardFlags =
                    (int)TutorialProgressFlags.ToolDrawCoinRewardGranted
            };

            progress.Normalize();

            Assert.AreEqual(TutorialStep.AnimalDrawExplanation, progress.currentStep);
            Assert.AreEqual(0, progress.dialogueIndex);
        }

        [Test]
        public void Normalize_버전3혼합플래그_진행플래그와일회성보상으로분리한다()
        {
            TutorialSaveData progress = new TutorialSaveData
            {
                version = 3,
                currentStep = TutorialStep.Completed,
                rewardFlags =
                    (int)TutorialProgressFlags.TownWindowMinimized |
                    (int)TutorialProgressFlags.TownWindowRewardGranted
            };

            progress.Normalize();

            Assert.AreEqual(TutorialSaveData.CurrentVersion, progress.version);
            Assert.AreEqual(
                (int)TutorialProgressFlags.TownWindowMinimized,
                progress.progressFlags);
            Assert.AreEqual(
                (int)TutorialProgressFlags.TownWindowRewardGranted,
                progress.rewardFlags);
        }

        [Test]
        public void CreateReplayProgress_현재진행은초기화하고_일회성보상은보존한다()
        {
            TutorialSaveData completed = new TutorialSaveData
            {
                currentStep = TutorialStep.Completed,
                dialogueIndex = 4,
                manualEarnedCoin = 125L,
                autoProductionEarnedCoin = 60L,
                progressFlags =
                    (int)TutorialProgressFlags.TownWindowGuideCompleted |
                    (int)TutorialProgressFlags.TownWindowMinimized |
                    (int)TutorialProgressFlags.TownWindowExpanded,
                rewardFlags =
                    (int)TutorialProgressFlags.TownWindowRewardGranted |
                    (int)TutorialProgressFlags.ToolDrawCoinRewardGranted
            };

            TutorialSaveData replay = completed.CreateReplayProgress();

            Assert.AreEqual(TutorialStep.IntroDialogue, replay.currentStep);
            Assert.AreEqual(0, replay.dialogueIndex);
            Assert.AreEqual(0L, replay.manualEarnedCoin);
            Assert.AreEqual(0L, replay.autoProductionEarnedCoin);
            Assert.AreEqual(0, replay.progressFlags);
            Assert.AreEqual(completed.rewardFlags, replay.rewardFlags);
            Assert.IsTrue(replay.HasProgressFlag(
                TutorialProgressFlags.TownWindowRewardGranted));
            Assert.IsTrue(replay.HasProgressFlag(
                TutorialProgressFlags.ToolDrawCoinRewardGranted));
        }
    }
}
