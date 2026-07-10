using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UIButtonSoundPlayer : MonoBehaviour
{
    [SerializeField] private Button targetButton;
    [SerializeField] private string uiSoundId;
    [SerializeField] private SoundManager soundManager;

    private void Awake()
    {
        if (targetButton == null)
        {
            TryGetComponent(out targetButton);
        }
    }

    private void OnEnable()
    {
        if (targetButton != null)
        {
            targetButton.onClick.AddListener(Play);
        }
    }

    private void OnDisable()
    {
        if (targetButton != null)
        {
            targetButton.onClick.RemoveListener(Play);
        }
    }

    public void Play()
    {
        SoundManager manager = soundManager != null ? soundManager : SoundManager.Instance;

        if (manager == null)
        {
            Debug.LogWarning("SoundManager is not available. UI sound cannot be played.");
            return;
        }

        manager.PlayUI(uiSoundId);
    }
}
