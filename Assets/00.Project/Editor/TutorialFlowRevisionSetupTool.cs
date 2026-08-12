using System;
using TaskTown.Tutorial;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TaskTown.EditorTools
{
    /// <summary>
    /// 축소·확장 튜토리얼 기획 변경을 Config와 Overlay Scene에 적용합니다.
    /// Scene과 ScriptableObject를 텍스트로 수정하지 않고 Unity 직렬화와 Undo를 사용합니다.
    /// </summary>
    public static class TutorialFlowRevisionSetupTool
    {
        private const string ConfigPath =
            "Assets/00.Project/03.ScriptableObjects/Tutorial/TutorialConfig.asset";
        private const string ScenePath =
            "Assets/00.Project/00.Scenes/Project_Scene/tutorial_Overlay.unity";
        private const string MenuPath =
            "Tools/TaskTown/Tutorial/축소·확장 단계 설정 적용";

        private static readonly string[] GuideMessages =
        {
            "훌륭합니다, 촌장님! 마을 재건에 필요한 코인이 모이기 시작했군요.",
            "촌장님께서 다른 일을 하실 때는, 타운을 작게 접어둘 수도 있답니다.",
            "화면의 축소 버튼을 눌러 타운을 작게 만들어보시겠습니까?"
        };

        private static readonly string[] CompletionMessages =
        {
            "잘하셨어요!",
            "이렇게 타운을 작게 접어두었다가, 언제든 다시 돌아와 남은 일들을 이어갈 수 있답니다.",
            "촌장님께서 다른 일을 하는 동안에도, 마을의 주민들은 각자의 자리에서 열심히 일하게 될 겁니다.",
            "타운으로 돌아오셨으니, 이제 마을에 함께할 첫 번째 주민을 맞이해볼까요?"
        };

        [MenuItem(MenuPath)]
        public static void ApplyFromMenu()
        {
            ApplyConfig();
            ApplyOverlayScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void ApplyConfig()
        {
            TutorialConfigSO config =
                AssetDatabase.LoadAssetAtPath<TutorialConfigSO>(ConfigPath);
            if (config == null)
                throw new InvalidOperationException(
                    $"튜토리얼 Config를 찾지 못했습니다: {ConfigPath}");

            Undo.RecordObject(config, "튜토리얼 축소·확장 기획값 적용");
            SerializedObject serialized = new(config);
            SerializedProperty steps = serialized.FindProperty("steps");

            SerializedProperty windowStep = GetOrAddStep(
                steps,
                TutorialStep.CollapseAndExpandTown);
            SetStringArray(windowStep.FindPropertyRelative("messages"), GuideMessages);
            SetStringArray(
                windowStep.FindPropertyRelative("completionMessages"),
                CompletionMessages);
            windowStep.FindPropertyRelative("objectiveText").stringValue =
                "타운을 축소한 뒤, 다시 확장 화면으로 돌아와보세요!";
            windowStep.FindPropertyRelative("progressDisplayType").intValue =
                (int)TutorialProgressDisplayType.None;
            windowStep.FindPropertyRelative("progressFormat").stringValue =
                "{0} / {1}";
            windowStep.FindPropertyRelative("allowClickAdvance").boolValue = true;

            SerializedProperty drawAnimal = GetRequiredStep(
                steps,
                TutorialStep.DrawAnimal);
            drawAnimal.FindPropertyRelative("objectiveText").stringValue =
                "지급받은 코인으로 마을에 함께할 동물을 불러보세요!";

            SerializedProperty autoProduction = GetRequiredStep(
                steps,
                TutorialStep.ConfirmAutoProduction);
            autoProduction.FindPropertyRelative("objectiveText").stringValue =
                "자동 생산으로 Town Coin 50 획득";
            autoProduction.FindPropertyRelative("progressDisplayType").intValue =
                (int)TutorialProgressDisplayType.AutoProductionCoin;
            autoProduction.FindPropertyRelative("progressFormat").stringValue =
                "{0} / {1}";

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(config);
        }

        private static void ApplyOverlayScene()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            TutorialEventBridge bridge = FindBridge(scene);
            if (bridge == null)
                throw new InvalidOperationException(
                    $"{ScenePath}에서 {nameof(TutorialEventBridge)}를 찾지 못했습니다.");

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("튜토리얼 버튼 강조 연결");

            TutorialButtonHighlighter highlighter =
                bridge.GetComponent<TutorialButtonHighlighter>();
            if (highlighter == null)
                highlighter = Undo.AddComponent<TutorialButtonHighlighter>(bridge.gameObject);

            SerializedObject bridgeSerialized = new(bridge);
            bridgeSerialized.FindProperty("buttonHighlighter").objectReferenceValue =
                highlighter;
            bridgeSerialized.ApplyModifiedProperties();

            EditorUtility.SetDirty(highlighter);
            EditorUtility.SetDirty(bridge);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Undo.CollapseUndoOperations(undoGroup);

            Selection.activeGameObject = bridge.gameObject;
            Debug.Log(
                "[Tutorial] 축소·확장 단계 Config와 버튼 강조 연결을 적용했습니다.",
                bridge);
        }

        private static SerializedProperty GetOrAddStep(
            SerializedProperty steps,
            TutorialStep step)
        {
            SerializedProperty existing = FindStep(steps, step);
            if (existing != null)
                return existing;

            int index = steps.arraySize;
            steps.InsertArrayElementAtIndex(index);
            SerializedProperty created = steps.GetArrayElementAtIndex(index);
            created.FindPropertyRelative("step").intValue = (int)step;
            SetStringArray(created.FindPropertyRelative("messages"), Array.Empty<string>());
            SetStringArray(
                created.FindPropertyRelative("completionMessages"),
                Array.Empty<string>());
            created.FindPropertyRelative("objectiveText").stringValue = string.Empty;
            created.FindPropertyRelative("progressDisplayType").intValue =
                (int)TutorialProgressDisplayType.None;
            created.FindPropertyRelative("progressFormat").stringValue = "{0} / {1}";
            created.FindPropertyRelative("allowClickAdvance").boolValue = false;
            return created;
        }

        private static SerializedProperty GetRequiredStep(
            SerializedProperty steps,
            TutorialStep step)
        {
            return FindStep(steps, step) ??
                   throw new InvalidOperationException(
                       $"TutorialConfig에 {step} 단계가 없습니다.");
        }

        private static SerializedProperty FindStep(
            SerializedProperty steps,
            TutorialStep step)
        {
            for (int index = 0; index < steps.arraySize; index++)
            {
                SerializedProperty candidate = steps.GetArrayElementAtIndex(index);
                if (candidate.FindPropertyRelative("step").intValue == (int)step)
                    return candidate;
            }

            return null;
        }

        private static void SetStringArray(
            SerializedProperty property,
            string[] values)
        {
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
                property.GetArrayElementAtIndex(index).stringValue = values[index];
        }

        private static TutorialEventBridge FindBridge(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                TutorialEventBridge bridge =
                    root.GetComponentInChildren<TutorialEventBridge>(true);
                if (bridge != null)
                    return bridge;
            }

            return null;
        }
    }
}
