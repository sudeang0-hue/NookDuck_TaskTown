using System.Collections;
using System.Reflection;
using DG.Tweening;
using NUnit.Framework;
using TaskTown.Tutorial;
using UnityEngine;
using UnityEngine.UI;

namespace TaskTown.EditorTests.Tutorial
{
    public class TutorialButtonHighlighterTests
    {
        [Test]
        public void Highlight와Clear_버튼ScaleTween을생성하고원래크기로복구한다()
        {
            GameObject root = new("TutorialButtonHighlighterTest");
            TutorialButtonHighlighter highlighter =
                root.AddComponent<TutorialButtonHighlighter>();
            GameObject buttonObject = new(
                "TargetButton",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            Vector3 originalScale = new(0.8f, 0.8f, 0.8f);
            buttonObject.transform.localScale = originalScale;

            highlighter.Highlight(buttonObject.GetComponent<Button>());

            Tween tween = DOTween.TweensByTarget(buttonObject.transform)?[0];
            Assert.IsNotNull(tween);
            Assert.IsTrue(tween.IsActive());

            highlighter.Clear();

            Assert.AreEqual(0, GetEntryCount(highlighter));
            Assert.AreEqual(originalScale, buttonObject.transform.localScale);

            Object.DestroyImmediate(buttonObject);
            Object.DestroyImmediate(root);
        }

        private static int GetEntryCount(TutorialButtonHighlighter highlighter)
        {
            FieldInfo field = typeof(TutorialButtonHighlighter).GetField(
                "entries",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field);
            ICollection entries = field.GetValue(highlighter) as ICollection;
            Assert.IsNotNull(entries);
            return entries.Count;
        }
    }
}
