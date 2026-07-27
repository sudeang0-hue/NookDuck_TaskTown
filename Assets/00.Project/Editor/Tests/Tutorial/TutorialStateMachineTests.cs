using NUnit.Framework;
using TaskTown.KDH;
using TaskTown.Tutorial;

namespace TaskTown.EditorTests.Tutorial
{
    public class TutorialStateMachineTests
    {
        [Test]
        public void DialogueCompleted_인트로단계_수동코인단계로진행한다()
        {
            TutorialStateMachine machine = CreateMachine(TutorialStep.IntroDialogue);

            bool handled = machine.TryHandleSignal(TutorialSignalType.DialogueCompleted);

            Assert.IsTrue(handled);
            Assert.AreEqual(TutorialStep.EarnManualCoin, machine.CurrentStep);
        }

        [Test]
        public void TryHandleSignal_현재단계와관계없는신호_무시한다()
        {
            TutorialStateMachine machine = CreateMachine(TutorialStep.DrawAnimal);

            bool handled = machine.TryHandleSignal(TutorialSignalType.ToolDrawn);

            Assert.IsFalse(handled);
            Assert.AreEqual(TutorialStep.DrawAnimal, machine.CurrentStep);
        }

        [Test]
        public void ManualCoinEarned_실제지급량이100이상이되면_다음단계로진행한다()
        {
            TutorialStateMachine machine = CreateMachine(TutorialStep.EarnManualCoin);

            bool firstHandled = machine.TryHandleSignal(TutorialSignalType.ManualCoinEarned, 40L);
            bool secondHandled = machine.TryHandleSignal(TutorialSignalType.ManualCoinEarned, 65L);

            Assert.IsTrue(firstHandled);
            Assert.IsTrue(secondHandled);
            Assert.AreEqual(105L, machine.ManualEarnedCoin);
            Assert.AreEqual(TutorialStep.DrawAnimal, machine.CurrentStep);
        }

        [TestCase(0L)]
        [TestCase(-1L)]
        public void ManualCoinEarned_지급량이양수가아니면_무시한다(long amount)
        {
            TutorialStateMachine machine = CreateMachine(TutorialStep.EarnManualCoin);

            bool handled = machine.TryHandleSignal(TutorialSignalType.ManualCoinEarned, amount);

            Assert.IsFalse(handled);
            Assert.AreEqual(0L, machine.ManualEarnedCoin);
            Assert.AreEqual(TutorialStep.EarnManualCoin, machine.CurrentStep);
        }

        [Test]
        public void SetPaused_일시정지중에는_진행신호와대화인덱스를무시한다()
        {
            TutorialStateMachine machine = CreateMachine(TutorialStep.IntroDialogue);
            machine.SetPaused(true);

            bool dialogueChanged = machine.TrySetDialogueIndex(1);
            bool handled = machine.TryHandleSignal(TutorialSignalType.DialogueCompleted);

            Assert.IsFalse(dialogueChanged);
            Assert.IsFalse(handled);
            Assert.AreEqual(0, machine.DialogueIndex);
            Assert.AreEqual(TutorialStep.IntroDialogue, machine.CurrentStep);
        }

        [Test]
        public void TrySetDialogueIndex_대화단계에서만저장하고_단계전환시초기화한다()
        {
            TutorialStateMachine machine = CreateMachine(TutorialStep.IntroDialogue);

            bool changed = machine.TrySetDialogueIndex(3);
            machine.TryHandleSignal(TutorialSignalType.DialogueCompleted);
            bool changedOutsideDialogue = machine.TrySetDialogueIndex(1);

            Assert.IsTrue(changed);
            Assert.AreEqual(0, machine.DialogueIndex);
            Assert.IsFalse(changedOutsideDialogue);
        }

        [Test]
        public void Constructor_저장된진행상태의복사본으로_재개한다()
        {
            TutorialSaveData saved = new TutorialSaveData
            {
                currentStep = TutorialStep.EarnManualCoin,
                manualEarnedCoin = 70L
            };

            TutorialStateMachine machine = new TutorialStateMachine(saved);
            saved.currentStep = TutorialStep.Completed;
            saved.manualEarnedCoin = 999L;

            Assert.AreEqual(TutorialStep.EarnManualCoin, machine.CurrentStep);
            Assert.AreEqual(70L, machine.ManualEarnedCoin);
        }

        [Test]
        public void 전체필수신호를순서대로처리하면_완료이벤트를한번호출한다()
        {
            TutorialStateMachine machine = CreateMachine(TutorialStep.IntroDialogue);
            int completedCount = 0;
            machine.Completed += () => completedCount++;

            Assert.IsTrue(machine.TryHandleSignal(TutorialSignalType.DialogueCompleted));
            Assert.IsTrue(machine.TryHandleSignal(TutorialSignalType.ManualCoinEarned, 100L));
            Assert.IsTrue(machine.TryHandleSignal(TutorialSignalType.AnimalDrawn));
            Assert.IsTrue(machine.TryHandleSignal(TutorialSignalType.ToolDrawn));
            Assert.IsTrue(machine.TryHandleSignal(TutorialSignalType.AnimalAssigned));
            Assert.IsTrue(machine.TryHandleSignal(TutorialSignalType.AutoProductionConfirmed));
            Assert.IsTrue(machine.TryHandleSignal(TutorialSignalType.VillageInfoOpened));
            Assert.IsTrue(machine.TryHandleSignal(TutorialSignalType.AnyUpgradePurchased));
            Assert.IsTrue(machine.TryHandleSignal(TutorialSignalType.DialogueCompleted));

            Assert.IsTrue(machine.IsCompleted);
            Assert.AreEqual(TutorialStep.Completed, machine.CurrentStep);
            Assert.AreEqual(1, completedCount);
            Assert.IsFalse(machine.TryHandleSignal(TutorialSignalType.DialogueCompleted));
            Assert.AreEqual(1, completedCount);
        }

        [Test]
        public void AnyUpgradePurchased_업그레이드단계_완료대화로진행한다()
        {
            TutorialStateMachine machine = CreateMachine(TutorialStep.UpgradeVillage);

            bool handled = machine.TryHandleSignal(TutorialSignalType.AnyUpgradePurchased);

            Assert.IsTrue(handled);
            Assert.AreEqual(TutorialStep.CompletionDialogue, machine.CurrentStep);
        }

        private static TutorialStateMachine CreateMachine(TutorialStep step)
        {
            return new TutorialStateMachine(new TutorialSaveData
            {
                currentStep = step
            });
        }
    }
}
