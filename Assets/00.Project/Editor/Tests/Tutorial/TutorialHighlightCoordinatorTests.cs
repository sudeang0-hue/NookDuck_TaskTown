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

            helper.Dispose();
            Object.DestroyImmediate(ringObject);
        }

        private static TestContext CreateContext(TutorialHighlightEffect effects)
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
            GameObject ringObject = new("FocusRing", typeof(RectTransform));
            ringObject.transform.SetParent(pointerRoot.transform, false);
            GameObject handObject = new("Hand", typeof(RectTransform));
            handObject.transform.SetParent(pointerRoot.transform, false);

            GameObject buttonObject = new(
                "TargetButton",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            buttonObject.transform.SetParent(canvasObject.transform, false);

            TutorialConfigSO config = CreateConfig(effects);
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
                config,
                coordinator);
        }

        private static TutorialConfigSO CreateConfig(TutorialHighlightEffect effects)
        {
            TutorialConfigSO config = ScriptableObject.CreateInstance<TutorialConfigSO>();
            SerializedObject serialized = new(config);
            SerializedProperty steps = serialized.FindProperty("steps");
            steps.arraySize = 1;
            SerializedProperty content = steps.GetArrayElementAtIndex(0);
            content.FindPropertyRelative("step").intValue = (int)TutorialStep.DrawAnimal;
            content.FindPropertyRelative("highlightEffects").intValue = (int)effects;
            content.FindPropertyRelative("pointerPositionMode").intValue =
                (int)TutorialPointerPositionMode.CanvasPosition;
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
                TutorialConfigSO config,
                TutorialHighlightCoordinator coordinator)
            {
                CanvasObject = canvasObject;
                SystemObject = systemObject;
                ButtonObject = buttonObject;
                PointerRoot = pointerRoot;
                Config = config;
                Coordinator = coordinator;
            }

            public GameObject CanvasObject { get; }
            public GameObject SystemObject { get; }
            public GameObject ButtonObject { get; }
            public GameObject PointerRoot { get; }
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
