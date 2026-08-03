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
    [SerializeField] private LevelUpPopup confirmPopup; // 확인용으로 재사용 가능

    private void Awake()
    {
        if (resetButton != null)
            resetButton.onClick.AddListener(OnClickReset);
    }

    private void OnDestroy()
    {
        if (resetButton != null)
            resetButton.onClick.RemoveListener(OnClickReset);
    }

    private void OnEnable()
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
        // LevelUpPopup 재사용 시 coin 문구는 안내 텍스트로 대체
        if (confirmPopup != null)
        {
            confirmPopup.OpenLevelUpPopup(
                "마을을 초기화하고 처음부터 시작할까요?\n(도감 기록은 유지됩니다)",
                "코인·인벤·업그레이드가 초기화됩니다",
                () => GameResetService.ResetProgressAndGoToTeamLogo());
            return;
        }
        GameResetService.ResetProgressAndGoToTeamLogo();
    }
}
