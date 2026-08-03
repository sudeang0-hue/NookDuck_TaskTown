using System;
using TaskTown.Tutorial;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TaskTown.EditorTools
{
    [CustomEditor(typeof(TutorialBubbleView))]
    public sealed class TutorialBubbleLayoutEditor : UnityEditor.Editor
    {
        private const string ScenePath =
            "Assets/00.Project/00.Scenes/Project_Scene/tutorial_Overlay.unity";

        private TutorialSpeakerSide previewSide = TutorialSpeakerSide.Left;
        private TutorialBubbleLayoutPreset previewOrigin;
        private bool hasPreviewOrigin;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField(
                "말풍선 배치 편집",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Left/Right 미리보기 후 Scene View의 Rect Tool로 위치를 " +
                "조정하고 현재 배치를 원하는 프리셋으로 저장하세요. " +
                "미리보기 종료 시 시작 전 배치로 복원됩니다.",
                MessageType.Info);

            previewSide = (TutorialSpeakerSide)EditorGUILayout.EnumPopup(
                "미리보기 방향",
                previewSide);

            TutorialBubbleView view = (TutorialBubbleView)target;

            if (GUILayout.Button("선택 방향 미리보기"))
                Preview(view, previewSide);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Left 미리보기"))
                    Preview(view, TutorialSpeakerSide.Left);

                if (GUILayout.Button("Right 미리보기"))
                    Preview(view, TutorialSpeakerSide.Right);
            }

            using (new EditorGUI.DisabledScope(!hasPreviewOrigin))
            {
                if (GUILayout.Button("미리보기 종료"))
                    RestorePreview(view, true);
            }

            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("현재 배치를 Left로 저장"))
                    Capture(view, TutorialSpeakerSide.Left);

                if (GUILayout.Button("현재 배치를 Right로 저장"))
                    Capture(view, TutorialSpeakerSide.Right);
            }

            if (GUILayout.Button("Left 기준 Right 대칭 생성"))
                CreateMirroredRight(view);
        }

        private void OnDisable()
        {
            TutorialBubbleView view = target as TutorialBubbleView;
            if (view != null && hasPreviewOrigin)
                RestorePreview(view, false);
        }

        private void Preview(
            TutorialBubbleView view,
            TutorialSpeakerSide side)
        {
            if (!hasPreviewOrigin)
            {
                previewOrigin = view.CaptureCurrentLayoutPreset();
                hasPreviewOrigin = true;
            }

            previewSide = side;
            Undo.RegisterFullObjectHierarchyUndo(
                view.gameObject,
                $"튜토리얼 말풍선 {side} 미리보기");
            view.ApplySpeakerLayout(side, Vector2.zero, true);
            MarkSceneDirty(view);
            SceneView.RepaintAll();
        }

        private void RestorePreview(
            TutorialBubbleView view,
            bool registerUndo)
        {
            if (!hasPreviewOrigin)
                return;

            if (registerUndo)
            {
                Undo.RegisterFullObjectHierarchyUndo(
                    view.gameObject,
                    "튜토리얼 말풍선 미리보기 종료");
            }

            view.ApplyLayoutPreset(previewOrigin);
            hasPreviewOrigin = false;
            MarkSceneDirty(view);
            SceneView.RepaintAll();
        }

        private static void Capture(
            TutorialBubbleView view,
            TutorialSpeakerSide side)
        {
            Undo.RecordObject(view, $"튜토리얼 말풍선 {side} 배치 저장");
            view.CaptureCurrentLayout(side);
            EditorUtility.SetDirty(view);
            MarkSceneDirty(view);
        }

        private static void CreateMirroredRight(TutorialBubbleView view)
        {
            Undo.RecordObject(view, "튜토리얼 말풍선 Right 대칭 배치 생성");
            view.CreateMirroredRightLayout();
            EditorUtility.SetDirty(view);
            MarkSceneDirty(view);
        }

        private static void MarkSceneDirty(TutorialBubbleView view)
        {
            if (view != null && view.gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
        }

        [MenuItem("Tools/TaskTown/Tutorial/말풍선 배치 편집")]
        private static void OpenLayoutEditor()
        {
            Scene scene = OpenOverlayScene();

            TutorialBubbleView view = FindView(scene);
            if (view == null)
            {
                throw new InvalidOperationException(
                    $"{ScenePath}에서 {nameof(TutorialBubbleView)}를 찾지 못했습니다.");
            }

            SceneManager.SetActiveScene(scene);
            Selection.activeGameObject = view.gameObject;
            EditorGUIUtility.PingObject(view.gameObject);
        }

        [MenuItem("Tools/TaskTown/Tutorial/말풍선 배치 기본값 연결")]
        private static void InitializeLayoutPresets()
        {
            Scene scene = OpenOverlayScene();
            TutorialBubbleView view = FindView(scene);
            if (view == null)
            {
                throw new InvalidOperationException(
                    $"{ScenePath}에서 {nameof(TutorialBubbleView)}를 찾지 못했습니다.");
            }

            RectTransform layoutRoot = view.GetComponent<RectTransform>();
            RectTransform portraitRoot = FindRect(view.transform, "PortraitFrame");
            RectTransform bubbleRoot = FindRect(view.transform, "SpeechBubble");
            RectTransform bubbleBackground =
                FindRect(view.transform, "SpeechBubble/Image");
            RectTransform bubbleBorder =
                FindRect(view.transform, "SpeechBubbleBorder");

            if (layoutRoot == null || portraitRoot == null ||
                bubbleRoot == null || bubbleBackground == null)
            {
                throw new InvalidOperationException(
                    "TutorialWidget의 배치 대상 RectTransform을 찾지 못했습니다.");
            }

            Undo.RecordObject(view, "튜토리얼 말풍선 배치 기본값 연결");
            SerializedObject serialized = new(view);
            serialized.FindProperty("layoutRoot").objectReferenceValue = layoutRoot;
            serialized.FindProperty("portraitRoot").objectReferenceValue = portraitRoot;
            serialized.FindProperty("bubbleRoot").objectReferenceValue = bubbleRoot;
            serialized.FindProperty("bubbleBackground").objectReferenceValue =
                bubbleBackground;
            serialized.FindProperty("bubbleBorder").objectReferenceValue =
                bubbleBorder;
            serialized.ApplyModifiedProperties();

            view.CaptureCurrentLayout(TutorialSpeakerSide.Left);
            view.CreateMirroredRightLayout();
            EditorUtility.SetDirty(view);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            SceneManager.SetActiveScene(scene);
            Selection.activeGameObject = view.gameObject;
            Debug.Log(
                "[Tutorial] 현재 배치를 Left로 저장하고 Right 대칭 배치를 생성했습니다.",
                view);
        }

        private static Scene OpenOverlayScene()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(
                    ScenePath,
                    OpenSceneMode.Additive);
            }

            return scene;
        }

        private static RectTransform FindRect(Transform root, string path)
        {
            Transform target = root.Find(path);
            return target as RectTransform;
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
