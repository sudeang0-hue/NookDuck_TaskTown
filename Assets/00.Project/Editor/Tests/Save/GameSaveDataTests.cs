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
                manualEarnedCoin = 45
            };

            TutorialSaveData copy = original.Copy();
            copy.manualEarnedCoin = 90;

            Assert.AreEqual(45L, original.manualEarnedCoin);
            Assert.AreEqual(90L, copy.manualEarnedCoin);
        }
    }
}
