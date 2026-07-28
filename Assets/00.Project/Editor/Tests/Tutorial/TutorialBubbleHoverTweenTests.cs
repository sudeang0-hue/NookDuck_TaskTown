using System.Reflection;
using DG.Tweening;
using NUnit.Framework;
using TaskTown.Tutorial;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace TaskTown.EditorTests.Tutorial
{
    public class TutorialBubbleHoverTweenTests
    {
        [Test]
        public void Hover_마우스진입과이탈에따라_말풍선크기를변경한다()
        {
            GameObject bubble = new(
                "SpeechBubble",
                typeof(RectTransform),
                typeof(TutorialBubbleHoverTween));
            RectTransform rectTransform = bubble.GetComponent<RectTransform>();
            TutorialBubbleHoverTween hoverTween = bubble.GetComponent<TutorialBubbleHoverTween>();

            SerializedObject serialized = new(hoverTween);
            serialized.FindProperty("scaleTarget").objectReferenceValue = rectTransform;
            serialized.FindProperty("expandDuration").floatValue = 0f;
            serialized.FindProperty("collapseDuration").floatValue = 0f;
            serialized.FindProperty("collapseDelay").floatValue = 0f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            hoverTween.CollapseImmediate();
            Assert.AreEqual(Vector3.one * 0.5f, rectTransform.localScale);

            hoverTween.OnPointerEnter(null);
            Assert.AreEqual(Vector3.one, rectTransform.localScale);

            hoverTween.OnPointerExit(null);
            Assert.AreEqual(Vector3.one * 0.5f, rectTransform.localScale);

            Object.DestroyImmediate(bubble);
        }

        [Test]
        public void Hover_마우스이탈후_기본2초동안확대상태를유지한다()
        {
            GameObject bubble = new(
                "SpeechBubble",
                typeof(RectTransform),
                typeof(TutorialBubbleHoverTween));
            RectTransform rectTransform = bubble.GetComponent<RectTransform>();
            TutorialBubbleHoverTween hoverTween = bubble.GetComponent<TutorialBubbleHoverTween>();

            SerializedObject serialized = new(hoverTween);
            serialized.FindProperty("scaleTarget").objectReferenceValue = rectTransform;
            serialized.FindProperty("expandDuration").floatValue = 0f;
            serialized.FindProperty("collapseDuration").floatValue = 0f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.AreEqual(2f, serialized.FindProperty("collapseDelay").floatValue);

            hoverTween.OnPointerEnter(null);
            hoverTween.OnPointerExit(null);

            Assert.AreEqual(Vector3.one, rectTransform.localScale);

            // 지연 중 다시 진입하면 예약된 축소가 취소되어야 합니다.
            hoverTween.OnPointerEnter(null);
            Assert.AreEqual(Vector3.one, rectTransform.localScale);

            hoverTween.CollapseImmediate();
            Object.DestroyImmediate(bubble);
        }

        [Test]
        public void Punch_실행하면_DOPunchScaleTween을생성한다()
        {
            GameObject bubble = new(
                "SpeechBubble",
                typeof(RectTransform),
                typeof(TutorialBubbleHoverTween));
            TutorialBubbleHoverTween hoverTween = bubble.GetComponent<TutorialBubbleHoverTween>();

            hoverTween.CollapseImmediate();
            hoverTween.PlayPunch();

            Tween tween = GetScaleTween(hoverTween);
            Assert.IsNotNull(tween);
            Assert.IsTrue(tween.IsActive());

            hoverTween.CollapseImmediate();
            Object.DestroyImmediate(bubble);
        }

        [Test]
        public void StepChanged_퀘스트완료시만_Punch를실행한다()
        {
            GameObject controllerObject = new(
                "TutorialController",
                typeof(TutorialManager),
                typeof(TutorialBubbleController));
            GameObject viewObject = new(
                "TutorialView",
                typeof(TutorialBubbleView));
            GameObject bubble = new(
                "SpeechBubble",
                typeof(RectTransform),
                typeof(TutorialBubbleHoverTween));

            TutorialBubbleController controller =
                controllerObject.GetComponent<TutorialBubbleController>();
            TutorialManager manager =
                controllerObject.GetComponent<TutorialManager>();
            TutorialBubbleView view = viewObject.GetComponent<TutorialBubbleView>();
            TutorialBubbleHoverTween hoverTween =
                bubble.GetComponent<TutorialBubbleHoverTween>();
            TutorialConfigSO config = AssetDatabase.LoadAssetAtPath<TutorialConfigSO>(
                "Assets/00.Project/03.ScriptableObjects/Tutorial/TutorialConfig.asset");

            Assert.IsNotNull(config);
            LogAssert.Expect(
                LogType.Warning,
                "[TutorialManager] SaveManager가 없어 기본 진행 상태로 시작합니다. " +
                "현재 진행은 디스크 저장에 포함되지 않습니다.");
            manager.Initialize(null);

            SerializedObject viewSerialized = new(view);
            viewSerialized.FindProperty("hoverTween").objectReferenceValue = hoverTween;
            viewSerialized.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject controllerSerialized = new(controller);
            controllerSerialized.FindProperty("tutorialManager").objectReferenceValue = manager;
            controllerSerialized.FindProperty("config").objectReferenceValue = config;
            controllerSerialized.FindProperty("view").objectReferenceValue = view;
            controllerSerialized.ApplyModifiedPropertiesWithoutUndo();

            InvokeStepChanged(
                controller,
                TutorialStep.IntroDialogue,
                TutorialStep.EarnManualCoin);
            Assert.IsNull(GetScaleTween(hoverTween));

            InvokeStepChanged(
                controller,
                TutorialStep.DrawAnimal,
                TutorialStep.DrawTool);
            Assert.IsTrue(GetScaleTween(hoverTween)?.IsActive());

            hoverTween.CollapseImmediate();
            Object.DestroyImmediate(bubble);
            Object.DestroyImmediate(viewObject);
            Object.DestroyImmediate(controllerObject);
        }

        private static void InvokeStepChanged(
            TutorialBubbleController controller,
            TutorialStep previousStep,
            TutorialStep nextStep)
        {
            MethodInfo method = typeof(TutorialBubbleController).GetMethod(
                "HandleStepChanged",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            method.Invoke(controller, new object[] { previousStep, nextStep });
        }

        private static Tween GetScaleTween(TutorialBubbleHoverTween hoverTween)
        {
            FieldInfo field = typeof(TutorialBubbleHoverTween).GetField(
                "scaleTween",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field);
            return field.GetValue(hoverTween) as Tween;
        }
    }
}
