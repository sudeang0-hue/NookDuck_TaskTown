using System.Reflection;
using DG.Tweening;
using NUnit.Framework;
using TaskTown.Tutorial;
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
        public void TutorialCoinHandlers_수동과자동획득시_Feedback을재생한다()
        {
            GameObject bridgeObject = new(
                "TutorialCoinFeedbackBridgeTest",
                typeof(TutorialEventBridge),
                typeof(TutorialManualCoinFeedback));
            GameObject targetObject = new("AllCoinText", typeof(RectTransform));
            TutorialEventBridge bridge =
                bridgeObject.GetComponent<TutorialEventBridge>();
            TutorialManualCoinFeedback feedback =
                bridgeObject.GetComponent<TutorialManualCoinFeedback>();
            RectTransform target = targetObject.GetComponent<RectTransform>();
            Vector3 originalScale = new(0.9f, 0.9f, 1f);
            target.localScale = originalScale;

            try
            {
                SetPrivateField(bridge, "manualCoinFeedback", feedback);
                feedback.Bind(target);

                InvokePrivate(bridge, "HandleManualCoinGranted", 1);
                Assert.AreEqual(1, DOTween.TweensByTarget(target)?.Count);

                feedback.StopAndRestore();
                feedback.Bind(target);

                InvokePrivate(bridge, "HandleProductionCoinGranted", 1);
                Assert.AreEqual(1, DOTween.TweensByTarget(target)?.Count);
            }
            finally
            {
                feedback.StopAndRestore();
                Assert.AreEqual(originalScale, target.localScale);
                Object.DestroyImmediate(bridgeObject);
                Object.DestroyImmediate(targetObject);
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
