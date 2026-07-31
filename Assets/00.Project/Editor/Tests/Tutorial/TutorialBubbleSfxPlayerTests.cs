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
                player.GetQuestClearSoundId(TutorialStep.CollapseAndExpandTown));
            Assert.AreEqual(
                "SFX_Player_Collect_Pop_3",
                player.GetQuestClearSoundId(TutorialStep.DrawAnimal));
            Assert.AreEqual(
                "SFX_Player_Collect_Pop_1",
                player.GetQuestClearSoundId(TutorialStep.DrawTool));
            Assert.AreEqual(
                "SFX_Player_Collect_Pop_2",
                player.GetQuestClearSoundId(TutorialStep.AssignAnimal));
            Assert.AreEqual(
                "SFX_Player_Collect_Pop_3",
                player.GetQuestClearSoundId(TutorialStep.ConfirmAutoProduction));
            Assert.AreEqual(
                "SFX_Player_Collect_Pop_1",
                player.GetQuestClearSoundId(TutorialStep.OpenVillageInfo));
            Assert.AreEqual(
                "SFX_Player_Collect_Pop_2",
                player.GetQuestClearSoundId(TutorialStep.UpgradeVillage));
            Assert.AreEqual(
                string.Empty,
                player.GetQuestClearSoundId(TutorialStep.IntroDialogue));
            Assert.AreEqual(0.5f, player.QuestClearPresentationDelay);

            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void SkipButtonSoundIds_요청한세효과음의등록ID를사용한다()
        {
            GameObject gameObject = new("TutorialBubbleSkipSfxPlayerTest");
            TutorialBubbleSfxPlayer player =
                gameObject.AddComponent<TutorialBubbleSfxPlayer>();
            SerializedObject serialized = new(player);

            Assert.AreEqual(
                "SFX_Pop_Bubble_Single_1",
                serialized.FindProperty("skipOpenSoundId").stringValue);
            Assert.AreEqual(
                "UI_Click",
                serialized.FindProperty("skipConfirmSoundId").stringValue);
            Assert.AreEqual(
                "UI_Close",
                serialized.FindProperty("skipCancelSoundId").stringValue);

            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void SoundLibrary_튜토리얼과Skip효과음이_UI카테고리로등록되어있다()
        {
            SoundLibrary library =
                AssetDatabase.LoadAssetAtPath<SoundLibrary>(SoundLibraryPath);
            Assert.IsNotNull(library);

            string[] soundIds =
            {
                "SFX_Pop_Bubble_Single_1",
                "SFX_Player_Collect_Pop_1",
                "SFX_Player_Collect_Pop_2",
                "SFX_Player_Collect_Pop_3",
                "UI_Click",
                "UI_Close"
            };

            foreach (string soundId in soundIds)
            {
                Assert.IsTrue(
                    library.TryGetSound(soundId, out SoundClipData soundData),
                    $"SoundLibrary에 {soundId}가 없습니다.");
                Assert.AreEqual(SoundCategory.UI, soundData.Category);
                Assert.IsTrue(soundData.HasClip);
            }

            AssertSoundClipName(
                library,
                "SFX_Pop_Bubble_Single_1",
                "SFX_Pop_Bubble_Single_1");
            AssertSoundClipName(
                library,
                "UI_Click",
                "SFX_UI_Button_Click_Generic_1");
            AssertSoundClipName(
                library,
                "UI_Close",
                "SFX_UI_Button_Click_Close_1");
        }

        private static void AssertSoundClipName(
            SoundLibrary library,
            string soundId,
            string expectedClipName)
        {
            Assert.IsTrue(library.TryGetSound(soundId, out SoundClipData soundData));
            AudioClip clip = soundData.GetClip();
            Assert.IsNotNull(clip, $"{soundId}의 AudioClip이 없습니다.");
            Assert.AreEqual(expectedClipName, clip.name);
        }
    }
}
