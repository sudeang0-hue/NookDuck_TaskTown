using System;
using System.Collections.Generic;
using TaskTown.Tutorial;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TaskTown.EditorTools
{
    /// <summary>
    /// 튜토리얼 효과음을 SoundLibrary와 Overlay Scene에 연결합니다.
    /// Scene YAML을 직접 수정하지 않고 Unity 직렬화와 Undo 기록을 사용합니다.
    /// </summary>
    public static class TutorialBubbleSfxSetupTool
    {
        private const string ScenePath =
            "Assets/00.Project/00.Scenes/Project_Scene/tutorial_Overlay.unity";
        private const string SoundLibraryPath =
            "Assets/00.Project/07.Audio/SoundLibrary/SoundLibrary.asset";
        private const string SoundDataFolder =
            "Assets/00.Project/07.Audio/SoundClipData/Tutorial";
        private const string MenuPath =
            "Tools/TaskTown/Tutorial/말풍선 SFX 설정 적용";

        private static readonly SoundSetup[] SoundSetups =
        {
            new(
                "SFX_Pop_Bubble_Single_1",
                "Assets/00.Project/07.Audio/mp3/02_UI/Bubble/" +
                "SFX_Pop_Bubble_Single_1.wav"),
            new(
                "SFX_Player_Collect_Pop_1",
                "Assets/00.Project/07.Audio/mp3/02_UI/Pop/" +
                "SFX_Player_Collect_Pop_1.wav"),
            new(
                "SFX_Player_Collect_Pop_2",
                "Assets/00.Project/07.Audio/mp3/02_UI/Pop/" +
                "SFX_Player_Collect_Pop_2.wav"),
            new(
                "SFX_Player_Collect_Pop_3",
                "Assets/00.Project/07.Audio/mp3/02_UI/Pop/" +
                "SFX_Player_Collect_Pop_3.wav")
        };

        [MenuItem(MenuPath)]
        public static void ApplyFromMenu()
        {
            ApplySoundDataAndLibrary();
            ApplyToOverlayScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void ApplySoundDataAndLibrary()
        {
            EnsureFolder(SoundDataFolder);

            List<SoundClipData> soundDataList = new();
            foreach (SoundSetup setup in SoundSetups)
                soundDataList.Add(GetOrCreateSoundData(setup));

            SoundLibrary library =
                AssetDatabase.LoadAssetAtPath<SoundLibrary>(SoundLibraryPath);
            if (library == null)
                throw new InvalidOperationException(
                    $"{SoundLibraryPath}에서 SoundLibrary를 찾지 못했습니다.");

            Undo.RecordObject(library, "튜토리얼 효과음 SoundLibrary 등록");
            SerializedObject librarySerialized = new(library);
            SerializedProperty sounds = librarySerialized.FindProperty("sounds");

            foreach (SoundClipData soundData in soundDataList)
            {
                if (ContainsReference(sounds, soundData))
                    continue;

                int index = sounds.arraySize;
                sounds.InsertArrayElementAtIndex(index);
                sounds.GetArrayElementAtIndex(index).objectReferenceValue = soundData;
            }

            librarySerialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(library);
        }

        private static SoundClipData GetOrCreateSoundData(SoundSetup setup)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(setup.ClipPath);
            if (clip == null)
                throw new InvalidOperationException(
                    $"튜토리얼 효과음 AudioClip을 찾지 못했습니다: {setup.ClipPath}");

            string assetPath = $"{SoundDataFolder}/{setup.SoundId}.asset";
            SoundClipData soundData =
                AssetDatabase.LoadAssetAtPath<SoundClipData>(assetPath);

            if (soundData == null)
            {
                soundData = ScriptableObject.CreateInstance<SoundClipData>();
                soundData.name = setup.SoundId;
                AssetDatabase.CreateAsset(soundData, assetPath);
                Undo.RegisterCreatedObjectUndo(soundData, "튜토리얼 효과음 데이터 생성");
            }
            else
            {
                Undo.RecordObject(soundData, "튜토리얼 효과음 데이터 갱신");
            }

            SerializedObject serialized = new(soundData);
            serialized.FindProperty("soundId").stringValue = setup.SoundId;
            serialized.FindProperty("category").enumValueIndex = (int)SoundCategory.UI;

            SerializedProperty clips = serialized.FindProperty("clips");
            clips.arraySize = 1;
            clips.GetArrayElementAtIndex(0).objectReferenceValue = clip;

            serialized.FindProperty("volumeScale").floatValue = 1f;
            serialized.FindProperty("loop").boolValue = false;
            serialized.FindProperty("randomizePitch").boolValue = false;
            serialized.FindProperty("minPitch").floatValue = 1f;
            serialized.FindProperty("maxPitch").floatValue = 1f;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(soundData);
            return soundData;
        }

        private static void ApplyToOverlayScene()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            TutorialBubbleController controller = FindController(scene);
            if (controller == null)
                throw new InvalidOperationException(
                    $"{ScenePath}에서 {nameof(TutorialBubbleController)}를 찾지 못했습니다.");

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("튜토리얼 말풍선 SFX 설정");

            TutorialBubbleSfxPlayer player =
                controller.GetComponent<TutorialBubbleSfxPlayer>();
            if (player == null)
                player = Undo.AddComponent<TutorialBubbleSfxPlayer>(controller.gameObject);

            SerializedObject playerSerialized = new(player);
            playerSerialized.FindProperty("dialogueAdvanceSoundId").stringValue =
                SoundSetups[0].SoundId;

            SerializedProperty playlist =
                playerSerialized.FindProperty("questClearSoundIds");
            playlist.arraySize = 3;
            for (int index = 0; index < playlist.arraySize; index++)
            {
                playlist.GetArrayElementAtIndex(index).stringValue =
                    SoundSetups[index + 1].SoundId;
            }

            playerSerialized.FindProperty("questClearPresentationDelay").floatValue =
                GetQuestClearDuration();
            playerSerialized.ApplyModifiedProperties();

            SerializedObject controllerSerialized = new(controller);
            controllerSerialized.FindProperty("sfxPlayer").objectReferenceValue = player;
            controllerSerialized.ApplyModifiedProperties();

            EditorUtility.SetDirty(player);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Undo.CollapseUndoOperations(undoGroup);

            Selection.activeGameObject = controller.gameObject;
            Debug.Log(
                $"[Tutorial] {scene.name}/{controller.name}에 말풍선 SFX를 연결했습니다. " +
                "퀘스트 완료음은 1 → 2 → 3 순서로 반복됩니다.",
                controller);
        }

        private static float GetQuestClearDuration()
        {
            float duration = 0f;
            for (int index = 1; index < SoundSetups.Length; index++)
            {
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(
                    SoundSetups[index].ClipPath);
                if (clip != null)
                    duration = Mathf.Max(duration, clip.length);
            }

            return duration;
        }

        private static bool ContainsReference(
            SerializedProperty array,
            UnityEngine.Object target)
        {
            for (int index = 0; index < array.arraySize; index++)
            {
                if (array.GetArrayElementAtIndex(index).objectReferenceValue == target)
                    return true;
            }

            return false;
        }

        private static TutorialBubbleController FindController(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                TutorialBubbleController controller =
                    root.GetComponentInChildren<TutorialBubbleController>(true);
                if (controller != null)
                    return controller;
            }

            return null;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            string parent = folderPath[..folderPath.LastIndexOf('/')];
            string folderName = folderPath[(folderPath.LastIndexOf('/') + 1)..];
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        private readonly struct SoundSetup
        {
            public SoundSetup(string soundId, string clipPath)
            {
                SoundId = soundId;
                ClipPath = clipPath;
            }

            public string SoundId { get; }
            public string ClipPath { get; }
        }
    }
}
