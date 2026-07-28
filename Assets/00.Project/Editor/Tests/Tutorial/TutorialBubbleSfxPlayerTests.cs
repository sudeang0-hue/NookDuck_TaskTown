using NUnit.Framework;
using TaskTown.Tutorial;
using UnityEditor;
using UnityEngine;

namespace TaskTown.EditorTests.Tutorial
{
    public class TutorialBubbleSfxPlayerTests
    {
        private const string SoundLibraryPath =
            "Assets/00.Project/07.Audio/SoundLibrary/SoundLibrary.asset";

        [Test]
        public void QuestClearPlaylist_퀘스트순서에따라_123순환한다()
        {
            GameObject gameObject = new("TutorialBubbleSfxPlayerTest");
            TutorialBubbleSfxPlayer player =
                gameObject.AddComponent<TutorialBubbleSfxPlayer>();

            Assert.AreEqual(
                "SFX_Player_Collect_Pop_1",
                player.GetQuestClearSoundId(TutorialStep.EarnManualCoin));
            Assert.AreEqual(
                "SFX_Player_Collect_Pop_2",
                player.GetQuestClearSoundId(TutorialStep.DrawAnimal));
            Assert.AreEqual(
                "SFX_Player_Collect_Pop_3",
                player.GetQuestClearSoundId(TutorialStep.DrawTool));
            Assert.AreEqual(
                "SFX_Player_Collect_Pop_1",
                player.GetQuestClearSoundId(TutorialStep.AssignAnimal));
            Assert.AreEqual(
                "SFX_Player_Collect_Pop_2",
                player.GetQuestClearSoundId(TutorialStep.ConfirmAutoProduction));
            Assert.AreEqual(
                "SFX_Player_Collect_Pop_3",
                player.GetQuestClearSoundId(TutorialStep.OpenVillageInfo));
            Assert.AreEqual(
                "SFX_Player_Collect_Pop_1",
                player.GetQuestClearSoundId(TutorialStep.UpgradeVillage));
            Assert.AreEqual(
                string.Empty,
                player.GetQuestClearSoundId(TutorialStep.IntroDialogue));
            Assert.AreEqual(0.5f, player.QuestClearPresentationDelay);

            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void SoundLibrary_튜토리얼효과음4종이_UI카테고리로등록되어있다()
        {
            SoundLibrary library =
                AssetDatabase.LoadAssetAtPath<SoundLibrary>(SoundLibraryPath);
            Assert.IsNotNull(library);

            string[] soundIds =
            {
                "SFX_Pop_Bubble_Single_1",
                "SFX_Player_Collect_Pop_1",
                "SFX_Player_Collect_Pop_2",
                "SFX_Player_Collect_Pop_3"
            };

            foreach (string soundId in soundIds)
            {
                Assert.IsTrue(
                    library.TryGetSound(soundId, out SoundClipData soundData),
                    $"SoundLibrary에 {soundId}가 없습니다.");
                Assert.AreEqual(SoundCategory.UI, soundData.Category);
                Assert.IsTrue(soundData.HasClip);
            }
        }
    }
}
