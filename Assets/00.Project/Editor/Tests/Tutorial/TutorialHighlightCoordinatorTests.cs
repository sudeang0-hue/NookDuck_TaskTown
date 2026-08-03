using System.Reflection;
using DG.Tweening;
using NUnit.Framework;
using TaskTown.Tutorial;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TaskTown.EditorTests.Tutorial
{
    public class TutorialHighlightCoordinatorTests
    {
        [Test]
        public void Highlight_Pointer전용설정은_버튼Scale을변경하지않는다()
        {
            TestContext context = CreateContext(TutorialHighlightEffect.Pointer);

            try
            {
                context.Coordinator.Highlight(
                    TutorialStep.DrawAnimal,
                    new[] { context.Button },
                    new[] { context.Button });

                Assert.IsNull(DOTween.TweensByTarget(context.Button.transform));
                Assert.IsTrue(context.PointerRoot.activeSelf);
                Assert.IsFalse(context.RingObject.activeSelf);
            }
            finally
            {
                context.Dispose();
            }
        }

        [Test]
        public void Highlight_Scale전용설정은_손가락UI를표시하지않는다()
        {
            TestContext context = CreateContext(TutorialHighlightEffect.ScalePulse);

            try
            {
                context.Coordinator.Highlight(
                    TutorialStep.DrawAnimal,
                    new[] { context.Button },
                    new[] { context.Button });

                Tween tween = DOTween.TweensByTarget(context.Button.transform)?[0];
                Assert.IsNotNull(tween);
                Assert.IsTrue(tween.IsActive());
                Assert.IsFalse(context.PointerRoot.activeSelf);
            }
            finally
            {
                context.Dispose();
            }
        }

        [Test]
        public void HighlightWorldPointer_월드Collider중심에_손가락을표시한다()
        {
            TestContext context = CreateContext(TutorialHighlightEffect.Pointer);
            GameObject cameraObject = new("WorldPointerCamera", typeof(Camera));
            GameObject targetObject = new("village_house", typeof(BoxCollider));
            targetObject.transform.position = new Vector3(0f, 0f, 5f);

            try
            {
                context.Coordinator.HighlightWorldPointer(
                    TutorialStep.DrawAnimal,
                    targetObject.GetComponent<BoxCollider>(),
                    cameraObject.GetComponent<Camera>());

                Assert.IsTrue(context.PointerRoot.activeSelf);
                Assert.IsFalse(context.RingObject.activeSelf);
                Assert.IsNull(DOTween.TweensByTarget(targetObject.transform));
            }
            finally
            {
                Object.DestroyImmediate(targetObject);
                Object.DestroyImmediate(cameraObject);
                context.Dispose();
            }
        }

        [Test]
        public void Highlight_버튼내부기준점과추가오프셋을_구간별로적용한다()
        {
            TestContext context = CreateContext(
                TutorialHighlightEffect.Pointer,
                TutorialPointerPositionMode.FollowHighlightedButton);

            try
            {
                RectTransform buttonRect =
                    context.Button.GetComponent<RectTransform>();
                buttonRect.sizeDelta = new Vector2(200f, 100f);

                context.Coordinator.Highlight(
                    TutorialStep.DrawAnimal,
                    System.Array.Empty<Button>(),
                    new[] { context.Button },
                    new Vector2(1f, 0.5f),
                    new Vector2(-10f, 0f));

                RectTransform pointerRect =
                    context.PointerRoot.GetComponent<RectTransform>();
                Assert.AreEqual(90f, pointerRect.anchoredPosition.x, 0.01f);
                Assert.AreEqual(0f, pointerRect.anchoredPosition.y, 0.01f);
            }
            finally
            {
                context.Dispose();
            }
        }

        [Test]
        public void Highlight_FocusRing설정은_손가락과도넛을함께표시한다()
        {
            TestContext context = CreateContext(
                TutorialHighlightEffect.Pointer |
                TutorialHighlightEffect.FocusRing);

            try
            {
                context.Coordinator.Highlight(
                    TutorialStep.DrawAnimal,
                    new[] { context.Button },
                    new[] { context.Button });

                Assert.IsTrue(context.PointerRoot.activeSelf);
                Assert.IsTrue(context.RingObject.activeSelf);
                Assert.IsNotNull(context.RingObject.GetComponent<CanvasRenderer>());
            }
            finally
            {
                context.Dispose();
            }
        }

        [Test]
        public void FocusRingGraphic_도넛Mesh를생성하고입력을가리지않는다()
        {
            GameObject ringObject = new(
                "FocusRingTest",
                typeof(RectTransform),
                typeof(TutorialFocusRingGraphic));
            TutorialFocusRingGraphic graphic =
                ringObject.GetComponent<TutorialFocusRingGraphic>();
            ringObject.GetComponent<RectTransform>().sizeDelta = Vector2.one * 120f;
            VertexHelper helper = new();

            MethodInfo populateMesh = typeof(TutorialFocusRingGraphic).GetMethod(
                "OnPopulateMesh",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(VertexHelper) },
                null);
            Assert.IsNotNull(populateMesh);
            populateMesh.Invoke(graphic, new object[] { helper });

            Assert.Greater(helper.currentVertCount, 0);
            Assert.Greater(helper.currentIndexCount, 0);
            Assert.IsFalse(graphic.raycastTarget);
            Assert.IsNotNull(ringObject.GetComponent<CanvasRenderer>());

            helper.Dispose();
            Object.DestroyImmediate(ringObject);
        }

        private static TestContext CreateContext(
            TutorialHighlightEffect effects,
            TutorialPointerPositionMode pointerMode =
                TutorialPointerPositionMode.CanvasPosition)
        {
            GameObject canvasObject = new(
                "TutorialHighlightCanvasTest",
                typeof(RectTransform),
                typeof(Canvas));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            GameObject systemObject = new(
                "TutorialHighlightSystemTest",
                typeof(TutorialButtonHighlighter),
                typeof(TutorialPointerIndicator),
                typeof(TutorialHighlightCoordinator));

            GameObject pointerRoot = new(
                "TutorialPointerRoot",
                typeof(RectTransform),
                typeof(CanvasGroup));
            pointerRoot.transform.SetParent(canvasObject.transform, false);
            GameObject ringObject = new(
                "FocusRing",
                typeof(RectTransform),
                typeof(TutorialFocusRingGraphic));
            ringObject.transform.SetParent(pointerRoot.transform, false);
            GameObject handObject = new("Hand", typeof(RectTransform));
            handObject.transform.SetParent(pointerRoot.transform, false);

            GameObject buttonObject = new(
                "TargetButton",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            buttonObject.transform.SetParent(canvasObject.transform, false);

            TutorialConfigSO config = CreateConfig(effects, pointerMode);
            TutorialPointerIndicator pointerIndicator =
                systemObject.GetComponent<TutorialPointerIndicator>();
            TutorialButtonHighlighter scaleHighlighter =
                systemObject.GetComponent<TutorialButtonHighlighter>();
            TutorialHighlightCoordinator coordinator =
                systemObject.GetComponent<TutorialHighlightCoordinator>();

            SerializedObject pointerSerialized = new(pointerIndicator);
            pointerSerialized.FindProperty("overlayCanvas").objectReferenceValue = canvas;
            pointerSerialized.FindProperty("indicatorRoot").objectReferenceValue =
                pointerRoot.GetComponent<RectTransform>();
            pointerSerialized.FindProperty("indicatorCanvasGroup").objectReferenceValue =
                pointerRoot.GetComponent<CanvasGroup>();
            pointerSerialized.FindProperty("ringTransform").objectReferenceValue =
                ringObject.GetComponent<RectTransform>();
            pointerSerialized.FindProperty("handTransform").objectReferenceValue =
                handObject.GetComponent<RectTransform>();
            pointerSerialized.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject coordinatorSerialized = new(coordinator);
            coordinatorSerialized.FindProperty("config").objectReferenceValue = config;
            coordinatorSerialized.FindProperty("scaleHighlighter").objectReferenceValue =
                scaleHighlighter;
            coordinatorSerialized.FindProperty("pointerIndicator").objectReferenceValue =
                pointerIndicator;
            coordinatorSerialized.ApplyModifiedPropertiesWithoutUndo();

            pointerRoot.SetActive(false);
            return new TestContext(
                canvasObject,
                systemObject,
                buttonObject,
                pointerRoot,
                ringObject,
                config,
                coordinator);
        }

        private static TutorialConfigSO CreateConfig(
            TutorialHighlightEffect effects,
            TutorialPointerPositionMode pointerMode)
        {
            TutorialConfigSO config = ScriptableObject.CreateInstance<TutorialConfigSO>();
            SerializedObject serialized = new(config);
            SerializedProperty steps = serialized.FindProperty("steps");
            steps.arraySize = 1;
            SerializedProperty content = steps.GetArrayElementAtIndex(0);
            content.FindPropertyRelative("step").intValue = (int)TutorialStep.DrawAnimal;
            content.FindPropertyRelative("highlightEffects").intValue = (int)effects;
            content.FindPropertyRelative("pointerPositionMode").intValue =
                (int)pointerMode;
            content.FindPropertyRelative("pointerCanvasPosition").vector2Value =
                new Vector2(100f, 50f);
            content.FindPropertyRelative("pointerRingSize").floatValue = 120f;
            content.FindPropertyRelative("pointerRingPadding").floatValue = 24f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        private readonly struct TestContext
        {
            public TestContext(
                GameObject canvasObject,
                GameObject systemObject,
                GameObject buttonObject,
                GameObject pointerRoot,
                GameObject ringObject,
                TutorialConfigSO config,
                TutorialHighlightCoordinator coordinator)
            {
                CanvasObject = canvasObject;
                SystemObject = systemObject;
                ButtonObject = buttonObject;
                PointerRoot = pointerRoot;
                RingObject = ringObject;
                Config = config;
                Coordinator = coordinator;
            }

            public GameObject CanvasObject { get; }
            public GameObject SystemObject { get; }
            public GameObject ButtonObject { get; }
            public GameObject PointerRoot { get; }
            public GameObject RingObject { get; }
            public TutorialConfigSO Config { get; }
            public TutorialHighlightCoordinator Coordinator { get; }
            public Button Button => ButtonObject.GetComponent<Button>();

            public void Dispose()
            {
                Coordinator.ClearAllHighlights();
                Object.DestroyImmediate(Config);
                Object.DestroyImmediate(ButtonObject);
                Object.DestroyImmediate(SystemObject);
                Object.DestroyImmediate(CanvasObject);
            }
        }
    }
}
