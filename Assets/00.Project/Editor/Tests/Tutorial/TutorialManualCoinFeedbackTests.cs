using System.Reflection;
using DG.Tweening;
using NUnit.Framework;
using TaskTown.Tutorial;
using TMPro;
using UnityEngine;

namespace TaskTown.EditorTests.Tutorial
{
    public class TutorialManualCoinFeedbackTests
    {
        [Test]
        public void Play_WhenRepeated_KeepsSingleTweenAndRestoresOriginalScale()
        {
            GameObject feedbackObject = new(
                "TutorialManualCoinFeedbackTest",
                typeof(TutorialManualCoinFeedback));
            GameObject targetObject = new("AllCoinTextTest", typeof(RectTransform));
            TutorialManualCoinFeedback feedback =
                feedbackObject.GetComponent<TutorialManualCoinFeedback>();
            RectTransform target = targetObject.GetComponent<RectTransform>();
            Vector3 originalScale = new(0.8f, 0.8f, 1f);
            target.localScale = originalScale;

            try
            {
                feedback.Bind(target);
                feedback.Play();

                int firstActiveTweenCount = 0;
                foreach (Tween tween in DOTween.TweensByTarget(target))
                {
                    if (tween.IsActive())
                        firstActiveTweenCount++;
                }

                feedback.Play();

                int repeatedActiveTweenCount = 0;
                foreach (Tween tween in DOTween.TweensByTarget(target))
                {
                    if (tween.IsActive())
                        repeatedActiveTweenCount++;
                }

                Assert.Greater(firstActiveTweenCount, 0);
                Assert.AreEqual(firstActiveTweenCount, repeatedActiveTweenCount);

                feedback.StopAndRestore();
                Assert.AreEqual(originalScale, target.localScale);
                Assert.IsNull(DOTween.TweensByTarget(target));
            }
            finally
            {
                DOTween.Kill(target);
                Object.DestroyImmediate(feedbackObject);
                Object.DestroyImmediate(targetObject);
            }
        }

        [Test]
        public void GlobalCoinPunch_같은프레임의연속획득은_Tween하나로합친다()
        {
            GameObject controllerObject = new(
                "GlobalCoinPunchTest",
                typeof(UIController_Coin));
            GameObject textObject = new(
                "AllCoinText",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(controllerObject.transform);

            UIController_Coin controller =
                controllerObject.GetComponent<UIController_Coin>();
            TMP_Text text = textObject.GetComponent<TMP_Text>();
            Vector3 originalScale = new(0.9f, 0.9f, 1f);
            text.rectTransform.localScale = originalScale;

            try
            {
                SetPrivateField(controller, "allCoinText", text);
                SetPrivateField(controller, "allCoinOriginalScale", originalScale);

                InvokePrivate(controller, "HandleCoinGranted", 1);
                InvokePrivate(controller, "HandleCoinGranted", 1);
                InvokePrivate(controller, "HandleCoinGranted", 1);
                InvokePrivate(controller, "PlayRequestedGainPunch");

                var tweens = DOTween.TweensByTarget(text.rectTransform);
                Assert.IsNotNull(tweens);
                Assert.AreEqual(1, tweens.Count);
                Tween firstTween = tweens[0];

                InvokePrivate(controller, "HandleCoinGranted", 1);
                InvokePrivate(controller, "HandleCoinGranted", 1);
                InvokePrivate(controller, "PlayRequestedGainPunch");

                tweens = DOTween.TweensByTarget(text.rectTransform);
                Assert.IsNotNull(tweens);
                Assert.AreEqual(1, tweens.Count);
                Assert.AreSame(firstTween, tweens[0]);
            }
            finally
            {
                InvokePrivate(controller, "StopGainPunchAndRestore");
                Assert.AreEqual(originalScale, text.rectTransform.localScale);
                Object.DestroyImmediate(controllerObject);
            }
        }

        private static object InvokePrivate(
            object target,
            string methodName,
            params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            return method.Invoke(target, arguments);
        }

        private static void SetPrivateField(
            object target,
            string fieldName,
            object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field);
            field.SetValue(target, value);
        }
    }
}
