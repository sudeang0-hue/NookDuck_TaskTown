using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace TaskTown.Tutorial
{
    /// <summary>
    /// 튜토리얼 문구가 한글 단어 중간에서 어색하게 줄바꿈되지 않도록,
    /// TMP_Text의 실제 표시 폭을 기준으로 공백 위치에 줄바꿈을 미리 삽입합니다.
    /// </summary>
    public static class TutorialTextLineBreakUtility
    {
        private const float WidthEpsilon = 0.5f;

        public static string ApplyWordLineBreaks(TMP_Text target, string value)
        {
            string safeValue = value ?? string.Empty;
            if (target == null || string.IsNullOrWhiteSpace(safeValue))
                return safeValue;

            float maxLineWidth = GetAvailableLineWidth(target);
            if (maxLineWidth <= 0f)
                return safeValue;

            return ApplyWordLineBreaks(
                safeValue,
                maxLineWidth,
                candidate => target
                    .GetPreferredValues(
                        candidate,
                        float.PositiveInfinity,
                        float.PositiveInfinity)
                    .x);
        }

        public static string ApplyWordLineBreaks(
            string value,
            float maxLineWidth,
            Func<string, float> measureWidth)
        {
            string safeValue = value ?? string.Empty;
            if (string.IsNullOrWhiteSpace(safeValue) ||
                maxLineWidth <= 0f ||
                measureWidth == null)
            {
                return safeValue;
            }

            string normalized = safeValue
                .Replace("\r\n", "\n")
                .Replace('\r', '\n');
            string[] sourceLines = normalized.Split('\n');
            StringBuilder builder = new();

            for (int i = 0; i < sourceLines.Length; i++)
            {
                if (i > 0)
                    builder.Append('\n');

                builder.Append(WrapSingleLine(
                    sourceLines[i],
                    maxLineWidth,
                    measureWidth));
            }

            return builder.ToString();
        }

        private static string WrapSingleLine(
            string sourceLine,
            float maxLineWidth,
            Func<string, float> measureWidth)
        {
            if (string.IsNullOrWhiteSpace(sourceLine))
                return string.Empty;

            List<string> words = SplitWords(sourceLine);
            if (words.Count <= 1)
                return sourceLine.Trim();

            StringBuilder result = new();
            StringBuilder currentLine = new();

            foreach (string word in words)
            {
                if (currentLine.Length == 0)
                {
                    currentLine.Append(word);
                    continue;
                }

                string candidate = currentLine + " " + word;
                if (Fits(candidate, maxLineWidth, measureWidth))
                {
                    currentLine.Append(' ');
                    currentLine.Append(word);
                    continue;
                }

                if (result.Length > 0)
                    result.Append('\n');

                result.Append(currentLine);
                currentLine.Clear();
                currentLine.Append(word);
            }

            if (currentLine.Length > 0)
            {
                if (result.Length > 0)
                    result.Append('\n');

                result.Append(currentLine);
            }

            return result.ToString();
        }

        private static List<string> SplitWords(string sourceLine)
        {
            List<string> words = new();
            StringBuilder word = new();

            foreach (char character in sourceLine)
            {
                if (char.IsWhiteSpace(character))
                {
                    FlushWord(words, word);
                    continue;
                }

                word.Append(character);
            }

            FlushWord(words, word);
            return words;
        }

        private static void FlushWord(List<string> words, StringBuilder word)
        {
            if (word.Length == 0)
                return;

            words.Add(word.ToString());
            word.Clear();
        }

        private static bool Fits(
            string text,
            float maxLineWidth,
            Func<string, float> measureWidth)
        {
            float width = measureWidth(text);
            return float.IsNaN(width) ||
                   width <= maxLineWidth + WidthEpsilon;
        }

        private static float GetAvailableLineWidth(TMP_Text target)
        {
            RectTransform rectTransform = target.rectTransform;
            if (rectTransform == null)
                return 0f;

            Vector4 margin = target.margin;
            return rectTransform.rect.width - margin.x - margin.z;
        }
    }
}
