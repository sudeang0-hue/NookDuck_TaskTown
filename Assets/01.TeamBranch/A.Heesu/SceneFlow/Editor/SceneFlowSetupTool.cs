using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TaskTown.SceneFlow;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TaskTown.SceneFlowEditor
{
    /// <summary>
    /// Scene 파일을 텍스트로 수정하지 않고 Unity Editor API로 시작 Flow를 생성합니다.
    /// 기존 동명 Scene과 에셋은 덮어쓰지 않습니다.
    /// </summary>
    [InitializeOnLoad]
    public static class SceneFlowSetupTool
    {
        private const string RootFolder = "Assets/01.TeamBranch/A.Heesu";
        private const string SceneFolder = RootFolder + "/Scenes";
        private const string ResourceFolder = RootFolder + "/Resources/SceneFlow";
        private const string StepFolder = ResourceFolder + "/Steps";

        private const string LogoScenePath = SceneFolder + "/TeamLogoScene.unity";
        private const string TitleScenePath = SceneFolder + "/TitleScene.unity";
        private const string MainScenePath = "Assets/00.Project/00.Scenes/Project_Scene/MainScene.unity";

        private const string CatalogPath = ResourceFolder + "/SceneCatalog.asset";
        private const string LoadPlanPath = ResourceFolder + "/StartupLoadPlan.asset";
        private const string CoreStepPath = StepFolder + "/CoreValidationStep.asset";
        private const string SoundStepPath = StepFolder + "/LoadSoundSettingsStep.asset";
        private const string PreloadStepPath = StepFolder + "/PreloadMainSceneStep.asset";

        private const string AutoRunSessionKey = "TaskTown.SceneFlowSetupTool.AutoRun.1";
        private const string ValidationSessionKey = "TaskTown.SceneFlowSetupTool.ValidationRunning";
        private const string ValidationReportPath = "Temp/SceneFlowValidationResult.txt";
        private const double ValidationTimeoutSeconds = 30d;
        private static bool autoRunQueued;
        private static double validationStartedAt;

        static SceneFlowSetupTool()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            QueueAutoGeneration();
        }

        [MenuItem("Tools/TaskTown/Scene Flow/Generate Startup Flow %#g")]
        public static void GenerateStartupFlow()
        {
            try
            {
                EnsureFolder(SceneFolder);
                EnsureFolder(ResourceFolder);
                EnsureFolder(StepFolder);

                SceneCatalogSO catalog = CreateAssetIfMissing<SceneCatalogSO>(CatalogPath, out bool catalogCreated);
                if (catalogCreated || IsArrayPropertyEmpty(catalog, "entries"))
                {
                    ConfigureCatalog(catalog);
                }

                CoreValidationStep coreStep = CreateAssetIfMissing<CoreValidationStep>(CoreStepPath, out bool coreCreated);
                if (coreCreated)
                {
                    ConfigureStep(coreStep, StartupLoadPhase.CoreValidation, 10, 1f, "필수 설정 확인");
                }

                LoadSoundSettingsStep soundStep = CreateAssetIfMissing<LoadSoundSettingsStep>(SoundStepPath, out bool soundCreated);
                if (soundCreated)
                {
                    ConfigureStep(soundStep, StartupLoadPhase.LocalSettings, 110, 1f, "사운드 설정 불러오기");
                }

                PreloadMainSceneStep preloadStep = CreateAssetIfMissing<PreloadMainSceneStep>(PreloadStepPath, out bool preloadCreated);
                if (preloadCreated)
                {
                    ConfigureStep(preloadStep, StartupLoadPhase.ScenePreload, 710, 8f, "메인 Scene 불러오기");
                }

                StartupLoadPlanSO loadPlan = CreateAssetIfMissing<StartupLoadPlanSO>(LoadPlanPath, out bool planCreated);
                if (planCreated || IsArrayPropertyEmpty(loadPlan, "steps"))
                {
                    ConfigureLoadPlan(loadPlan, coreStep, soundStep, preloadStep);
                }

                GenerateLogoSceneIfMissing();
                GenerateTitleSceneIfMissing(loadPlan);
                ConfigureBuildSettings();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                SessionState.SetBool(AutoRunSessionKey, true);

                Debug.Log(
                    "[SceneFlowSetupTool] 시작 Flow 생성 완료: " +
                    "TeamLogoScene → TitleScene → MainScene");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SceneFlowSetupTool] 시작 Flow 생성 실패\n{exception}");
                throw;
            }
        }

        [MenuItem("Tools/TaskTown/Scene Flow/Select Startup Load Plan")]
        private static void SelectStartupLoadPlan()
        {
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<StartupLoadPlanSO>(LoadPlanPath);
        }

        [MenuItem("Tools/TaskTown/Scene Flow/Validate Startup Flow %#t")]
        public static void ValidateStartupFlow()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[SceneFlowSetupTool] Play Mode에서는 검증을 시작할 수 없습니다.");
                return;
            }

            SceneAsset logoScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(LogoScenePath);
            if (logoScene == null)
            {
                Debug.LogError("[SceneFlowSetupTool] TeamLogoScene을 찾지 못했습니다.");
                return;
            }

            if (File.Exists(ValidationReportPath))
            {
                File.Delete(ValidationReportPath);
            }

            SessionState.SetBool(ValidationSessionKey, true);
            EditorSceneManager.playModeStartScene = logoScene;
            EditorApplication.isPlaying = true;
        }

        private static void QueueAutoGeneration()
        {
            if (autoRunQueued || SessionState.GetBool(AutoRunSessionKey, false))
            {
                return;
            }

            autoRunQueued = true;
            EditorApplication.delayCall += TryAutoGenerate;
        }

        private static void TryAutoGenerate()
        {
            autoRunQueued = false;

            if (SessionState.GetBool(AutoRunSessionKey, false) || IsSetupComplete())
            {
                SessionState.SetBool(AutoRunSessionKey, true);
                return;
            }

            if (EditorApplication.isCompiling ||
                EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode)
            {
                QueueAutoGeneration();
                return;
            }

            GenerateStartupFlow();
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(ValidationSessionKey, false))
            {
                return;
            }

            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                validationStartedAt = EditorApplication.timeSinceStartup;
                EditorApplication.update -= MonitorStartupFlow;
                EditorApplication.update += MonitorStartupFlow;
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                EditorApplication.update -= MonitorStartupFlow;
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorSceneManager.playModeStartScene = null;
                SessionState.SetBool(ValidationSessionKey, false);
            }
        }

        private static void MonitorStartupFlow()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid() && activeScene.name == "MainScene")
            {
                string message = "SUCCESS: TeamLogoScene -> TitleScene -> MainScene";
                File.WriteAllText(ValidationReportPath, message);
                Debug.Log($"[SceneFlowSetupTool] {message}");
                EditorSceneManager.playModeStartScene = null;
                EditorApplication.update -= MonitorStartupFlow;
                EditorApplication.isPlaying = false;
                return;
            }

            if (EditorApplication.timeSinceStartup - validationStartedAt < ValidationTimeoutSeconds)
            {
                return;
            }

            string errorMessage = $"FAILED: 제한 시간 안에 MainScene에 도달하지 못했습니다. Current={activeScene.name}";
            File.WriteAllText(ValidationReportPath, errorMessage);
            Debug.LogError($"[SceneFlowSetupTool] {errorMessage}");
            EditorSceneManager.playModeStartScene = null;
            EditorApplication.update -= MonitorStartupFlow;
            EditorApplication.isPlaying = false;
        }

        private static bool IsSetupComplete()
        {
            return AssetDatabase.LoadAssetAtPath<SceneCatalogSO>(CatalogPath) != null &&
                   AssetDatabase.LoadAssetAtPath<StartupLoadPlanSO>(LoadPlanPath) != null &&
                   AssetDatabase.LoadAssetAtPath<SceneAsset>(LogoScenePath) != null &&
                   AssetDatabase.LoadAssetAtPath<SceneAsset>(TitleScenePath) != null;
        }

        private static void GenerateLogoSceneIfMissing()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(LogoScenePath) != null)
            {
                return;
            }

            CreateAndSaveAdditiveScene(LogoScenePath, () =>
            {
                Canvas canvas = CreateCanvas("TeamLogoCanvas");
                CreateFullScreenImage(canvas.transform, "Background", new Color(0.025f, 0.035f, 0.07f, 1f));

                GameObject logoGroupObject = new("LogoGroup", typeof(RectTransform), typeof(CanvasGroup));
                logoGroupObject.transform.SetParent(canvas.transform, false);
                SetFullStretch(logoGroupObject.GetComponent<RectTransform>());

                CanvasGroup logoCanvasGroup = logoGroupObject.GetComponent<CanvasGroup>();
                CreateText(
                    logoGroupObject.transform,
                    "TaskTownLogo",
                    "TASK TOWN",
                    96,
                    FontStyle.Bold,
                    Color.white,
                    new Vector2(1000f, 150f),
                    new Vector2(0f, 35f));
                CreateText(
                    logoGroupObject.transform,
                    "LogoGuide",
                    "TEAM LOGO",
                    30,
                    FontStyle.Normal,
                    new Color(0.55f, 0.75f, 1f, 1f),
                    new Vector2(700f, 70f),
                    new Vector2(0f, -75f));

                GameObject controllerObject = new("TeamLogoFlow", typeof(TeamLogoController));
                TeamLogoController controller = controllerObject.GetComponent<TeamLogoController>();
                SerializedObject serializedController = new(controller);
                serializedController.FindProperty("logoCanvasGroup").objectReferenceValue = logoCanvasGroup;
                serializedController.FindProperty("nextScene").intValue = (int)SceneId.Title;
                serializedController.ApplyModifiedPropertiesWithoutUndo();
            });
        }

        private static void GenerateTitleSceneIfMissing(StartupLoadPlanSO loadPlan)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TitleScenePath) != null)
            {
                return;
            }

            CreateAndSaveAdditiveScene(TitleScenePath, () =>
            {
                Canvas canvas = CreateCanvas("TitleCanvas");
                CreateFullScreenImage(canvas.transform, "Background", new Color(0.018f, 0.026f, 0.055f, 1f));

                CreateText(
                    canvas.transform,
                    "GameTitle",
                    "TASK TOWN",
                    82,
                    FontStyle.Bold,
                    Color.white,
                    new Vector2(1000f, 140f),
                    new Vector2(0f, 150f));

                Slider progressSlider = CreateProgressSlider(canvas.transform);
                Text statusText = CreateText(
                    canvas.transform,
                    "LoadingStatus",
                    "시작 준비  0%",
                    28,
                    FontStyle.Normal,
                    new Color(0.75f, 0.85f, 1f, 1f),
                    new Vector2(1000f, 80f),
                    new Vector2(0f, -175f));

                GameObject loadingObject = new(
                    "StartupLoading",
                    typeof(StartupLoadPipeline),
                    typeof(TitleLoadingController));

                StartupLoadPipeline pipeline = loadingObject.GetComponent<StartupLoadPipeline>();
                SerializedObject serializedPipeline = new(pipeline);
                serializedPipeline.FindProperty("loadPlan").objectReferenceValue = loadPlan;
                serializedPipeline.FindProperty("targetScene").intValue = (int)SceneId.Main;
                serializedPipeline.ApplyModifiedPropertiesWithoutUndo();

                TitleLoadingController controller = loadingObject.GetComponent<TitleLoadingController>();
                SerializedObject serializedController = new(controller);
                serializedController.FindProperty("startupLoadPipeline").objectReferenceValue = pipeline;
                serializedController.FindProperty("progressSlider").objectReferenceValue = progressSlider;
                serializedController.FindProperty("statusText").objectReferenceValue = statusText;
                serializedController.ApplyModifiedPropertiesWithoutUndo();
            });
        }

        private static void CreateAndSaveAdditiveScene(string scenePath, Action buildScene)
        {
            Scene originalActiveScene = SceneManager.GetActiveScene();
            Scene generatedScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(generatedScene);

            try
            {
                buildScene.Invoke();
                EditorSceneManager.MarkSceneDirty(generatedScene);

                if (!EditorSceneManager.SaveScene(generatedScene, scenePath))
                {
                    throw new InvalidOperationException($"Scene 저장에 실패했습니다: {scenePath}");
                }
            }
            finally
            {
                if (generatedScene.IsValid() && generatedScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(generatedScene, true);
                }

                if (originalActiveScene.IsValid() && originalActiveScene.isLoaded)
                {
                    SceneManager.SetActiveScene(originalActiveScene);
                }
            }
        }

        private static Canvas CreateCanvas(string name)
        {
            GameObject canvasObject = new(
                name,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private static Image CreateFullScreenImage(Transform parent, string name, Color color)
        {
            GameObject imageObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            SetFullStretch(imageObject.GetComponent<RectTransform>());

            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string content,
            int fontSize,
            FontStyle fontStyle,
            Color color,
            Vector2 size,
            Vector2 anchoredPosition)
        {
            GameObject textObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);

            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = anchoredPosition;

            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.raycastTarget = false;

            return text;
        }

        private static Slider CreateProgressSlider(Transform parent)
        {
            GameObject sliderObject = new(
                "LoadingProgress",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Slider));
            sliderObject.transform.SetParent(parent, false);

            RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
            sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
            sliderRect.pivot = new Vector2(0.5f, 0.5f);
            sliderRect.sizeDelta = new Vector2(820f, 34f);
            sliderRect.anchoredPosition = new Vector2(0f, -100f);

            Image background = sliderObject.GetComponent<Image>();
            background.color = new Color(0.12f, 0.16f, 0.25f, 1f);
            background.raycastTarget = false;

            GameObject fillAreaObject = new("FillArea", typeof(RectTransform));
            fillAreaObject.transform.SetParent(sliderObject.transform, false);
            RectTransform fillAreaRect = fillAreaObject.GetComponent<RectTransform>();
            SetFullStretch(fillAreaRect);
            fillAreaRect.offsetMin = new Vector2(5f, 5f);
            fillAreaRect.offsetMax = new Vector2(-5f, -5f);

            GameObject fillObject = new("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillObject.transform.SetParent(fillAreaObject.transform, false);
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            SetFullStretch(fillRect);

            Image fillImage = fillObject.GetComponent<Image>();
            fillImage.color = new Color(0.25f, 0.65f, 1f, 1f);
            fillImage.raycastTarget = false;

            Slider slider = sliderObject.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
            slider.wholeNumbers = false;
            slider.direction = Slider.Direction.LeftToRight;
            slider.fillRect = fillRect;
            slider.targetGraphic = background;

            return slider;
        }

        private static void SetFullStretch(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static void ConfigureCatalog(SceneCatalogSO catalog)
        {
            SerializedObject serializedCatalog = new(catalog);
            SerializedProperty entries = serializedCatalog.FindProperty("entries");
            entries.arraySize = 3;

            ConfigureCatalogEntry(entries.GetArrayElementAtIndex(0), SceneId.TeamLogo, "TeamLogoScene");
            ConfigureCatalogEntry(entries.GetArrayElementAtIndex(1), SceneId.Title, "TitleScene");
            ConfigureCatalogEntry(entries.GetArrayElementAtIndex(2), SceneId.Main, "MainScene");

            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        private static void ConfigureCatalogEntry(SerializedProperty entry, SceneId id, string sceneName)
        {
            entry.FindPropertyRelative("id").intValue = (int)id;
            entry.FindPropertyRelative("sceneName").stringValue = sceneName;
        }

        private static void ConfigureStep(
            StartupLoadStepSO step,
            StartupLoadPhase phase,
            int order,
            float weight,
            string displayName)
        {
            SerializedObject serializedStep = new(step);
            serializedStep.FindProperty("phase").intValue = (int)phase;
            serializedStep.FindProperty("order").intValue = order;
            serializedStep.FindProperty("weight").floatValue = weight;
            serializedStep.FindProperty("displayName").stringValue = displayName;
            serializedStep.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(step);
        }

        private static void ConfigureLoadPlan(StartupLoadPlanSO plan, params StartupLoadStepSO[] steps)
        {
            SerializedObject serializedPlan = new(plan);
            SerializedProperty stepList = serializedPlan.FindProperty("steps");
            stepList.arraySize = steps.Length;

            for (int i = 0; i < steps.Length; i++)
            {
                stepList.GetArrayElementAtIndex(i).objectReferenceValue = steps[i];
            }

            serializedPlan.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(plan);
        }

        private static bool IsArrayPropertyEmpty(Object target, string propertyName)
        {
            SerializedObject serializedObject = new(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            return property == null || property.arraySize == 0;
        }

        private static T CreateAssetIfMissing<T>(string assetPath, out bool created) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (existing != null)
            {
                created = false;
                return existing;
            }

            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            created = true;
            return asset;
        }

        private static void ConfigureBuildSettings()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath) == null)
            {
                throw new FileNotFoundException("MainScene을 찾지 못했습니다.", MainScenePath);
            }

            string[] startupPaths = { LogoScenePath, TitleScenePath, MainScenePath };
            List<EditorBuildSettingsScene> scenes = startupPaths
                .Select(path => new EditorBuildSettingsScene(path, true))
                .ToList();

            HashSet<string> startupPathSet = new(startupPaths, StringComparer.Ordinal);
            EditorBuildSettingsScene[] existingScenes = EditorBuildSettings.scenes;

            for (int i = 0; i < existingScenes.Length; i++)
            {
                EditorBuildSettingsScene existing = existingScenes[i];
                if (!startupPathSet.Contains(existing.path))
                {
                    scenes.Add(existing);
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string normalizedPath = folderPath.Replace('\\', '/');
            string[] parts = normalizedPath.Split('/');
            string currentPath = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string nextPath = currentPath + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, parts[i]);
                }

                currentPath = nextPath;
            }
        }
    }
}
