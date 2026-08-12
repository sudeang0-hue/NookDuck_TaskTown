using System.Reflection;
using NUnit.Framework;
using TaskTown.KDH;
using TaskTown.Tutorial;
using UnityEngine;
using UnityEngine.TestTools;

namespace TaskTown.EditorTests.Tutorial
{
    public class TutorialManagerTests
    {
        [Test]
        public void SetPaused_상태가바뀔때만PauseChanged를호출한다()
        {
            GameObject gameObject = new("TutorialManagerTest");
            TutorialManager manager = gameObject.AddComponent<TutorialManager>();
            LogAssert.Expect(
                LogType.Warning,
                "[TutorialManager] SaveManager가 없어 기본 진행 상태로 시작합니다. " +
                "현재 진행은 디스크 저장에 포함되지 않습니다.");
            manager.Initialize(null);

            int eventCount = 0;
            bool lastPaused = false;
            manager.PauseChanged += isPaused =>
            {
                eventCount++;
                lastPaused = isPaused;
            };

            manager.SetPaused(true);
            manager.SetPaused(true);
            Assert.AreEqual(1, eventCount);
            Assert.IsTrue(lastPaused);

            manager.SetPaused(false);
            Assert.AreEqual(2, eventCount);
            Assert.IsFalse(lastPaused);

            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void CollapseAndExpandTown_완료대화연속입력에도_1500코인을한번만지급한다()
        {
            GameObject coinObject = new("CoinManagerTest");
            CoinManager coinManager = coinObject.AddComponent<CoinManager>();
            FieldInfo coinInstanceField = typeof(CoinManager).GetField(
                "<Instance>k__BackingField",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(coinInstanceField);
            coinInstanceField.SetValue(null, coinManager);
            GameObject tutorialObject = new("TutorialManagerTest");
            TutorialManager manager = tutorialObject.AddComponent<TutorialManager>();
            TutorialStateMachine machine = new(new TutorialSaveData
            {
                currentStep = TutorialStep.CollapseAndExpandTown,
                progressFlags =
                    (int)TutorialProgressFlags.TownWindowGuideCompleted |
                    (int)TutorialProgressFlags.TownWindowMinimized |
                    (int)TutorialProgressFlags.TownWindowExpanded
            });

            FieldInfo stateMachineField = typeof(TutorialManager).GetField(
                "stateMachine",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(stateMachineField);
            stateMachineField.SetValue(manager, machine);

            bool first = manager.ReportSignal(TutorialSignalType.DialogueCompleted);
            bool second = manager.ReportSignal(TutorialSignalType.DialogueCompleted);

            Assert.IsTrue(first);
            Assert.IsFalse(second);
            Assert.AreEqual(TutorialManager.TownWindowCompletionReward, coinManager.Balance);
            Assert.AreEqual(TutorialStep.DrawAnimal, manager.CurrentStep);
            Assert.IsTrue(manager.IsTownWindowRewardGranted);

            coinInstanceField.SetValue(null, null);
            Object.DestroyImmediate(tutorialObject);
            Object.DestroyImmediate(coinObject);
        }

        [Test]
        public void AnimalDrawn_연속신호에도_도구뽑기용1500코인을한번만지급한다()
        {
            GameObject coinObject = new("CoinManagerTest");
            CoinManager coinManager = coinObject.AddComponent<CoinManager>();
            FieldInfo coinInstanceField = typeof(CoinManager).GetField(
                "<Instance>k__BackingField",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(coinInstanceField);
            coinInstanceField.SetValue(null, coinManager);

            GameObject tutorialObject = new("TutorialManagerTest");
            TutorialManager manager = tutorialObject.AddComponent<TutorialManager>();
            TutorialStateMachine machine = new(new TutorialSaveData
            {
                currentStep = TutorialStep.DrawAnimal
            });

            FieldInfo stateMachineField = typeof(TutorialManager).GetField(
                "stateMachine",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(stateMachineField);
            stateMachineField.SetValue(manager, machine);

            bool first = manager.ReportSignal(TutorialSignalType.AnimalDrawn);
            bool second = manager.ReportSignal(TutorialSignalType.AnimalDrawn);

            Assert.IsTrue(first);
            Assert.IsFalse(second);
            Assert.AreEqual(TutorialManager.ToolDrawCoinReward, coinManager.Balance);
            Assert.AreEqual(TutorialStep.AnimalDrawExplanation, manager.CurrentStep);
            Assert.IsTrue(manager.IsToolDrawCoinRewardGranted);

            Assert.IsTrue(manager.ReportSignal(TutorialSignalType.DialogueCompleted));
            Assert.AreEqual(TutorialStep.DrawTool, manager.CurrentStep);

            coinInstanceField.SetValue(null, null);
            Object.DestroyImmediate(tutorialObject);
            Object.DestroyImmediate(coinObject);
        }

        [Test]
        public void TrySkipTutorial_연속호출에도_완료이벤트를한번호출한다()
        {
            GameObject gameObject = new("TutorialManagerSkipTest");
            TutorialManager manager = gameObject.AddComponent<TutorialManager>();
            LogAssert.Expect(
                LogType.Warning,
                "[TutorialManager] SaveManager가 없어 기본 진행 상태로 시작합니다. " +
                "현재 진행은 디스크 저장에 포함되지 않습니다.");
            manager.Initialize(null);
            int completedCount = 0;
            manager.TutorialCompleted += () => completedCount++;

            bool first = manager.TrySkipTutorial();
            bool second = manager.TrySkipTutorial();

            Assert.IsTrue(first);
            Assert.IsFalse(second);
            Assert.IsTrue(manager.IsCompleted);
            Assert.AreEqual(1, completedCount);

            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void TryRestartTutorial_재보기진행으로_인트로부터다시시작한다()
        {
            GameObject gameObject = new("TutorialManagerRestartTest");
            TutorialManager manager = gameObject.AddComponent<TutorialManager>();
            LogAssert.Expect(
                LogType.Warning,
                "[TutorialManager] SaveManager가 없어 기본 진행 상태로 시작합니다. " +
                "현재 진행은 디스크 저장에 포함되지 않습니다.");
            manager.Initialize(null);

            TutorialSaveData replayProgress = new TutorialSaveData
            {
                currentStep = TutorialStep.Completed,
                rewardFlags =
                    (int)TutorialProgressFlags.TownWindowRewardGranted |
                    (int)TutorialProgressFlags.ToolDrawCoinRewardGranted
            }.CreateReplayProgress();

            int progressChangedCount = 0;
            int stepChangedCount = 0;
            int restartedCount = 0;
            manager.ProgressChanged += _ => progressChangedCount++;
            manager.StepChanged += (_, _) => stepChangedCount++;
            manager.TutorialRestarted += () => restartedCount++;

            bool restarted = manager.TryRestartTutorial(replayProgress);

            Assert.IsTrue(restarted);
            Assert.AreEqual(TutorialStep.IntroDialogue, manager.CurrentStep);
            Assert.AreEqual(0L, manager.ManualEarnedCoin);
            Assert.IsTrue(manager.IsTownWindowRewardGranted);
            Assert.IsTrue(manager.IsToolDrawCoinRewardGranted);
            Assert.AreEqual(1, progressChangedCount);
            Assert.AreEqual(0, stepChangedCount);
            Assert.AreEqual(1, restartedCount);

            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void 다시보기_이미받은1500코인보상은_추가지급없이단계만진행한다()
        {
            GameObject coinObject = new("CoinManagerReplayTest");
            CoinManager coinManager = coinObject.AddComponent<CoinManager>();
            FieldInfo coinInstanceField = typeof(CoinManager).GetField(
                "<Instance>k__BackingField",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(coinInstanceField);
            coinInstanceField.SetValue(null, coinManager);

            GameObject tutorialObject = new("TutorialManagerReplayTest");
            TutorialManager manager = tutorialObject.AddComponent<TutorialManager>();
            TutorialStateMachine machine = new(new TutorialSaveData
            {
                currentStep = TutorialStep.CollapseAndExpandTown,
                progressFlags =
                    (int)TutorialProgressFlags.TownWindowGuideCompleted |
                    (int)TutorialProgressFlags.TownWindowMinimized |
                    (int)TutorialProgressFlags.TownWindowExpanded,
                rewardFlags =
                    (int)TutorialProgressFlags.TownWindowRewardGranted |
                    (int)TutorialProgressFlags.ToolDrawCoinRewardGranted
            });

            FieldInfo stateMachineField = typeof(TutorialManager).GetField(
                "stateMachine",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(stateMachineField);
            stateMachineField.SetValue(manager, machine);

            Assert.IsTrue(manager.ReportSignal(TutorialSignalType.DialogueCompleted));
            Assert.AreEqual(0L, coinManager.Balance);
            Assert.AreEqual(TutorialStep.DrawAnimal, manager.CurrentStep);

            Assert.IsTrue(manager.ReportSignal(TutorialSignalType.AnimalDrawn));
            Assert.AreEqual(0L, coinManager.Balance);
            Assert.AreEqual(TutorialStep.AnimalDrawExplanation, manager.CurrentStep);

            coinInstanceField.SetValue(null, null);
            Object.DestroyImmediate(tutorialObject);
            Object.DestroyImmediate(coinObject);
        }
    }
}
