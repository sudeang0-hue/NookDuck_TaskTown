using UnityEngine;
using UnityEngine.UI;
using Manager;
using TaskTown;
using UI;

/// <summary>
/// 엔드리스(또는 완주) 이후 메뉴에서 마을 리셋을 다시 고를 수 있게 합니다.
/// </summary>
public class VillageResetMenuButton : MonoBehaviour
{
    [SerializeField] private Button resetButton;
    [SerializeField] private VillageResetConfirmPopup confirmPopup;

    private VillageSystemManager subscribedVillage;
    private bool isVillageStateSubscribed;

    private void Awake()
    {
        if (resetButton != null)
            resetButton.onClick.AddListener(OnClickReset);
    }

    private void OnEnable()
    {
        TrySubscribeVillageState();
        RefreshVisible();
    }

    private void Start()
    {
        // OnEnable 시점에 Instance가 아직 없을 수 있어 Start에서 한 번 더 구독·갱신
        TrySubscribeVillageState();
        RefreshVisible();
    }

    private void OnDisable()
    {
        UnsubscribeVillageState();
    }

    private void OnDestroy()
    {
        UnsubscribeVillageState();

        if (resetButton != null)
            resetButton.onClick.RemoveListener(OnClickReset);
    }

    private void TrySubscribeVillageState()
    {
        if (isVillageStateSubscribed)
            return;

        VillageSystemManager village = VillageSystemManager.Instance;
        if (village == null)
            return;

        // 이벤트 기반: Update 폴링 없이 레벨/엔드리스 변경 시에만 버튼 표시 갱신
        village.OnVillageStateChanged -= OnVillageStateChanged;
        village.OnVillageStateChanged += OnVillageStateChanged;
        subscribedVillage = village;
        isVillageStateSubscribed = true;
    }

    private void UnsubscribeVillageState()
    {
        if (!isVillageStateSubscribed)
            return;

        if (subscribedVillage != null)
            subscribedVillage.OnVillageStateChanged -= OnVillageStateChanged;
        else if (VillageSystemManager.Instance != null)
            VillageSystemManager.Instance.OnVillageStateChanged -= OnVillageStateChanged;

        subscribedVillage = null;
        isVillageStateSubscribed = false;
    }

    private void OnVillageStateChanged()
    {
        RefreshVisible();
    }

    private void RefreshVisible()
    {
        // 엔딩 후에만 노출: 레벨 10이거나 엔드리스 ON
        VillageSystemManager village = VillageSystemManager.Instance;
        bool canReset = village != null
            && (village.IsVillageLevelMaxed || village.IsEndlessMode);
        if (resetButton != null)
            resetButton.gameObject.SetActive(canReset);
    }

    private void OnClickReset()
    {
        if (confirmPopup != null)
        {
            confirmPopup.Open(() => GameResetService.ResetProgressAndGoToTeamLogo());
            return;
        }

        GameResetService.ResetProgressAndGoToTeamLogo();
    }
}
