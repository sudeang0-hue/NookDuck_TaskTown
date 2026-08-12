using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TaskTown.Tutorial;

namespace TaskTown.EditorTests.Tutorial
{
    public class TutorialTextLineBreakUtilityTests
    {
        [Test]
        public void ApplyWordLineBreaks_본문을_공백기준으로만_나눈다()
        {
            const string source =
                "마을 재건을 위한 첫 업무를 시작해볼까요";

            string result = TutorialTextLineBreakUtility.ApplyWordLineBreaks(
                source,
                8f,
                MeasureByLength);

            StringAssert.Contains("\n", result);
            Assert.AreEqual(NormalizeSpaces(source), JoinWrappedLines(result));
        }

        [Test]
        public void ApplyWordLineBreaks_목표문구도_단어중간을_자르지않는다()
        {
            const string source =
                "타이핑과 클릭으로 Town Coin 100 획득";

            string result = TutorialTextLineBreakUtility.ApplyWordLineBreaks(
                source,
                10f,
                MeasureByLength);

            StringAssert.Contains("\n", result);
            Assert.AreEqual(NormalizeSpaces(source), JoinWrappedLines(result));
        }

        [Test]
        public void ApplyWordLineBreaks_한단어가폭보다길면_단어를그대로둔다()
        {
            const string source = "시작해볼까요";

            string result = TutorialTextLineBreakUtility.ApplyWordLineBreaks(
                source,
                2f,
                MeasureByLength);

            Assert.AreEqual(source, result);
        }

        [Test]
        public void ApplyWordLineBreaks_기존수동줄바꿈을_유지한다()
        {
            const string source =
                "첫 번째 안내 문구입니다\n두 번째 안내 문구입니다";

            string result = TutorialTextLineBreakUtility.ApplyWordLineBreaks(
                source,
                8f,
                MeasureByLength);

            StringAssert.Contains("\n", result);
            Assert.AreEqual(NormalizeSpaces(source), JoinWrappedLines(result));
        }

        private static float MeasureByLength(string value)
        {
            return value?.Length ?? 0;
        }

        private static string JoinWrappedLines(string value)
        {
            return NormalizeSpaces(value.Replace('\n', ' '));
        }

        private static string NormalizeSpaces(string value)
        {
            return Regex.Replace(value ?? string.Empty, "\\s+", " ").Trim();
        }
    }
}
