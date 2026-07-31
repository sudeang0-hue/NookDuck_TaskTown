using UnityEngine;

public class HoverOutlineTarget : MonoBehaviour
{
    [Header("Renderer")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField, Min(0)] private int outlineMaterialIndex = 1;

    [Header("Outline")]
    [SerializeField] private string outlineSizeProperty = "_OutlineSize";
    [SerializeField] private float defaultOutlineSize = 1f;
    [SerializeField] private float hoveredOutlineSize = 1.03f;

    private MaterialPropertyBlock propertyBlock;
    private int outlineSizePropertyId;
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

        targetRenderer.GetPropertyBlock(
            propertyBlock,
            outlineMaterialIndex);

        propertyBlock.SetFloat(
            outlineSizePropertyId,
            isActive ? hoveredOutlineSize : defaultOutlineSize);

        targetRenderer.SetPropertyBlock(
            propertyBlock,
            outlineMaterialIndex);
    }

    private void Initialize()
    {
        if (isInitialized)
            return;

        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();

        if (targetRenderer == null)
        {
            Debug.LogWarning(
                $"[{nameof(HoverOutlineTarget)}] Renderer가 없습니다.",
                this);

            return;
        }

        Material[] sharedMaterials = targetRenderer.sharedMaterials;

        if (outlineMaterialIndex < 0 ||
            outlineMaterialIndex >= sharedMaterials.Length)
        {
            Debug.LogWarning(
                $"[{nameof(HoverOutlineTarget)}] " +
                $"Outline Material Index가 올바르지 않습니다. " +
                $"현재 Material 수: {sharedMaterials.Length}",
                this);

            return;
        }

        if (string.IsNullOrWhiteSpace(outlineSizeProperty))
        {
            Debug.LogWarning(
                $"[{nameof(HoverOutlineTarget)}] " +
                "Outline Size Property가 비어 있습니다.",
                this);

            return;
        }

        propertyBlock = new MaterialPropertyBlock();
        outlineSizePropertyId = Shader.PropertyToID(outlineSizeProperty);
        isInitialized = true;
    }

    private void OnDisable()
    {
        if (isInitialized)
            SetOutline(false);
    }
}