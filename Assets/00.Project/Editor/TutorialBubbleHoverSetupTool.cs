using System;
using TaskTown.Tutorial;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TaskTown.EditorTools
{
    /// <summary>
    /// 튜토리얼 말풍선 Hover 연출을 지정된 Overlay Scene에 안전하게 연결합니다.
    /// Scene YAML을 직접 수정하지 않고 Unity 직렬화와 Undo 기록을 사용합니다.
    /// </summary>
    public static class TutorialBubbleHoverSetupTool
    {
        private const string ScenePath =
            "Assets/00.Project/00.Scenes/Project_Scene/tutorial_Overlay.unity";
        private const string MenuPath =
            "Tools/TaskTown/Tutorial/말풍선 Hover 설정 적용";

        [MenuItem(MenuPath)]
        public static void ApplyFromMenu()
        {
            ApplyToOverlayScene();
        }

        private static void ApplyToOverlayScene()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            TutorialBubbleView view = FindView(scene);
            if (view == null)
                throw new InvalidOperationException(
                    $"{ScenePath}에서 {nameof(TutorialBubbleView)}를 찾지 못했습니다.");

            SerializedObject viewSerialized = new(view);
            SerializedProperty advanceButtonProperty = viewSerialized.FindProperty("advanceButton");
            Button advanceButton = advanceButtonProperty.objectReferenceValue as Button;
            if (advanceButton == null)
                throw new InvalidOperationException(
                    $"{view.name}의 advanceButton 참조가 비어 있습니다.");

            GameObject bubbleObject = advanceButton.gameObject;
            RectTransform bubbleTransform = advanceButton.GetComponent<RectTransform>();
            if (bubbleTransform == null)
                throw new InvalidOperationException(
                    $"{bubbleObject.name}에 RectTransform이 없습니다.");

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("튜토리얼 말풍선 Hover 설정");

            TutorialBubbleHoverTween hoverTween =
                bubbleObject.GetComponent<TutorialBubbleHoverTween>();
            if (hoverTween == null)
                hoverTween = Undo.AddComponent<TutorialBubbleHoverTween>(bubbleObject);

            SerializedObject hoverSerialized = new(hoverTween);
            hoverSerialized.FindProperty("scaleTarget").objectReferenceValue = bubbleTransform;
            hoverSerialized.FindProperty("collapsedScale").floatValue = 0.5f;
            hoverSerialized.FindProperty("expandedScale").floatValue = 1f;
            hoverSerialized.FindProperty("expandDuration").floatValue = 0.2f;
            hoverSerialized.FindProperty("collapseDuration").floatValue = 0.15f;
            hoverSerialized.FindProperty("collapseDelay").floatValue = 2f;
            hoverSerialized.FindProperty("punchStrength").floatValue = 0.12f;
            hoverSerialized.FindProperty("punchDuration").floatValue = 0.25f;
            hoverSerialized.FindProperty("punchVibrato").intValue = 6;
            hoverSerialized.FindProperty("punchElasticity").floatValue = 0.6f;
            hoverSerialized.ApplyModifiedProperties();

            SerializedProperty hoverTweenProperty = viewSerialized.FindProperty("hoverTween");
            hoverTweenProperty.objectReferenceValue = hoverTween;
            viewSerialized.ApplyModifiedProperties();

            Undo.RecordObject(bubbleTransform, "말풍선 Pivot 및 기본 크기 설정");
            bubbleTransform.pivot = Vector2.zero;
            bubbleTransform.localScale = Vector3.one * 0.5f;

            EditorUtility.SetDirty(hoverTween);
            EditorUtility.SetDirty(view);
            EditorUtility.SetDirty(bubbleTransform);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Undo.CollapseUndoOperations(undoGroup);

            Selection.activeGameObject = bubbleObject;
            Debug.Log(
                $"[Tutorial] {scene.name}/{bubbleObject.name}에 Hover 확대 연출을 연결했습니다. " +
                "Pivot=(0, 0), Scale=0.5 → 1.0",
                bubbleObject);
        }

        private static TutorialBubbleView FindView(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                TutorialBubbleView view =
                    root.GetComponentInChildren<TutorialBubbleView>(true);
                if (view != null)
                    return view;
            }

            return null;
        }
    }
}
