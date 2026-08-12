using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// VillageHouse 호버 시 PC_Renderer의 Stencil Outline Feature만 켭니다.
/// 메시 슬롯 Outline 없이 RenderObjects Override로 실루엣만 그립니다.
/// </summary>
public class HoverOutlineTarget : MonoBehaviour
{
    [Header("URP Outline Features")]
    [SerializeField] private UniversalRendererData rendererData;
    [SerializeField] private string maskFeatureName = "VillageHouseOutlineMask";
    [SerializeField] private string outlineFeatureName = "VillageHouseOutline";

    [Header("Outline Material (RenderObjects Override)")]
    [SerializeField] private Material outlineMaterial;
    [SerializeField] private string thicknessProperty = "_Outline_Thickness";
    [SerializeField, Min(0f)] private float hoveredThickness = 0.06f;

    private ScriptableRendererFeature maskFeature;
    private ScriptableRendererFeature outlineFeature;
    private int thicknessPropertyId;
    private bool isInitialized;

    private void Awake()
    {
        Initialize();

        if (isInitialized)
            SetOutline(false);
    }

    public void SetOutline(bool isActive)
    {
        if (!isInitialized)
            Initialize();

        if (!isInitialized)
            return;

        if (maskFeature != null)
            maskFeature.SetActive(isActive);

        if (outlineFeature != null)
            outlineFeature.SetActive(isActive);

        if (outlineMaterial != null)
        {
            outlineMaterial.SetFloat(
                thicknessPropertyId,
                isActive ? hoveredThickness : 0f);
        }
    }

    private void Initialize()
    {
        if (isInitialized)
            return;

        if (rendererData == null)
        {
            Debug.LogWarning(
                $"[{nameof(HoverOutlineTarget)}] UniversalRendererData가 없습니다.",
                this);
            return;
        }

        for (int i = 0; i < rendererData.rendererFeatures.Count; i++)
        {
            ScriptableRendererFeature feature = rendererData.rendererFeatures[i];
            if (feature == null)
                continue;

            if (feature.name == maskFeatureName)
                maskFeature = feature;
            else if (feature.name == outlineFeatureName)
                outlineFeature = feature;
        }

        if (maskFeature == null && outlineFeature == null)
        {
            Debug.LogWarning(
                $"[{nameof(HoverOutlineTarget)}] Outline Feature를 찾지 못했습니다. " +
                $"names=({maskFeatureName}, {outlineFeatureName})",
                this);
            return;
        }

        thicknessPropertyId = Shader.PropertyToID(thicknessProperty);
        isInitialized = true;
    }

    private void OnDisable()
    {
        if (isInitialized)
            SetOutline(false);
    }
}
