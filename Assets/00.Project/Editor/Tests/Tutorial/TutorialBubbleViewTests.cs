using System.Reflection;
using DG.Tweening;
using NUnit.Framework;
using TaskTown.KDH;
using TaskTown.Tutorial;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TaskTown.EditorTests.Tutorial
{
    public class TutorialBubbleViewTests
    {
        [Test]
        public void Render_대화단계에서만_말풍선클릭을전달한다()
        {
            GameObject root = new(
                "TutorialBubbleViewTest",
                typeof(CanvasGroup),
                typeof(TutorialBubbleView));
            GameObject buttonObject = new(
                "SpeechBubble",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(TutorialBubbleHoverTween));
            buttonObject.transform.SetParent(root.transform);
            GameObject indicator = new("ClickIndicator");
            indicator.transform.SetParent(root.transform);

            TutorialBubbleView view = root.GetComponent<TutorialBubbleView>();
            Button button = buttonObject.GetComponent<Button>();
            Image image = buttonObject.GetComponent<Image>();
            TutorialBubbleHoverTween hoverTween = buttonObject.GetComponent<TutorialBubbleHoverTween>();
            button.targetGraphic = image;

            SerializedObject serialized = new(view);
            serialized.FindProperty("advanceButton").objectReferenceValue = button;
            serialized.FindProperty("advanceIndicator").objectReferenceValue = indicator;
            serialized.FindProperty("hoverTween").objectReferenceValue = hoverTween;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // 일반 MonoBehaviour의 OnEnable은 EditMode 테스트에서 자동 호출되지 않습니다.
            InvokeLifecycle(view, "OnEnable");

            int advanceCount = 0;
            view.AdvanceRequested += () => advanceCount++;

            view.Render("Guide", null, "Dialogue", string.Empty, string.Empty, true);
            button.onClick.Invoke();

            Assert.AreEqual(1, advanceCount);
            Assert.IsTrue(image.raycastTarget);
            Assert.IsTrue(button.interactable);
            Assert.IsTrue(indicator.activeSelf);
            Assert.IsTrue(GetScaleTween(hoverTween)?.IsActive());

            view.Render("Guide", null, "Quest", "Objective", "0 / 100", false);
            button.onClick.Invoke();

            Assert.AreEqual(1, advanceCount);
            Assert.IsTrue(image.raycastTarget);
            Assert.IsFalse(button.interactable);
            Assert.IsFalse(indicator.activeSelf);

            buttonObject.transform.localScale = Vector3.one;
            view.SetVisible(false);
            Assert.AreEqual(Vector3.one * 0.5f, buttonObject.transform.localScale);

            InvokeLifecycle(view, "OnDisable");
            Object.DestroyImmediate(root);
        }

        [TestCase(
            TutorialStep.EarnManualCoin,
            TutorialStep.CollapseAndExpandTown,
            105L,
            "100 / 100 Town Coin")]
        [TestCase(
            TutorialStep.ConfirmAutoProduction,
            TutorialStep.OpenVillageInfo,
            51L,
            "50 / 50")]
        public void ProgressChanged_목표달성으로단계가바뀌면_완료수치를먼저표시한다(
            TutorialStep presentedStep,
            TutorialStep nextStep,
            long earnedAmount,
            string expectedProgress)
        {
            GameObject root = new(
                "TutorialCompletedProgressTest",
                typeof(CanvasGroup),
                typeof(TutorialBubbleView),
                typeof(TutorialBubbleController));
            GameObject progressObject = new(
                "ProgressText",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            progressObject.transform.SetParent(root.transform);

            try
            {
                TutorialBubbleView view = root.GetComponent<TutorialBubbleView>();
                TutorialBubbleController controller =
                    root.GetComponent<TutorialBubbleController>();
                TMP_Text progressText = progressObject.GetComponent<TMP_Text>();
                TutorialConfigSO config =
                    AssetDatabase.LoadAssetAtPath<TutorialConfigSO>(
                        "Assets/00.Project/03.ScriptableObjects/Tutorial/" +
                        "TutorialConfig.asset");
                Assert.IsNotNull(config);

                SerializedObject viewSerialized = new(view);
                viewSerialized.FindProperty("canvasGroup").objectReferenceValue =
                    root.GetComponent<CanvasGroup>();
                viewSerialized.FindProperty("progressText").objectReferenceValue =
                    progressText;
                viewSerialized.ApplyModifiedPropertiesWithoutUndo();

                SerializedObject controllerSerialized = new(controller);
                controllerSerialized.FindProperty("config").objectReferenceValue = config;
                controllerSerialized.FindProperty("view").objectReferenceValue = view;
                controllerSerialized.ApplyModifiedPropertiesWithoutUndo();

                SetPrivateField(controller, "presentedStep", presentedStep);
                SetPrivateField(controller, "hasPresentedStep", true);

                TutorialSaveData progress = new()
                {
                    currentStep = nextStep,
                    manualEarnedCoin = presentedStep == TutorialStep.EarnManualCoin
                        ? earnedAmount
                        : 0L,
                    autoProductionEarnedCoin =
                        presentedStep == TutorialStep.ConfirmAutoProduction
                            ? earnedAmount
                            : 0L
                };

                InvokeProgressChanged(controller, progress);

                Assert.AreEqual(expectedProgress, progressText.text);
                Assert.IsTrue(progressText.gameObject.activeSelf);
                Assert.IsTrue(view.IsVisible);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void InvokeLifecycle(TutorialBubbleView view, string methodName)
        {
            MethodInfo method = typeof(TutorialBubbleView).GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            method.Invoke(view, null);
        }

        private static Tween GetScaleTween(TutorialBubbleHoverTween hoverTween)
        {
            FieldInfo field = typeof(TutorialBubbleHoverTween).GetField(
                "scaleTween",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field);
            return field.GetValue(hoverTween) as Tween;
        }

        private static void InvokeProgressChanged(
            TutorialBubbleController controller,
            TutorialSaveData progress)
        {
            MethodInfo method = typeof(TutorialBubbleController).GetMethod(
                "HandleProgressChanged",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            method.Invoke(controller, new object[] { progress });
        }

        private static void SetPrivateField(
            TutorialBubbleController controller,
            string fieldName,
            object value)
        {
            FieldInfo field = typeof(TutorialBubbleController).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field);
            field.SetValue(controller, value);
        }
    }
}
