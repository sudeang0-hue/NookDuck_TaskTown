using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class BGMPlayerSettingsPrefabBuilder
{
    private const string MenuPath = "Tools/TaskTown/UI/Build BGM Player Settings Panel";
    private const string PreviewMenuPath = "Tools/TaskTown/UI/Preview BGM Player Sound Tab";
    private const string OptionPrefabPath =
        "Assets/00.Project/02.Prefabs/UI/ALL_UI_Connect/canvas/Option_Canvas_tab.prefab";
    private const string ContentPath =
        "SoundOption_root/SoundScroll_Root/Scroll View/Viewport/Content";
    private const string FontAssetPath =
        "Assets/00.Project/06.UI/Fonts/Griun_Fromsol-Rg_BGMPlayer.asset";
    private const string UndoName = "Build BGM Player Settings Panel";

    private static readonly Color PanelColor = new(0.9569f, 0.8824f, 0.7451f, 0.58f);
    private static readonly Color TextColor = new(0.22f, 0.15f, 0.10f, 1f);
    private static readonly Color ButtonNormalColor = new(0.82f, 0.70f, 0.53f, 1f);
    private static readonly Color ButtonHighlightedColor = new(0.91f, 0.82f, 0.67f, 1f);
    private static readonly Color ButtonPressedColor = new(0.67f, 0.53f, 0.38f, 1f);
    private static readonly Color ProgressBackgroundColor = new(0.45f, 0.35f, 0.27f, 0.3f);
    private static readonly Color ProgressFillColor = new(0.52f, 0.31f, 0.17f, 1f);

    [MenuItem(MenuPath)]
    public static void Build()
    {
        PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();

        if (prefabStage == null || prefabStage.assetPath != OptionPrefabPath)
        {
            Debug.LogError($"[BGMPlayerSettingsPrefabBuilder] Prefab Stage에서 {OptionPrefabPath}을 먼저 열어주세요.");
            return;
        }

        Transform content = prefabStage.prefabContentsRoot.transform.Find(ContentPath);

        if (content == null)
        {
            Debug.LogError($"[BGMPlayerSettingsPrefabBuilder] Content를 찾지 못했습니다: {ContentPath}");
            return;
        }

        Transform existingRoot = content.Find("BGMPlayerRoot");

        if (existingRoot != null)
        {
            Selection.activeGameObject = existingRoot.gameObject;
            Debug.LogWarning("[BGMPlayerSettingsPrefabBuilder] BGMPlayerRoot가 이미 존재하여 중복 생성을 중단했습니다.");
            return;
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(UndoName);

        RemoveScrollTestPlaceholders(content);

        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        fontAsset ??= TMP_Settings.defaultFontAsset;

        GameObject rootObject = CreateUIObject("BGMPlayerRoot", content);
        RectTransform rootRect = rootObject.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 1f);
        rootRect.anchorMax = new Vector2(0f, 1f);
        rootRect.anchoredPosition = new Vector2(288f, -267f);
        rootRect.localScale = Vector3.one * 0.8f;
        rootRect.sizeDelta = new Vector2(576f, 230f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);

        Image rootImage = Undo.AddComponent<Image>(rootObject);
        rootImage.color = PanelColor;
        rootImage.raycastTarget = false;

        LayoutElement rootLayout = Undo.AddComponent<LayoutElement>(rootObject);
        rootLayout.preferredWidth = 576f;
        rootLayout.minHeight = 230f;
        rootLayout.preferredHeight = 230f;

        BGMPlaylistPanel panel = Undo.AddComponent<BGMPlaylistPanel>(rootObject);

        TextMeshProUGUI titleText = CreateText(
            "TitleText",
            rootRect,
            "BGM Player",
            fontAsset,
            22f,
            FontStyles.Bold,
            TextAlignmentOptions.MidlineLeft);
        SetTopLeftRect(titleText.rectTransform, new Vector2(20f, -14f), new Vector2(280f, 40f));

        (Button modeButton, TextMeshProUGUI modeButtonText) = CreateButton(
            "PlaybackModeButton",
            rootRect,
            "순차 반복",
            fontAsset,
            new Vector2(150f, 40f));
        SetTopRightRect(modeButton.GetComponent<RectTransform>(), new Vector2(-20f, -14f), new Vector2(150f, 40f));

        TextMeshProUGUI trackNameText = CreateText(
            "TrackNameText",
            rootRect,
            "BGM 준비 중",
            fontAsset,
            20f,
            FontStyles.Normal,
            TextAlignmentOptions.MidlineLeft);
        SetTopStretchRect(trackNameText.rectTransform, 20f, 112f, -62f, 36f);
        trackNameText.textWrappingMode = TextWrappingModes.NoWrap;
        trackNameText.overflowMode = TextOverflowModes.Ellipsis;

        TextMeshProUGUI trackIndexText = CreateText(
            "TrackIndexText",
            rootRect,
            "- / -",
            fontAsset,
            18f,
            FontStyles.Normal,
            TextAlignmentOptions.MidlineRight);
        SetTopRightRect(trackIndexText.rectTransform, new Vector2(-20f, -62f), new Vector2(82f, 36f));

        (Button previousButton, _) = CreateButton(
            "PreviousButton",
            rootRect,
            "이전",
            fontAsset,
            new Vector2(108f, 54f));
        SetTopCenterRect(previousButton.GetComponent<RectTransform>(), new Vector2(-126f, -108f), new Vector2(108f, 54f));

        (Button playPauseButton, TextMeshProUGUI playPauseButtonText) = CreateButton(
            "PlayPauseButton",
            rootRect,
            "재생",
            fontAsset,
            new Vector2(144f, 54f));
        SetTopCenterRect(playPauseButton.GetComponent<RectTransform>(), new Vector2(0f, -108f), new Vector2(144f, 54f));

        (Button nextButton, _) = CreateButton(
            "NextButton",
            rootRect,
            "다음",
            fontAsset,
            new Vector2(108f, 54f));
        SetTopCenterRect(nextButton.GetComponent<RectTransform>(), new Vector2(126f, -108f), new Vector2(108f, 54f));

        TextMeshProUGUI currentTimeText = CreateText(
            "CurrentTimeText",
            rootRect,
            "00:00",
            fontAsset,
            16f,
            FontStyles.Normal,
            TextAlignmentOptions.Center);
        SetTopLeftRect(currentTimeText.rectTransform, new Vector2(20f, -181f), new Vector2(62f, 26f));

        TextMeshProUGUI durationText = CreateText(
            "DurationText",
            rootRect,
            "00:00",
            fontAsset,
            16f,
            FontStyles.Normal,
            TextAlignmentOptions.Center);
        SetTopRightRect(durationText.rectTransform, new Vector2(-20f, -181f), new Vector2(62f, 26f));

        GameObject progressBackgroundObject = CreateUIObject("ProgressBackground", rootRect);
        RectTransform progressBackgroundRect = progressBackgroundObject.GetComponent<RectTransform>();
        SetTopStretchRect(progressBackgroundRect, 92f, 92f, -188f, 12f);
        Image progressBackground = Undo.AddComponent<Image>(progressBackgroundObject);
        progressBackground.color = ProgressBackgroundColor;
        progressBackground.raycastTarget = false;

        GameObject progressFillObject = CreateUIObject("ProgressFill", progressBackgroundRect);
        RectTransform progressFillRect = progressFillObject.GetComponent<RectTransform>();
        SetStretchRect(progressFillRect);
        Image progressFill = Undo.AddComponent<Image>(progressFillObject);
        progressFill.color = ProgressFillColor;
        progressFill.raycastTarget = false;
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillOrigin = 0;
        progressFill.fillAmount = 0f;

        int bgmRowIndex = content.Cast<Transform>().ToList().FindIndex(child => child.name == "BGMRow");
        rootRect.SetSiblingIndex(Mathf.Max(0, bgmRowIndex + 1));

        SerializedObject serializedPanel = new(panel);
        SetReference(serializedPanel, "previousButton", previousButton);
        SetReference(serializedPanel, "playPauseButton", playPauseButton);
        SetReference(serializedPanel, "nextButton", nextButton);
        SetReference(serializedPanel, "playbackModeButton", modeButton);
        SetReference(serializedPanel, "trackNameText", trackNameText);
        SetReference(serializedPanel, "trackIndexText", trackIndexText);
        SetReference(serializedPanel, "currentTimeText", currentTimeText);
        SetReference(serializedPanel, "durationText", durationText);
        SetReference(serializedPanel, "playPauseButtonText", playPauseButtonText);
        SetReference(serializedPanel, "playbackModeButtonText", modeButtonText);
        SetReference(serializedPanel, "progressFillImage", progressFill);
        serializedPanel.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(prefabStage.scene);
        Selection.activeGameObject = rootObject;
        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log("[BGMPlayerSettingsPrefabBuilder] BGM Player 설정 UI를 구성했습니다. Prefab 저장 전 Undo할 수 있습니다.");
    }

    [MenuItem(PreviewMenuPath)]
    public static void PreviewSoundTab()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[BGMPlayerSettingsPrefabBuilder] Sound 탭 미리보기는 Play Mode에서 실행해주세요.");
            return;
        }

        UI.TabUIManager_Option optionTabManager = Object
            .FindObjectsByType<UI.TabUIManager_Option>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault();

        if (optionTabManager == null)
        {
            Debug.LogError("[BGMPlayerSettingsPrefabBuilder] TabUIManager_Option을 찾지 못했습니다.");
            return;
        }

        optionTabManager.OpenSoundOptionTab();
        Debug.Log("[BGMPlayerSettingsPrefabBuilder] 실제 탭 전환 흐름으로 Sound 옵션을 열었습니다.");
    }

    private static void RemoveScrollTestPlaceholders(Transform content)
    {
        string[] placeholderNames = { "Image", "Image (1)", "Image (2)", "Image (3)" };

        foreach (string placeholderName in placeholderNames)
        {
            Transform placeholder = content.Find(placeholderName);

            if (placeholder == null || placeholder.childCount != 0 ||
                placeholder.GetComponent<Image>() == null ||
                placeholder.GetComponent<LayoutElement>() == null)
            {
                continue;
            }

            Undo.DestroyObjectImmediate(placeholder.gameObject);
        }
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject gameObject = new(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(gameObject, UndoName);
        Undo.SetTransformParent(gameObject.transform, parent, UndoName);

        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.localPosition = Vector3.zero;
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.localScale = Vector3.one;
        gameObject.layer = LayerMask.NameToLayer("UI");
        return gameObject;
    }

    private static TextMeshProUGUI CreateText(
        string name,
        Transform parent,
        string value,
        TMP_FontAsset fontAsset,
        float fontSize,
        FontStyles fontStyle,
        TextAlignmentOptions alignment)
    {
        GameObject textObject = CreateUIObject(name, parent);
        TextMeshProUGUI text = Undo.AddComponent<TextMeshProUGUI>(textObject);
        text.text = value;
        text.font = fontAsset;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = TextColor;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    private static (Button button, TextMeshProUGUI label) CreateButton(
        string name,
        Transform parent,
        string labelText,
        TMP_FontAsset fontAsset,
        Vector2 size)
    {
        GameObject buttonObject = CreateUIObject(name, parent);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.sizeDelta = size;

        Image buttonImage = Undo.AddComponent<Image>(buttonObject);
        buttonImage.color = Color.white;

        Button button = Undo.AddComponent<Button>(buttonObject);
        button.targetGraphic = buttonImage;
        button.transition = Selectable.Transition.ColorTint;
        button.navigation = new Navigation { mode = Navigation.Mode.None };

        ColorBlock colors = button.colors;
        colors.normalColor = ButtonNormalColor;
        colors.highlightedColor = ButtonHighlightedColor;
        colors.selectedColor = ButtonHighlightedColor;
        colors.pressedColor = ButtonPressedColor;
        colors.disabledColor = new Color(ButtonNormalColor.r, ButtonNormalColor.g, ButtonNormalColor.b, 0.45f);
        colors.colorMultiplier = 1f;
        button.colors = colors;

        Undo.AddComponent<UIButtonSoundPlayer>(buttonObject);

        TextMeshProUGUI label = CreateText(
            "Label",
            buttonRect,
            labelText,
            fontAsset,
            18f,
            FontStyles.Normal,
            TextAlignmentOptions.Center);
        SetStretchRect(label.rectTransform, 6f, 6f, 4f, 4f);
        return (button, label);
    }

    private static void SetReference(SerializedObject serializedObject, string propertyName, Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static void SetTopLeftRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void SetTopRightRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = Vector2.one;
        rect.anchorMax = Vector2.one;
        rect.pivot = Vector2.one;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void SetTopCenterRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void SetTopStretchRect(
        RectTransform rect,
        float left,
        float right,
        float top,
        float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(left, top - height);
        rect.offsetMax = new Vector2(-right, top);
    }

    private static void SetStretchRect(
        RectTransform rect,
        float left = 0f,
        float right = 0f,
        float bottom = 0f,
        float top = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }
}
