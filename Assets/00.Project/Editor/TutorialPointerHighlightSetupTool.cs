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
    /// 손가락 강조 UI와 Runtime 컴포넌트를 Tutorial Overlay에 연결합니다.
    /// 반복 실행해도 동일한 오브젝트와 컴포넌트를 재사용합니다.
    /// </summary>
    public static class TutorialPointerHighlightSetupTool
    {
        private const string ConfigPath =
            "Assets/00.Project/03.ScriptableObjects/Tutorial/TutorialConfig.asset";
        private const string ScenePath =
            "Assets/00.Project/00.Scenes/Project_Scene/tutorial_Overlay.unity";
        private const string HandSpritePath =
            "Assets/01.TeamBranch/A.Heesu/Resources/tutorial/ClickIndicator.png";
        private const string HandSpriteName = "ClickIndicator_0";
        private const string MenuPath =
            "Tools/TaskTown/Tutorial/손가락 강조 UI 설정 적용";

        [MenuItem(MenuPath)]
        public static void ApplyFromMenu()
        {
            TutorialConfigSO config = LoadConfig();
            ApplyConfig(config);
            ApplyOverlayScene(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static TutorialConfigSO LoadConfig()
        {
            TutorialConfigSO config =
                AssetDatabase.LoadAssetAtPath<TutorialConfigSO>(ConfigPath);
            return config != null
                ? config
                : throw new InvalidOperationException(
                    $"튜토리얼 Config를 찾지 못했습니다: {ConfigPath}");
        }

        private static void ApplyConfig(TutorialConfigSO config)
        {
            Undo.RecordObject(config, "튜토리얼 손가락 강조 기본값 적용");
            SerializedObject serialized = new(config);
            SerializedProperty steps = serialized.FindProperty("steps");

            for (int index = 0; index < steps.arraySize; index++)
            {
                SerializedProperty content = steps.GetArrayElementAtIndex(index);
                content.FindPropertyRelative("highlightEffects").intValue =
                    (int)TutorialHighlightEffect.None;
            }

            SetHighlightDefaults(
                steps,
                TutorialStep.EarnManualCoin,
                TutorialHighlightEffect.FocusRing);
            SetHighlightDefaults(
                steps,
                TutorialStep.CollapseAndExpandTown,
                TutorialHighlightEffect.ScalePulse |
                TutorialHighlightEffect.Pointer |
                TutorialHighlightEffect.FocusRing);
            SetHighlightDefaults(
                steps,
                TutorialStep.DrawAnimal,
                TutorialHighlightEffect.ScalePulse | TutorialHighlightEffect.Pointer);
            SetHighlightDefaults(
                steps,
                TutorialStep.DrawTool,
                TutorialHighlightEffect.ScalePulse | TutorialHighlightEffect.Pointer);
            SetHighlightDefaults(
                steps,
                TutorialStep.AssignAnimal,
                TutorialHighlightEffect.ScalePulse | TutorialHighlightEffect.Pointer);
            SetHighlightDefaults(
                steps,
                TutorialStep.PlaceAnimalInVillage,
                TutorialHighlightEffect.ScalePulse | TutorialHighlightEffect.Pointer);
            SetHighlightDefaults(
                steps,
                TutorialStep.OpenVillageInfo,
                TutorialHighlightEffect.Pointer);
            SetHighlightDefaults(
                steps,
                TutorialStep.UpgradeVillage,
                TutorialHighlightEffect.ScalePulse | TutorialHighlightEffect.Pointer);

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(config);
        }

        private static void SetHighlightDefaults(
            SerializedProperty steps,
            TutorialStep step,
            TutorialHighlightEffect effects)
        {
            SerializedProperty content = FindStep(steps, step) ??
                throw new InvalidOperationException(
                    $"TutorialConfig에 {step} 단계가 없습니다.");

            content.FindPropertyRelative("highlightEffects").intValue = (int)effects;
            content.FindPropertyRelative("pointerPositionMode").intValue =
                (int)TutorialPointerPositionMode.FollowHighlightedButton;
            content.FindPropertyRelative("pointerOffset").vector2Value = Vector2.zero;
            content.FindPropertyRelative("pointerCanvasPosition").vector2Value = Vector2.zero;
            content.FindPropertyRelative("pointerRingSize").floatValue = 120f;
            content.FindPropertyRelative("pointerRingPadding").floatValue = 24f;
        }

        private static SerializedProperty FindStep(
            SerializedProperty steps,
            TutorialStep step)
        {
            for (int index = 0; index < steps.arraySize; index++)
            {
                SerializedProperty content = steps.GetArrayElementAtIndex(index);
                if (content.FindPropertyRelative("step").intValue == (int)step)
                    return content;
            }

            return null;
        }

        private static void ApplyOverlayScene(TutorialConfigSO config)
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            TutorialEventBridge bridge = FindComponentInScene<TutorialEventBridge>(scene);
            Canvas tutorialCanvas = FindNamedComponentInScene<Canvas>(
                scene,
                "TutorialCanvas");
            if (bridge == null || tutorialCanvas == null)
            {
                throw new InvalidOperationException(
                    "Tutorial Overlay에서 EventBridge 또는 TutorialCanvas를 찾지 못했습니다.");
            }

            Sprite handSprite = LoadHandSprite();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("튜토리얼 손가락 강조 UI 설정");

            TutorialButtonHighlighter scaleHighlighter =
                GetOrAddComponent<TutorialButtonHighlighter>(bridge.gameObject);
            TutorialPointerIndicator pointerIndicator =
                GetOrAddComponent<TutorialPointerIndicator>(bridge.gameObject);
            TutorialHighlightCoordinator coordinator =
                GetOrAddComponent<TutorialHighlightCoordinator>(bridge.gameObject);
            TutorialManualCoinFeedback manualCoinFeedback =
                GetOrAddComponent<TutorialManualCoinFeedback>(bridge.gameObject);

            RectTransform pointerRoot = GetOrCreateRectChild(
                tutorialCanvas.transform,
                "TutorialPointerRoot");
            pointerRoot.SetAsLastSibling();
            pointerRoot.anchorMin = new Vector2(0.5f, 0.5f);
            pointerRoot.anchorMax = new Vector2(0.5f, 0.5f);
            pointerRoot.pivot = new Vector2(0.5f, 0.5f);
            pointerRoot.anchoredPosition = Vector2.zero;
            pointerRoot.sizeDelta = Vector2.zero;

            CanvasGroup pointerCanvasGroup =
                GetOrAddComponent<CanvasGroup>(pointerRoot.gameObject);
            pointerCanvasGroup.alpha = 0f;
            pointerCanvasGroup.interactable = false;
            pointerCanvasGroup.blocksRaycasts = false;

            RectTransform ringTransform = GetOrCreateRectChild(
                pointerRoot,
                "FocusRing");
            ringTransform.anchorMin = new Vector2(0.5f, 0.5f);
            ringTransform.anchorMax = new Vector2(0.5f, 0.5f);
            ringTransform.pivot = new Vector2(0.5f, 0.5f);
            ringTransform.anchoredPosition = Vector2.zero;
            ringTransform.sizeDelta = Vector2.one * 120f;

            TutorialFocusRingGraphic ringGraphic =
                GetOrAddComponent<TutorialFocusRingGraphic>(ringTransform.gameObject);
            GetOrAddComponent<CanvasRenderer>(ringTransform.gameObject);
            ringGraphic.color = new Color(1f, 0.72f, 0.18f, 0.9f);
            ringGraphic.raycastTarget = false;

            RectTransform handTransform = GetOrCreateRectChild(
                pointerRoot,
                "HandImage");
            handTransform.anchorMin = new Vector2(0.5f, 0.5f);
            handTransform.anchorMax = new Vector2(0.5f, 0.5f);
            handTransform.pivot = new Vector2(0.25f, 0.88f);
            handTransform.anchoredPosition = Vector2.zero;
            handTransform.sizeDelta = new Vector2(120f, 145f);

            Image handImage = GetOrAddComponent<Image>(handTransform.gameObject);
            handImage.sprite = handSprite;
            handImage.preserveAspect = true;
            handImage.raycastTarget = false;

            SerializedObject pointerSerialized = new(pointerIndicator);
            pointerSerialized.FindProperty("overlayCanvas").objectReferenceValue =
                tutorialCanvas;
            pointerSerialized.FindProperty("indicatorRoot").objectReferenceValue =
                pointerRoot;
            pointerSerialized.FindProperty("indicatorCanvasGroup").objectReferenceValue =
                pointerCanvasGroup;
            pointerSerialized.FindProperty("ringTransform").objectReferenceValue =
                ringTransform;
            pointerSerialized.FindProperty("handTransform").objectReferenceValue =
                handTransform;
            pointerSerialized.ApplyModifiedProperties();

            SerializedObject coordinatorSerialized = new(coordinator);
            coordinatorSerialized.FindProperty("config").objectReferenceValue = config;
            coordinatorSerialized.FindProperty("scaleHighlighter").objectReferenceValue =
                scaleHighlighter;
            coordinatorSerialized.FindProperty("pointerIndicator").objectReferenceValue =
                pointerIndicator;
            coordinatorSerialized.ApplyModifiedProperties();

            SerializedObject bridgeSerialized = new(bridge);
            bridgeSerialized.FindProperty("buttonHighlighter").objectReferenceValue =
                scaleHighlighter;
            bridgeSerialized.FindProperty("highlightCoordinator").objectReferenceValue =
                coordinator;
            bridgeSerialized.FindProperty("manualCoinFeedback").objectReferenceValue =
                manualCoinFeedback;
            bridgeSerialized.ApplyModifiedProperties();

            pointerRoot.gameObject.SetActive(false);
            EditorUtility.SetDirty(pointerIndicator);
            EditorUtility.SetDirty(coordinator);
            EditorUtility.SetDirty(manualCoinFeedback);
            EditorUtility.SetDirty(bridge);
            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Selection.activeGameObject = pointerRoot.gameObject;
            Debug.Log(
                "[Tutorial] 손가락 강조 UI와 독립 효과 Coordinator 연결을 적용했습니다.",
                bridge);
        }

        private static Sprite LoadHandSprite()
        {
            foreach (UnityEngine.Object asset in
                     AssetDatabase.LoadAllAssetsAtPath(HandSpritePath))
            {
                if (asset is Sprite sprite && sprite.name == HandSpriteName)
                    return sprite;
            }

            throw new InvalidOperationException(
                $"손가락 Sprite {HandSpriteName}을 찾지 못했습니다: {HandSpritePath}");
        }

        private static RectTransform GetOrCreateRectChild(
            Transform parent,
            string objectName)
        {
            Transform existing = parent.Find(objectName);
            if (existing != null && existing is RectTransform existingRect)
                return existingRect;

            GameObject created = new(objectName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(created, $"{objectName} 생성");
            created.layer = parent.gameObject.layer;
            RectTransform rect = created.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static T GetOrAddComponent<T>(GameObject target)
            where T : Component
        {
            return target.TryGetComponent(out T existing)
                ? existing
                : Undo.AddComponent<T>(target);
        }

        private static T FindComponentInScene<T>(Scene scene)
            where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null)
                    return component;
            }

            return null;
        }

        private static T FindNamedComponentInScene<T>(
            Scene scene,
            string objectName)
            where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (T component in root.GetComponentsInChildren<T>(true))
                {
                    if (component.gameObject.name == objectName)
                        return component;
                }
            }

            return null;
        }
    }
}
