using TaskTown.Tutorial;
using UI;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 설정 또는 임시 UI의 '튜토리얼 다시 보기' 버튼을 제어합니다.
/// TutorialManager는 Overlay와 함께 언로드될 수 있으므로 MainScene의 TutorialOverlayLoader만 사용합니다.
/// </summary>
public sealed class TutorialReplayButtonController : MonoBehaviour
{
    [Header("Button")]
    [SerializeField] private Button replayButton;

    [Header("Confirmation")]
    [SerializeField] private LevelUpPopup confirmationPopup;
    [SerializeField, TextArea(2, 4)]
    private string confirmationMessage = "튜토리얼을 처음부터 다시 보시겠습니까?";
    [SerializeField, TextArea(2, 4)]
    private string rewardNotice =
        "이미 받은 튜토리얼 보상은 다시 지급되지 않습니다.";

    [Header("Runtime")]
    [SerializeField] private TutorialOverlayLoader overlayLoader;

    private bool isRequestPending;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (replayButton != null)
            replayButton.onClick.AddListener(OnReplayButtonClicked);
    }

    private void OnDisable()
    {
        if (replayButton != null)
            replayButton.onClick.RemoveListener(OnReplayButtonClicked);

        isRequestPending = false;
    }

    public void OnReplayButtonClicked()
    {
        if (isRequestPending)
            return;

        ResolveReferences();
        if (overlayLoader == null)
        {
            Debug.LogWarning(
                "[TutorialReplayButtonController] TutorialOverlayLoader를 찾을 수 없습니다.",
                this);
            return;
        }

        if (overlayLoader.IsBusy)
            return;

        if (confirmationPopup == null)
        {
            Debug.LogWarning(
                "[TutorialReplayButtonController] 확인 팝업이 연결되지 않았습니다.",
                this);
            return;
        }

        confirmationPopup.OpenLevelUpPopup(
            confirmationMessage,
            rewardNotice,
            ConfirmReplay,
            CancelReplay);
    }

    private void ConfirmReplay()
    {
        if (isRequestPending)
            return;

        isRequestPending = true;
        if (replayButton != null)
            replayButton.interactable = false;

        bool started = overlayLoader != null &&
                       overlayLoader.TryStartTutorialReplay();

        isRequestPending = false;
        if (replayButton != null)
            replayButton.interactable = true;

        if (!started)
        {
            string error = overlayLoader != null
                ? overlayLoader.LastError
                : "TutorialOverlayLoader를 찾을 수 없습니다.";
            Debug.LogWarning(
                $"[TutorialReplayButtonController] 튜토리얼 다시 보기를 시작하지 못했습니다. {error}",
                this);
        }
    }

    private void CancelReplay()
    {
        isRequestPending = false;
    }

    private void ResolveReferences()
    {
        if (replayButton == null)
            TryGetComponent(out replayButton);

        if (overlayLoader == null)
        {
            overlayLoader = TutorialOverlayLoader.Instance;
            if (overlayLoader == null)
                overlayLoader = FindFirstObjectByType<TutorialOverlayLoader>();
        }

        if (confirmationPopup == null)
        {
            confirmationPopup = FindFirstObjectByType<LevelUpPopup>(
                FindObjectsInactive.Include);
        }
    }
}
