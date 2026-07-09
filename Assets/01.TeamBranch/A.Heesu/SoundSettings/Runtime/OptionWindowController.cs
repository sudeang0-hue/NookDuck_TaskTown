using UnityEngine;

public enum OptionWindowSourceMode
{
    SceneObject,
    Prefab
}

public class OptionWindowController : MonoBehaviour
{
    [Header("Window Source")]
    [SerializeField] private OptionWindowSourceMode sourceMode = OptionWindowSourceMode.SceneObject;
    [SerializeField] private GameObject optionWindowRoot;
    [SerializeField] private GameObject optionWindowPrefab;
    [SerializeField] private Transform optionWindowParent;

    [Header("Initial State")]
    [SerializeField] private bool hideOnAwake = true;

    private GameObject cachedWindow;

    public bool IsOpen => cachedWindow != null && cachedWindow.activeSelf;

    private void Awake()
    {
        if (sourceMode == OptionWindowSourceMode.SceneObject)
        {
            cachedWindow = optionWindowRoot;
        }

        if (hideOnAwake && cachedWindow != null)
        {
            cachedWindow.SetActive(false);
        }
    }

    public void Open()
    {
        GameObject window = GetOrCreateWindow();

        if (window == null)
        {
            return;
        }

        window.SetActive(true);
    }

    public void Close()
    {
        GameObject window = GetOrCreateWindow();

        if (window == null)
        {
            return;
        }

        window.SetActive(false);
    }

    public void Toggle()
    {
        GameObject window = GetOrCreateWindow();

        if (window == null)
        {
            return;
        }

        window.SetActive(!window.activeSelf);
    }

    private GameObject GetOrCreateWindow()
    {
        if (sourceMode == OptionWindowSourceMode.SceneObject)
        {
            if (cachedWindow == null)
            {
                cachedWindow = optionWindowRoot;
            }

            if (cachedWindow == null)
            {
                Debug.LogWarning("Option window root is not assigned.");
            }

            return cachedWindow;
        }

        if (cachedWindow != null)
        {
            return cachedWindow;
        }

        if (optionWindowPrefab == null)
        {
            Debug.LogWarning("Option window prefab is not assigned.");
            return null;
        }

        Transform parent = optionWindowParent != null ? optionWindowParent : transform;
        cachedWindow = Instantiate(optionWindowPrefab, parent);
        cachedWindow.SetActive(false);

        return cachedWindow;
    }
}
