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
    }
}
