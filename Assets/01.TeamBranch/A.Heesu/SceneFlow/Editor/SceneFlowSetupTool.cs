using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TaskTown.SceneFlow;
using TaskTown.SceneFlow.Testing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
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
        private const string SoundResourceFolder = RootFolder + "/Resources/Sound";

        private const string BootstrapScenePath = SceneFolder + "/BootstrapScene.unity";
        private const string LogoScenePath = SceneFolder + "/TeamLogoScene.unity";
        private const string TitleScenePath = SceneFolder + "/TitleScene.unity";
        private const SceneId StartupTargetSceneId = SceneId.TestMainGame;
        private const string StartupTargetScenePath =
            SceneFolder + "/TestMainGameScene.unity";
        private static readonly Vector2Int BootstrapWindowSize = new(700, 700);
        private static readonly Vector2Int LogoWindowSize = new(900, 900);
        private static readonly Vector2Int TransparentWindowFallbackSize = new(1920, 1080);

        private const string CatalogPath = ResourceFolder + "/SceneCatalog.asset";
        private const string LoadPlanPath = ResourceFolder + "/StartupLoadPlan.asset";
        private const string CoreStepPath = StepFolder + "/CoreValidationStep.asset";
        private const string SoundStepPath = StepFolder + "/LoadSoundSettingsStep.asset";
        private const string PreloadStepPath = StepFolder + "/PreloadMainSceneStep.asset";
        private const string LogoSoundDataPath = SoundResourceFolder + "/TeamLogo_DuckQuack.asset";
        private const string TitleLoadingBgmDataPath = SoundResourceFolder + "/Title_LodingBGM.asset";

        private const string SoundLibraryPath = "Assets/00.Project/07.Audio/SoundLibrary/SoundLibrary.asset";
        private const string AudioMixerPath = "Assets/00.Project/07.Audio/CompanionAudioMixer.mixer";
        private const string LogoSoundClipPathA = RootFolder + "/Resources/Mp3/Logo_Duck1.mp3";
        private const string LogoSoundClipPathB = RootFolder + "/Resources/Mp3/Logo_Duck2.mp3";
        private const string TitleLoadingBgmClipPath = RootFolder + "/Resources/Mp3/Title_LodingBGM.mp3";

        private const string AutoRunSessionKey = "TaskTown.SceneFlowSetupTool.AutoRun.2";
        private const string ValidationSessionKey = "TaskTown.SceneFlowSetupTool.ValidationRunning";
        private const string ValidationTitleBgmObservedKey = "TaskTown.SceneFlowSetupTool.TitleBgmObserved";
        private const string ValidationReportPath = "Temp/SceneFlowValidationResult.txt";
        private const double ValidationTimeoutSeconds = 30d;
        private static bool autoRunQueued;
        private static double validationStartedAt;
        private static string StartupTargetSceneName => Path.GetFileNameWithoutExtension(StartupTargetScenePath);

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
                EnsureFolder(SoundResourceFolder);

                SceneCatalogSO catalog = CreateAssetIfMissing<SceneCatalogSO>(CatalogPath, out _);
                ConfigureCatalog(catalog);

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
                    ConfigureStep(preloadStep, StartupLoadPhase.ScenePreload, 710, 8f, "대상 Scene 불러오기");
                }
                else
                {
                    ConfigureStepDisplayName(preloadStep, "대상 Scene 불러오기");
                }

                StartupLoadPlanSO loadPlan = CreateAssetIfMissing<StartupLoadPlanSO>(LoadPlanPath, out bool planCreated);
                if (planCreated || IsArrayPropertyEmpty(loadPlan, "steps"))
                {
                    ConfigureLoadPlan(loadPlan, coreStep, preloadStep);
                }
                else
                {
                    RemoveStepFromLoadPlan(loadPlan, soundStep);
                }

                SoundClipData logoSoundData = CreateAssetIfMissing<SoundClipData>(LogoSoundDataPath, out bool logoSoundCreated);
                if (logoSoundCreated || IsArrayPropertyEmpty(logoSoundData, "clips"))
                {
                    ConfigureLogoSoundData(logoSoundData);
                }

                SoundClipData titleLoadingBgmData = CreateAssetIfMissing<SoundClipData>(
                    TitleLoadingBgmDataPath,
                    out bool titleLoadingBgmCreated);
                if (titleLoadingBgmCreated || IsArrayPropertyEmpty(titleLoadingBgmData, "clips"))
                {
                    ConfigureTitleLoadingBgmData(titleLoadingBgmData);
                }

                RegisterSoundData(logoSoundData);
                RegisterSoundData(titleLoadingBgmData);

                GenerateBootstrapSceneIfMissing();
                GenerateLogoSceneIfMissing();
                GenerateTitleSceneIfMissing(loadPlan);
                ConfigureStartupScenes();
                ConfigureBuildSettings();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                SessionState.SetBool(AutoRunSessionKey, true);

                Debug.Log(
                    "[SceneFlowSetupTool] 시작 Flow 생성 완료: " +
                    $"BootstrapScene → TeamLogoScene → TitleScene → {StartupTargetSceneName}");
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

            SceneAsset bootstrapScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScenePath);
            if (bootstrapScene == null)
            {
                Debug.LogError("[SceneFlowSetupTool] BootstrapScene을 찾지 못했습니다.");
                return;
            }

            if (File.Exists(ValidationReportPath))
            {
                File.Delete(ValidationReportPath);
            }

            SessionState.SetBool(ValidationSessionKey, true);
            SessionState.SetBool(ValidationTitleBgmObservedKey, false);
            EditorSceneManager.playModeStartScene = bootstrapScene;
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
                SessionState.SetBool(ValidationTitleBgmObservedKey, false);
            }
        }

        private static void MonitorStartupFlow()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid() && activeScene.name == "TitleScene")
            {
                ObserveTitleLoadingBgm();
            }

            if (activeScene.IsValid() && activeScene.name == StartupTargetSceneName)
            {
                if (!SessionState.GetBool(ValidationTitleBgmObservedKey, false))
                {
                    string bgmErrorMessage = "FAILED: TitleScene에서 Title_LodingBGM 재생을 확인하지 못했습니다.";
                    File.WriteAllText(ValidationReportPath, bgmErrorMessage);
                    Debug.LogError($"[SceneFlowSetupTool] {bgmErrorMessage}");
                    EditorSceneManager.playModeStartScene = null;
                    EditorApplication.update -= MonitorStartupFlow;
                    EditorApplication.isPlaying = false;
                    return;
                }

                string message =
                    $"SUCCESS: BootstrapScene -> TeamLogoScene -> " +
                    $"TitleScene(Title_LodingBGM) -> {StartupTargetSceneName}";
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

            string errorMessage =
                $"FAILED: 제한 시간 안에 {StartupTargetSceneName}에 도달하지 못했습니다. " +
                $"Current={activeScene.name}";
            File.WriteAllText(ValidationReportPath, errorMessage);
            Debug.LogError($"[SceneFlowSetupTool] {errorMessage}");
            EditorSceneManager.playModeStartScene = null;
            EditorApplication.update -= MonitorStartupFlow;
            EditorApplication.isPlaying = false;
        }

        private static void ObserveTitleLoadingBgm()
        {
            if (SessionState.GetBool(ValidationTitleBgmObservedKey, false) || SoundManager.Instance == null)
            {
                return;
            }

            AudioSource[] audioSources = SoundManager.Instance.GetComponentsInChildren<AudioSource>(true);
            bool isPlaying = audioSources.Any(source =>
                source != null &&
                source.isPlaying &&
                source.clip != null &&
                source.clip.name == "Title_LodingBGM");

            if (isPlaying)
            {
                SessionState.SetBool(ValidationTitleBgmObservedKey, true);
            }
        }

        private static bool IsSetupComplete()
        {
            return AssetDatabase.LoadAssetAtPath<SceneCatalogSO>(CatalogPath) != null &&
                   AssetDatabase.LoadAssetAtPath<StartupLoadPlanSO>(LoadPlanPath) != null &&
                   AssetDatabase.LoadAssetAtPath<SoundClipData>(LogoSoundDataPath) != null &&
                   AssetDatabase.LoadAssetAtPath<SoundClipData>(TitleLoadingBgmDataPath) != null &&
                   AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScenePath) != null &&
                   AssetDatabase.LoadAssetAtPath<SceneAsset>(LogoScenePath) != null &&
                   AssetDatabase.LoadAssetAtPath<SceneAsset>(TitleScenePath) != null;
        }

        private static void GenerateBootstrapSceneIfMissing()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScenePath) != null)
            {
                return;
            }

            SoundLibrary soundLibrary = AssetDatabase.LoadAssetAtPath<SoundLibrary>(SoundLibraryPath);
            AudioMixer audioMixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(AudioMixerPath);

            if (soundLibrary == null)
            {
                throw new FileNotFoundException("SoundLibrary를 찾지 못했습니다.", SoundLibraryPath);
            }

            if (audioMixer == null)
            {
                throw new FileNotFoundException("CompanionAudioMixer를 찾지 못했습니다.", AudioMixerPath);
            }

            CreateAndSaveAdditiveScene(BootstrapScenePath, () =>
            {
                GameObject appRoot = new(
                    "AppRoot",
                    typeof(SceneFlowManager),
                    typeof(SoundSettingsApplier),
                    typeof(SoundManager));

                SoundSettingsApplier settingsApplier = appRoot.GetComponent<SoundSettingsApplier>();
                SerializedObject serializedSettings = new(settingsApplier);
                serializedSettings.FindProperty("audioMixer").objectReferenceValue = audioMixer;
                serializedSettings.FindProperty("dontDestroyOnLoad").boolValue = true;
                serializedSettings.ApplyModifiedPropertiesWithoutUndo();

                GameObject bgmSourceObject = new("BGM AudioSource", typeof(AudioSource));
                bgmSourceObject.transform.SetParent(appRoot.transform, false);
                AudioSource bgmSource = bgmSourceObject.GetComponent<AudioSource>();
                bgmSource.playOnAwake = false;
                bgmSource.loop = true;
                bgmSource.spatialBlend = 0f;

                GameObject oneShotRoot = new("One Shot AudioSources");
                oneShotRoot.transform.SetParent(appRoot.transform, false);

                SoundManager soundManager = appRoot.GetComponent<SoundManager>();
                SerializedObject serializedSoundManager = new(soundManager);
                serializedSoundManager.FindProperty("settingsApplier").objectReferenceValue = settingsApplier;
                serializedSoundManager.FindProperty("applySavedSettingsOnAwake").boolValue = true;
                serializedSoundManager.FindProperty("dontDestroyOnLoad").boolValue = true;
                serializedSoundManager.FindProperty("soundLibrary").objectReferenceValue = soundLibrary;
                serializedSoundManager.FindProperty("bgmMixerGroup").objectReferenceValue = FindMixerGroup(audioMixer, "BGM");
                serializedSoundManager.FindProperty("uiMixerGroup").objectReferenceValue = FindMixerGroup(audioMixer, "UI");
                serializedSoundManager.FindProperty("environmentMixerGroup").objectReferenceValue = FindMixerGroup(audioMixer, "Environment");
                serializedSoundManager.FindProperty("bgmSource").objectReferenceValue = bgmSource;
                serializedSoundManager.FindProperty("oneShotSourceRoot").objectReferenceValue = oneShotRoot.transform;
                serializedSoundManager.FindProperty("oneShotPoolSize").intValue = 8;
                serializedSoundManager.FindProperty("playBgmOnStart").boolValue = false;
                serializedSoundManager.FindProperty("startBgmSoundId").stringValue = string.Empty;
                serializedSoundManager.ApplyModifiedPropertiesWithoutUndo();

                GameObject bootstrapObject = new("BootstrapFlow", typeof(BootstrapController));
                BootstrapController bootstrapController = bootstrapObject.GetComponent<BootstrapController>();
                SerializedObject serializedBootstrap = new(bootstrapController);
                serializedBootstrap.FindProperty("nextScene").intValue = (int)SceneId.TeamLogo;
                serializedBootstrap.FindProperty("requireSoundManager").boolValue = true;
                serializedBootstrap.ApplyModifiedPropertiesWithoutUndo();

                EnsureBootstrapWindowInitializer(SceneManager.GetActiveScene());
                CreateStartupCamera("BootstrapCamera");
            });
        }

        private static void ConfigureStartupScenes()
        {
            ConfigureScene(BootstrapScenePath, scene =>
            {
                Camera camera = EnsureStartupCamera(scene, "BootstrapCamera");
                ConfigureTransparentCamera(camera);
                EnsureBootstrapWindowInitializer(scene);
            });
            ConfigureScene(LogoScenePath, scene =>
            {
                Camera camera = EnsureStartupCamera(scene, "TeamLogoCamera");
                ConfigureTransparentCamera(camera);

                TeamLogoController controller = FindComponentInScene<TeamLogoController>(scene);
                if (controller == null)
                {
                    throw new InvalidOperationException("TeamLogoScene에서 TeamLogoController를 찾지 못했습니다.");
                }

                SerializedObject serializedController = new(controller);
                serializedController.FindProperty("playLogoSoundAfterFadeIn").boolValue = true;
                serializedController.FindProperty("logoSoundId").stringValue = "TeamLogo_DuckQuack";
                serializedController.ApplyModifiedPropertiesWithoutUndo();

                EnsureWindowResolutionController(
                    controller.gameObject,
                    false,
                    LogoWindowSize,
                    true);
                ConfigureLogoCanvas(scene);
            });
            ConfigureScene(TitleScenePath, scene =>
            {
                Camera camera = EnsureStartupCamera(scene, "TitleCamera");
                ConfigureTransparentWindowCamera(camera);

                TitleLoadingController controller = FindComponentInScene<TitleLoadingController>(scene);
                if (controller == null)
                {
                    throw new InvalidOperationException("TitleScene에서 TitleLoadingController를 찾지 못했습니다.");
                }

                SerializedObject serializedController = new(controller);
                serializedController.FindProperty("playLoadingBgmOnStart").boolValue = true;
                serializedController.FindProperty("loadingBgmSoundId").stringValue = "Title_LodingBGM";
                serializedController.FindProperty("stopLoadingBgmOnExit").boolValue = true;
                serializedController.ApplyModifiedPropertiesWithoutUndo();

                if (controller.TryGetComponent(out SceneWindowResolutionController oldWindowController))
                {
                    Object.DestroyImmediate(oldWindowController);
                }

                GameObject windowInitializer = EnsureTitleWindowInitializer(scene);
                EnsureWindowResolutionController(
                    windowInitializer,
                    true,
                    TransparentWindowFallbackSize,
                    false);
                ConfigureTitleCanvas(scene);
            });
            ConfigureScene(StartupTargetScenePath, ConfigureTargetWindowTest);
        }

        private static void ConfigureScene(string scenePath, Action<Scene> configure)
        {
            Scene originalActiveScene = SceneManager.GetActiveScene();
            Scene targetScene = SceneManager.GetSceneByPath(scenePath);
            bool openedForConfiguration = !targetScene.IsValid() || !targetScene.isLoaded;

            if (openedForConfiguration)
            {
                targetScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            }

            SceneManager.SetActiveScene(targetScene);

            try
            {
                configure.Invoke(targetScene);
                EditorSceneManager.MarkSceneDirty(targetScene);
                EditorSceneManager.SaveScene(targetScene);
            }
            finally
            {
                if (openedForConfiguration && targetScene.IsValid() && targetScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(targetScene, true);
                }

                if (originalActiveScene.IsValid() && originalActiveScene.isLoaded)
                {
                    SceneManager.SetActiveScene(originalActiveScene);
                }
            }
        }

        private static Camera EnsureStartupCamera(Scene scene, string cameraName)
        {
            Camera camera = FindComponentInScene<Camera>(scene);
            if (camera == null)
            {
                return CreateStartupCamera(cameraName);
            }

            if (!camera.TryGetComponent(out AudioListener _))
            {
                camera.gameObject.AddComponent<AudioListener>();
            }

            return camera;
        }

        private static Camera CreateStartupCamera(string cameraName)
        {
            GameObject cameraObject = new(cameraName, typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";

            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.cullingMask = 0;
            camera.orthographic = true;
            return camera;
        }

        private static void ConfigureTransparentCamera(Camera camera)
        {
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
        }

        private static void ConfigureTransparentWindowCamera(Camera camera)
        {
            ConfigureTransparentCamera(camera);
            camera.allowHDR = false;
            camera.allowMSAA = false;
        }

        private static void EnsureBootstrapWindowInitializer(Scene scene)
        {
            GameObject initializer = FindRootGameObject(scene, "BootstrapWindowInitializer");
            if (initializer == null)
            {
                initializer = new GameObject("BootstrapWindowInitializer");
            }

            if (initializer.TryGetComponent(out TransparentWindow originalWindow))
            {
                Object.DestroyImmediate(originalWindow);
            }

            if (initializer.TryGetComponent(out TransparentWindowFlowTest testWindow))
            {
                Object.DestroyImmediate(testWindow);
            }

            EnsureWindowResolutionController(initializer, false, BootstrapWindowSize, true);
        }

        private static GameObject EnsureTitleWindowInitializer(Scene scene)
        {
            GameObject initializer = FindRootGameObject(scene, "TitleWindowInitializer");
            if (initializer == null)
            {
                initializer = new GameObject("TitleWindowInitializer");
            }

            if (initializer.TryGetComponent(out TransparentWindow originalWindow))
            {
                Object.DestroyImmediate(originalWindow);
            }

            if (!initializer.TryGetComponent(out TransparentWindowFlowTest _))
            {
                initializer.AddComponent<TransparentWindowFlowTest>();
            }

            return initializer;
        }

        private static void ConfigureTargetWindowTest(Scene scene)
        {
            Camera camera = FindComponentInScene<Camera>(scene);
            if (camera == null)
            {
                throw new InvalidOperationException(
                    $"{StartupTargetSceneName}에서 투명 창에 사용할 Camera를 찾지 못했습니다.");
            }

            ConfigureTransparentWindowCamera(camera);

            TransparentWindow originalWindow = FindComponentInScene<TransparentWindow>(scene);
            GameObject host = originalWindow != null
                ? originalWindow.gameObject
                : FindRootGameObject(scene, "_Window");

            if (host == null)
            {
                host = new GameObject("SceneFlowWindowTest");
            }

            if (originalWindow != null)
            {
                Object.DestroyImmediate(originalWindow);
            }

            if (!host.TryGetComponent(out TransparentWindowFlowTest _))
            {
                host.AddComponent<TransparentWindowFlowTest>();
            }
        }

        private static void EnsureWindowResolutionController(
            GameObject target,
            bool useCurrentDisplayResolution,
            Vector2Int fallbackSize,
            bool centerOnCurrentMonitor)
        {
            if (!target.TryGetComponent(out SceneWindowResolutionController controller))
            {
                controller = target.AddComponent<SceneWindowResolutionController>();
            }

            SerializedObject serializedController = new(controller);
            serializedController.FindProperty("useCurrentDisplayResolution").boolValue =
                useCurrentDisplayResolution;
            serializedController.FindProperty("windowSize").vector2IntValue = fallbackSize;
            serializedController.FindProperty("centerOnCurrentMonitor").boolValue =
                centerOnCurrentMonitor;
            serializedController.FindProperty("applyInEditor").boolValue = false;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureLogoCanvas(Scene scene)
        {
            Canvas canvas = FindComponentInScene<Canvas>(scene);
            if (canvas == null)
            {
                throw new InvalidOperationException("TeamLogoScene에서 Canvas를 찾지 못했습니다.");
            }

            if (canvas.TryGetComponent(out CanvasScaler scaler))
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = LogoWindowSize;
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }

            Image background = FindChildImage(canvas.transform, "Background");
            if (background == null)
            {
                throw new InvalidOperationException("TeamLogoScene에서 Background Image를 찾지 못했습니다.");
            }

            SetFullStretch(background.rectTransform);
            Color color = background.color;
            color.a = 1f;
            background.color = color;
            background.raycastTarget = false;
        }

        private static void ConfigureTitleCanvas(Scene scene)
        {
            Canvas canvas = FindComponentInScene<Canvas>(scene);
            if (canvas == null)
            {
                throw new InvalidOperationException("TitleScene에서 Canvas를 찾지 못했습니다.");
            }

            Image background = FindChildImage(canvas.transform, "Background");
            if (background == null)
            {
                throw new InvalidOperationException("TitleScene에서 Background Image를 찾지 못했습니다.");
            }

            Color color = background.color;
            color.a = 0f;
            background.color = color;
            background.raycastTarget = false;
        }

        private static Image FindChildImage(Transform root, string objectName)
        {
            Image[] images = root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != null && images[i].name == objectName)
                {
                    return images[i];
                }
            }

            return null;
        }

        private static GameObject FindRootGameObject(Scene scene, string objectName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] != null && roots[i].name == objectName)
                {
                    return roots[i];
                }
            }

            return null;
        }

        private static T FindComponentInScene<T>(Scene scene) where T : Component
        {
            T[] components = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] != null && components[i].gameObject.scene == scene)
                {
                    return components[i];
                }
            }

            return null;
        }

        private static AudioMixerGroup FindMixerGroup(AudioMixer audioMixer, string groupName)
        {
            AudioMixerGroup group = audioMixer
                .FindMatchingGroups(groupName)
                .FirstOrDefault(candidate => candidate != null && candidate.name == groupName);

            if (group == null)
            {
                throw new InvalidOperationException($"AudioMixer Group을 찾지 못했습니다: {groupName}");
            }

            return group;
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
                serializedPipeline.FindProperty("targetScene").intValue = (int)StartupTargetSceneId;
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

        private static void ConfigureLogoSoundData(SoundClipData soundData)
        {
            AudioClip clipA = AssetDatabase.LoadAssetAtPath<AudioClip>(LogoSoundClipPathA);
            AudioClip clipB = AssetDatabase.LoadAssetAtPath<AudioClip>(LogoSoundClipPathB);

            if (clipA == null || clipB == null)
            {
                throw new FileNotFoundException(
                    "팀 로고용 AudioClip 두 개를 모두 찾지 못했습니다. " +
                    $"Paths: {LogoSoundClipPathA}, {LogoSoundClipPathB}");
            }

            SerializedObject serializedSoundData = new(soundData);
            serializedSoundData.FindProperty("soundId").stringValue = "TeamLogo_DuckQuack";
            serializedSoundData.FindProperty("category").intValue = (int)SoundCategory.UI;

            SerializedProperty clips = serializedSoundData.FindProperty("clips");
            clips.arraySize = 2;
            clips.GetArrayElementAtIndex(0).objectReferenceValue = clipA;
            clips.GetArrayElementAtIndex(1).objectReferenceValue = clipB;

            serializedSoundData.FindProperty("volumeScale").floatValue = 1f;
            serializedSoundData.FindProperty("loop").boolValue = false;
            serializedSoundData.FindProperty("randomizePitch").boolValue = false;
            serializedSoundData.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(soundData);
        }

        private static void ConfigureTitleLoadingBgmData(SoundClipData soundData)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(TitleLoadingBgmClipPath);
            if (clip == null)
            {
                throw new FileNotFoundException(
                    "Title 로딩용 AudioClip을 찾지 못했습니다.",
                    TitleLoadingBgmClipPath);
            }

            SerializedObject serializedSoundData = new(soundData);
            serializedSoundData.FindProperty("soundId").stringValue = "Title_LodingBGM";
            serializedSoundData.FindProperty("category").intValue = (int)SoundCategory.BGM;

            SerializedProperty clips = serializedSoundData.FindProperty("clips");
            clips.arraySize = 1;
            clips.GetArrayElementAtIndex(0).objectReferenceValue = clip;

            serializedSoundData.FindProperty("volumeScale").floatValue = 1f;
            serializedSoundData.FindProperty("loop").boolValue = true;
            serializedSoundData.FindProperty("randomizePitch").boolValue = false;
            serializedSoundData.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(soundData);
        }

        private static void RegisterSoundData(SoundClipData soundData)
        {
            SoundLibrary soundLibrary = AssetDatabase.LoadAssetAtPath<SoundLibrary>(SoundLibraryPath);
            if (soundLibrary == null)
            {
                throw new FileNotFoundException("SoundLibrary를 찾지 못했습니다.", SoundLibraryPath);
            }

            SerializedObject serializedLibrary = new(soundLibrary);
            SerializedProperty sounds = serializedLibrary.FindProperty("sounds");

            for (int i = 0; i < sounds.arraySize; i++)
            {
                if (sounds.GetArrayElementAtIndex(i).objectReferenceValue == soundData)
                {
                    return;
                }
            }

            int newIndex = sounds.arraySize;
            sounds.InsertArrayElementAtIndex(newIndex);
            sounds.GetArrayElementAtIndex(newIndex).objectReferenceValue = soundData;
            serializedLibrary.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(soundLibrary);
        }

        private static void ConfigureCatalog(SceneCatalogSO catalog)
        {
            SerializedObject serializedCatalog = new(catalog);
            SerializedProperty entries = serializedCatalog.FindProperty("entries");

            EnsureCatalogEntry(entries, SceneId.TeamLogo, "TeamLogoScene");
            EnsureCatalogEntry(entries, SceneId.Title, "TitleScene");
            EnsureCatalogEntry(entries, SceneId.Main, "MainScene");
            EnsureCatalogEntry(entries, SceneId.Bootstrap, "BootstrapScene");
            EnsureCatalogEntry(entries, SceneId.TestMainGame, StartupTargetSceneName);

            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        private static void EnsureCatalogEntry(SerializedProperty entries, SceneId id, string sceneName)
        {
            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("id").intValue == (int)id)
                {
                    ConfigureCatalogEntry(entry, id, sceneName);
                    return;
                }
            }

            int newIndex = entries.arraySize;
            entries.InsertArrayElementAtIndex(newIndex);
            ConfigureCatalogEntry(entries.GetArrayElementAtIndex(newIndex), id, sceneName);
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

        private static void ConfigureStepDisplayName(StartupLoadStepSO step, string displayName)
        {
            SerializedObject serializedStep = new(step);
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

        private static void RemoveStepFromLoadPlan(StartupLoadPlanSO plan, StartupLoadStepSO stepToRemove)
        {
            SerializedObject serializedPlan = new(plan);
            SerializedProperty stepList = serializedPlan.FindProperty("steps");

            for (int i = stepList.arraySize - 1; i >= 0; i--)
            {
                if (stepList.GetArrayElementAtIndex(i).objectReferenceValue == stepToRemove)
                {
                    stepList.DeleteArrayElementAtIndex(i);
                }
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
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(StartupTargetScenePath) == null)
            {
                throw new FileNotFoundException(
                    $"시작 Flow 대상 Scene을 찾지 못했습니다: {StartupTargetSceneName}",
                    StartupTargetScenePath);
            }

            string[] startupPaths =
            {
                BootstrapScenePath,
                LogoScenePath,
                TitleScenePath,
                StartupTargetScenePath
            };
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
