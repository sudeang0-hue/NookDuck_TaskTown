using NUnit.Framework;
using TaskTown.Tutorial;
using UnityEngine;
using UnityEngine.TestTools;

namespace TaskTown.EditorTests.Tutorial
{
    public class TutorialManagerTests
    {
        [Test]
        public void SetPaused_상태가바뀔때만PauseChanged를호출한다()
        {
            GameObject gameObject = new("TutorialManagerTest");
            TutorialManager manager = gameObject.AddComponent<TutorialManager>();
            LogAssert.Expect(
                LogType.Warning,
                "[TutorialManager] SaveManager가 없어 기본 진행 상태로 시작합니다. " +
                "현재 진행은 디스크 저장에 포함되지 않습니다.");
            manager.Initialize(null);

            int eventCount = 0;
            bool lastPaused = false;
            manager.PauseChanged += isPaused =>
            {
                eventCount++;
                lastPaused = isPaused;
            };

            manager.SetPaused(true);
            manager.SetPaused(true);
            Assert.AreEqual(1, eventCount);
            Assert.IsTrue(lastPaused);

            manager.SetPaused(false);
            Assert.AreEqual(2, eventCount);
            Assert.IsFalse(lastPaused);

            Object.DestroyImmediate(gameObject);
        }
    }
}
