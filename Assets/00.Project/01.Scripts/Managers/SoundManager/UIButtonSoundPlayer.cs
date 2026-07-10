/*
 * 역할:
 * - Unity UI Button 클릭에 UI 사운드를 연결하는 브릿지 컴포넌트입니다.
 *
 * 주요 기능:
 * - Button.onClick에 Play()를 등록/해제합니다.
 * - 클릭 시 SoundManager.PlayUI(uiSoundId)를 호출합니다.
 */
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
