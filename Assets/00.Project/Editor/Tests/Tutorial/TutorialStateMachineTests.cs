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
        public void ManualCoinEarned_실제지급량이100이상이되면_축소확장단계로진행한다()
        {
            TutorialStateMachine machine = CreateMachine(TutorialStep.EarnManualCoin);

            bool firstHandled = machine.TryHandleSignal(TutorialSignalType.ManualCoinEarned, 40L);
            bool secondHandled = machine.TryHandleSignal(TutorialSignalType.ManualCoinEarned, 65L);

            Assert.IsTrue(firstHandled);
            Assert.IsTrue(secondHandled);
            Assert.AreEqual(105L, machine.ManualEarnedCoin);
            Assert.AreEqual(TutorialStep.CollapseAndExpandTown, machine.CurrentStep);
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
                manualEarnedCoin = 70L,
                autoProductionEarnedCoin = 20L
            };

            TutorialStateMachine machine = new TutorialStateMachine(saved);
            saved.currentStep = TutorialStep.Completed;
            saved.manualEarnedCoin = 999L;

            Assert.AreEqual(TutorialStep.EarnManualCoin, machine.CurrentStep);
            Assert.AreEqual(70L, machine.ManualEarnedCoin);
            Assert.AreEqual(20L, machine.AutoProductionEarnedCoin);
        }

        [Test]
        public void CollapseAndExpandTown_하나의단계에서_안내축소확장을순서대로저장한다()
        {
            TutorialStateMachine machine = CreateMachine(
                TutorialStep.CollapseAndExpandTown);

            Assert.IsTrue(machine.TrySetDialogueIndex(2));
            Assert.IsTrue(machine.TryHandleSignal(TutorialSignalType.DialogueCompleted));
            Assert.IsTrue(machine.IsTownWindowGuideCompleted);
            Assert.AreEqual(0, machine.DialogueIndex);

            Assert.IsTrue(machine.TryHandleSignal(TutorialSignalType.TownWindowMinimized));
            Assert.IsTrue(machine.IsTownWindowMinimized);

            machine.SetPaused(true);
            Assert.IsTrue(machine.TryHandleSignal(TutorialSignalType.TownWindowExpanded));
            Assert.IsTrue(machine.IsTownWindowExpanded);
            Assert.AreEqual(0, machine.DialogueIndex);
            Assert.AreEqual(TutorialStep.CollapseAndExpandTown, machine.CurrentStep);
        }

        [Test]
        public void CollapseAndExpandTown_보상완료는한번만확정하고_동물뽑기로진행한다()
        {
            TutorialSaveData progress = new TutorialSaveData
            {
                currentStep = TutorialStep.CollapseAndExpandTown,
                progressFlags =
                    (int)TutorialProgressFlags.TownWindowGuideCompleted |
                    (int)TutorialProgressFlags.TownWindowMinimized |
                    (int)TutorialProgressFlags.TownWindowExpanded
            };
            TutorialStateMachine machine = new(progress);

            bool first = machine.TryCompleteTownWindowReward();
            bool second = machine.TryCompleteTownWindowReward();

            Assert.IsTrue(first);
            Assert.IsFalse(second);
            Assert.IsTrue(machine.IsTownWindowRewardGranted);
            Assert.AreEqual(TutorialStep.DrawAnimal, machine.CurrentStep);
        }

        [Test]
        public void AnimalDrawn_도구뽑기보상은한번만확정하고_도구뽑기로진행한다()
        {
            TutorialStateMachine machine = CreateMachine(TutorialStep.DrawAnimal);

            bool first = machine.TryHandleSignal(TutorialSignalType.AnimalDrawn);
            bool second = machine.TryCompleteAnimalDrawReward();

            Assert.IsTrue(first);
            Assert.IsFalse(second);
            Assert.IsTrue(machine.IsToolDrawCoinRewardGranted);
            Assert.AreEqual(TutorialStep.AnimalDrawExplanation, machine.CurrentStep);
        }

        [Test]
        public void AutoProductionConfirmed_자동생산누적50에도달하면_다음단계로진행한다()
        {
            TutorialStateMachine machine = CreateMachine(
                TutorialStep.ConfirmAutoProduction);

            Assert.IsTrue(machine.TryHandleSignal(
                TutorialSignalType.AutoProductionConfirmed,
                20L));
            Assert.IsTrue(machine.TryHandleSignal(
                TutorialSignalType.AutoProductionConfirmed,
                29L));
            Assert.AreEqual(49L, machine.AutoProductionEarnedCoin);
            Assert.AreEqual(TutorialStep.ConfirmAutoProduction, machine.CurrentStep);

            Assert.IsTrue(machine.TryHandleSignal(
                TutorialSignalType.AutoProductionConfirmed,
                1L));
            Assert.AreEqual(50L, machine.AutoProductionEarnedCoin);
            Assert.AreEqual(TutorialStep.VillagePlacementExplanation, machine.CurrentStep);
        }

        [Test]
        public void VillagePlacement_안내후실제배치변경신호로마을정보단계에진입한다()
        {
            TutorialStateMachine machine = CreateMachine(
                TutorialStep.VillagePlacementExplanation);

            Assert.IsTrue(machine.TryHandleSignal(TutorialSignalType.DialogueCompleted));
            Assert.AreEqual(TutorialStep.PlaceAnimalInVillage, machine.CurrentStep);

            Assert.IsTrue(machine.TryHandleSignal(TutorialSignalType.VillageAnimalPlaced));
            Assert.AreEqual(TutorialStep.OpenVillageInfo, machine.CurrentStep);
        }

        [Test]
        public void 전체필수신호를순서대로처리하면_완료이벤트를한번호출한다()
        {
            TutorialStateMachine machine = CreateMachine(TutorialStep.IntroDialogue);
            int completedCount = 0;
            machine.Completed += () => completedCount++;

            Assert.IsTrue(machine.TryHandleSignal(TutorialSignalType.DialogueCompleted));
            Assert.IsTrue(machine.TryHandleSignal(TutorialSignalType.ManualCoinEarned, 100L));
            Assert.IsTrue(machine.TryHandleSignal(TutorialSignalType.DialogueCompleted));
            Assert.IsTrue(machine.TryHandleSignal(TutorialSignalType.TownWindowMinimized));
            machine.SetPaused(true);
            Assert.IsTrue(machine.TryHandleSignal(TutorialSignalType.TownWindowExpanded));
            machine.SetPaused(false);
            Assert.IsTrue(machine.TryCompleteTownWindowReward());
            Assert.IsTrue(machine.TryHandleSignal(TutorialSignalType.AnimalDrawn));
            Assert.IsTrue(machine.IsToolDrawCoinRewardGranted);
            Assert.IsTrue(machine.TryHandleSignal(TutorialSignalType.DialogueCompleted));
            Assert.IsTrue(machine.TryHandleSignal(TutorialSignalType.ToolDrawn));
            Assert.IsTrue(machine.TryHandleSignal(TutorialSignalType.AnimalAssigned));
            Assert.IsTrue(machine.TryHandleSignal(
                TutorialSignalType.AutoProductionConfirmed,
                TutorialStateMachine.AutoProductionCoinTarget));
            Assert.IsTrue(machine.TryHandleSignal(TutorialSignalType.DialogueCompleted));
            Assert.IsTrue(machine.TryHandleSignal(TutorialSignalType.VillageAnimalPlaced));
            Assert.IsTrue(machine.TryHandleSignal(TutorialSignalType.VillageInfoOpened));
            Assert.IsTrue(machine.TryHandleSignal(TutorialSignalType.DialogueCompleted));
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

        [Test]
        public void TrySkipTutorial_일시정지중연속호출에도_보상없이한번호완료한다()
        {
            TutorialStateMachine machine = new(new TutorialSaveData
            {
                currentStep = TutorialStep.CollapseAndExpandTown,
                manualEarnedCoin = 75L,
                progressFlags =
                    (int)TutorialProgressFlags.TownWindowGuideCompleted |
                    (int)TutorialProgressFlags.TownWindowMinimized |
                    (int)TutorialProgressFlags.TownWindowExpanded
            });
            int progressChangedCount = 0;
            int stepChangedCount = 0;
            int completedCount = 0;
            machine.ProgressChanged += _ => progressChangedCount++;
            machine.StepChanged += (_, _) => stepChangedCount++;
            machine.Completed += () => completedCount++;
            machine.SetPaused(true);

            bool first = machine.TrySkipTutorial();
            bool second = machine.TrySkipTutorial();

            Assert.IsTrue(first);
            Assert.IsFalse(second);
            Assert.IsTrue(machine.IsCompleted);
            Assert.AreEqual(TutorialStep.Completed, machine.CurrentStep);
            Assert.AreEqual(75L, machine.ManualEarnedCoin);
            Assert.IsFalse(machine.IsTownWindowRewardGranted);
            Assert.AreEqual(1, progressChangedCount);
            Assert.AreEqual(1, stepChangedCount);
            Assert.AreEqual(1, completedCount);
        }

        [Test]
        public void Normalize_동물뽑기보상직후저장은결과설명단계에서재개한다()
        {
            TutorialSaveData progress = new TutorialSaveData
            {
                version = 2,
                currentStep = TutorialStep.DrawAnimal,
                rewardFlags = (int)TutorialProgressFlags.ToolDrawCoinRewardGranted
            };

            progress.Normalize();

            Assert.AreEqual(TutorialSaveData.CurrentVersion, progress.version);
            Assert.AreEqual(TutorialStep.AnimalDrawExplanation, progress.currentStep);
            Assert.AreEqual(0, progress.dialogueIndex);
        }

        [Test]
        public void 다시보기_이미받은보상단계는_보상플래그를유지하고정상진행한다()
        {
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

            Assert.IsTrue(machine.IsTownWindowStepCompletionReady);
            Assert.IsFalse(machine.IsTownWindowRewardReady);
            Assert.IsTrue(machine.TryCompleteTownWindowReward());
            Assert.AreEqual(TutorialStep.DrawAnimal, machine.CurrentStep);

            Assert.IsTrue(machine.IsAnimalDrawStepCompletionReady);
            Assert.IsFalse(machine.IsToolDrawCoinRewardReady);
            Assert.IsTrue(machine.TryHandleSignal(TutorialSignalType.AnimalDrawn));
            Assert.AreEqual(TutorialStep.AnimalDrawExplanation, machine.CurrentStep);
            Assert.IsTrue(machine.IsTownWindowRewardGranted);
            Assert.IsTrue(machine.IsToolDrawCoinRewardGranted);
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
