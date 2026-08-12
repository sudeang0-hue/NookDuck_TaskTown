using NUnit.Framework;
using TaskTown.Tutorial;
using UnityEditor;
using UnityEngine;

namespace TaskTown.EditorTests.Tutorial
{
    public class TutorialBubbleLayoutTests
    {
        [Test]
        public void Left배치를대칭생성하면_Right에서초상화와말풍선이반전된다()
        {
            GameObject root = new(
                "TutorialLayoutTest",
                typeof(RectTransform),
                typeof(TutorialBubbleView));
            GameObject portraitObject = CreateRect("Portrait", root.transform);
            GameObject bubbleObject = CreateRect("Bubble", root.transform);
            GameObject backgroundObject = CreateRect(
                "Background",
                bubbleObject.transform);
            GameObject borderObject = CreateRect("Border", root.transform);

            try
            {
                RectTransform rootRect = root.GetComponent<RectTransform>();
                RectTransform portrait =
                    portraitObject.GetComponent<RectTransform>();
                RectTransform bubble = bubbleObject.GetComponent<RectTransform>();
                RectTransform background =
                    backgroundObject.GetComponent<RectTransform>();
                RectTransform border = borderObject.GetComponent<RectTransform>();

                SetLayout(rootRect, Vector2.up, Vector2.zero, new Vector2(80f, -498f));
                SetLayout(portrait, Vector2.zero, Vector2.zero, new Vector2(16f, 0f));
                SetLayout(bubble, Vector2.zero, Vector2.zero, new Vector2(202f, 146f));
                SetLayout(
                    background,
                    Vector2.one * 0.5f,
                    Vector2.one * 0.5f,
                    new Vector2(-22f, 0f));
                SetLayout(border, Vector2.zero, Vector2.zero, new Vector2(164f, 179f));

                TutorialBubbleView view = root.GetComponent<TutorialBubbleView>();
                SerializedObject serialized = new(view);
                serialized.FindProperty("layoutRoot").objectReferenceValue = rootRect;
                serialized.FindProperty("portraitRoot").objectReferenceValue = portrait;
                serialized.FindProperty("bubbleRoot").objectReferenceValue = bubble;
                serialized.FindProperty("bubbleBackground").objectReferenceValue = background;
                serialized.FindProperty("bubbleBorder").objectReferenceValue = border;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                view.CaptureCurrentLayout(TutorialSpeakerSide.Left);
                view.CreateMirroredRightLayout();
                view.ApplySpeakerLayout(
                    TutorialSpeakerSide.Right,
                    new Vector2(10f, 5f));

                AssertVector(Vector2.one, rootRect.anchorMin);
                AssertVector(Vector2.one, rootRect.anchorMax);
                AssertVector(new Vector2(1f, 0f), rootRect.pivot);
                AssertVector(new Vector2(-70f, -493f), rootRect.anchoredPosition);

                AssertVector(Vector2.right, portrait.anchorMin);
                AssertVector(new Vector2(1f, 0f), portrait.pivot);
                AssertVector(new Vector2(-16f, 0f), portrait.anchoredPosition);

                AssertVector(Vector2.right, bubble.anchorMin);
                AssertVector(new Vector2(1f, 0f), bubble.pivot);
                AssertVector(new Vector2(-202f, 146f), bubble.anchoredPosition);

                AssertVector(new Vector2(22f, 0f), background.anchoredPosition);
                Assert.AreEqual(-1f, background.localScale.x, 0.001f);
                AssertVector(new Vector2(-164f, 179f), border.anchoredPosition);
                Assert.AreEqual(-1f, border.localScale.x, 0.001f);

                view.ApplySpeakerLayout(TutorialSpeakerSide.Left, Vector2.zero);
                AssertVector(new Vector2(80f, -498f), rootRect.anchoredPosition);
                AssertVector(new Vector2(202f, 146f), bubble.anchoredPosition);
                AssertVector(new Vector2(-22f, 0f), background.anchoredPosition);
                Assert.AreEqual(1f, background.localScale.x, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateRect(string name, Transform parent)
        {
            GameObject gameObject = new(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static void SetLayout(
            RectTransform target,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 position)
        {
            target.anchorMin = anchor;
            target.anchorMax = anchor;
            target.pivot = pivot;
            target.anchoredPosition = position;
            target.localScale = Vector3.one;
        }

        private static void AssertVector(Vector2 expected, Vector2 actual)
        {
            Assert.AreEqual(expected.x, actual.x, 0.001f);
            Assert.AreEqual(expected.y, actual.y, 0.001f);
        }
    }
}
