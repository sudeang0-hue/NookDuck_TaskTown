using System;
using System.Collections.Generic;
using NUnit.Framework;
using TaskTown.Tutorial;
using UnityEditor;

namespace TaskTown.EditorTests.Tutorial
{
    public class TutorialConfigTests
    {
        private const string ConfigPath =
            "Assets/00.Project/03.ScriptableObjects/Tutorial/TutorialConfig.asset";

        [Test]
        public void ConfigAsset_완료를제외한모든단계의문구가있다()
        {
            TutorialConfigSO config = LoadConfig();
            HashSet<TutorialStep> foundSteps = new();

            foreach (TutorialStepContent content in config.Steps)
            {
                Assert.IsNotNull(content);
                Assert.IsTrue(foundSteps.Add(content.Step),
                    $"중복 단계가 있습니다: {content.Step}");
                Assert.Greater(content.Messages.Count, 0,
                    $"표시 문구가 없습니다: {content.Step}");

                foreach (string message in content.Messages)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(message),
                        $"빈 표시 문구가 있습니다: {content.Step}");
                }

                foreach (string message in content.CompletionMessages)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(message),
                        $"빈 완료 문구가 있습니다: {content.Step}");
                }
            }

            foreach (TutorialStep step in Enum.GetValues(typeof(TutorialStep)))
            {
                if (step == TutorialStep.Completed)
                    continue;

                Assert.IsTrue(foundSteps.Contains(step),
                    $"설정이 누락된 단계입니다: {step}");
            }
        }

        [Test]
        public void ConfigAsset_인트로9개_완료대화1개를포함한다()
        {
            TutorialConfigSO config = LoadConfig();

            Assert.AreEqual(9, config.GetMessageCount(TutorialStep.IntroDialogue));
            Assert.AreEqual(1, config.GetMessageCount(TutorialStep.CompletionDialogue));
        }

        [TestCase(-10, 0)]
        [TestCase(0, 0)]
        [TestCase(3, 3)]
        [TestCase(99, 8)]
        public void ClampDialogueIndex_인트로범위를벗어나면_유효범위로보정한다(
            int input,
            int expected)
        {
            TutorialConfigSO config = LoadConfig();

            int result = config.ClampDialogueIndex(TutorialStep.IntroDialogue, input);

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void ConfigAsset_수동코인단계는100코인진행도를표시한다()
        {
            TutorialConfigSO config = LoadConfig();

            Assert.IsTrue(config.TryGetStepContent(
                TutorialStep.EarnManualCoin,
                out TutorialStepContent content));
            Assert.AreEqual(
                TutorialProgressDisplayType.ManualCoin,
                content.ProgressDisplayType);
            StringAssert.Contains("100", content.ObjectiveText);
        }

        [Test]
        public void ConfigAsset_축소확장단계는_안내3개와완료4개를포함한다()
        {
            TutorialConfigSO config = LoadConfig();

            Assert.IsTrue(config.TryGetStepContent(
                TutorialStep.CollapseAndExpandTown,
                out TutorialStepContent content));
            Assert.AreEqual(3, content.Messages.Count);
            Assert.AreEqual(4, content.CompletionMessages.Count);
            Assert.AreEqual(
                "타운을 축소한 뒤, 다시 확장 화면으로 돌아와보세요!",
                content.ObjectiveText);
        }

        [Test]
        public void ConfigAsset_자동생산단계는50코인진행도를표시한다()
        {
            TutorialConfigSO config = LoadConfig();

            Assert.IsTrue(config.TryGetStepContent(
                TutorialStep.ConfirmAutoProduction,
                out TutorialStepContent content));
            Assert.AreEqual(
                TutorialProgressDisplayType.AutoProductionCoin,
                content.ProgressDisplayType);
            StringAssert.Contains("50", content.ObjectiveText);
        }

        [Test]
        public void ConfigAsset_동물뽑기단계는_지급코인사용을안내한다()
        {
            TutorialConfigSO config = LoadConfig();

            Assert.IsTrue(config.TryGetStepContent(
                TutorialStep.DrawAnimal,
                out TutorialStepContent content));
            Assert.AreEqual(
                "지급받은 코인으로 마을에 함께할 동물을 불러보세요!",
                content.ObjectiveText);
        }

        [Test]
        public void ConfigAsset_클릭진행은인트로와완료대화에만허용한다()
        {
            TutorialConfigSO config = LoadConfig();

            foreach (TutorialStepContent content in config.Steps)
            {
                bool expected = content.Step == TutorialStep.IntroDialogue ||
                                content.Step == TutorialStep.CompletionDialogue ||
                                content.Step == TutorialStep.CollapseAndExpandTown;
                Assert.AreEqual(expected, content.AllowClickAdvance,
                    $"클릭 진행 설정이 잘못되었습니다: {content.Step}");
            }
        }

        private static TutorialConfigSO LoadConfig()
        {
            TutorialConfigSO config = AssetDatabase.LoadAssetAtPath<TutorialConfigSO>(
                ConfigPath);
            Assert.IsNotNull(config, $"튜토리얼 설정 에셋이 없습니다: {ConfigPath}");
            return config;
        }
    }
}
