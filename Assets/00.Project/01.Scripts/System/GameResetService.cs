using Manager;
using TaskTown.KDH;
using TaskTown.SceneFlow;
using UnityEngine;

namespace TaskTown
{
    /// <summary>
    /// 마을 엔딩 후 "처음부터" / 메뉴 "마을 리셋" 공통 처리.
    /// 도감(DexRecordManager)은 유지합니다.
    /// </summary>
    public static class GameResetService
    {
        /// <summary>
        /// 코인·인벤·마을 업그레이드·엔드리스를 초기화하고 TeamLogo로 갑니다.
        /// </summary>
        public static void ResetProgressAndGoToTeamLogo()
        {
            // 1) 코인
            if (CoinManager.Instance != null) CoinManager.Instance.SetCoin(0);

            // 2) 동물 / 도구 인벤
            if (InventoryManager_Animal.Instance != null) InventoryManager_Animal.Instance.ClearAnimalInventory();
            if (InventoryManager_Tool.Instance != null) InventoryManager_Tool.Instance.ClearToolInventory();

            // 3) 요소 업그레이드 (클릭/타이핑/도구효율)
            if (TownUpgradeManager.Instance != null) TownUpgradeManager.Instance.LoadLevels(0, 0, 0);

            // 4) 마을 레벨·사이클·엔드리스·배치
            VillageSystemManager village = VillageSystemManager.Instance;
            if (village != null)
            {
                village.DebugSetEndlessMode(false);
                village.DebugSetTownLevel(1); // 사이클 플래그도 리셋
                village.SetPlacedAnimalIds(System.Array.Empty<string>());
            }

            // 5) 세이브 파일 삭제 (다음에 빈 상태로 시작)
            //    DDOL이라 Start()의 LoadGame은 다시 안 돌므로, 런타임 클리어가 중요합니다.
            if (SaveManager.Instance != null) SaveManager.Instance.DeleteSave();

            // 6) 팀로고 → 타이틀 → 메인 흐름 재시작
            SceneFlowManager flow = SceneFlowManager.EnsureInstance();
            if (flow == null || !flow.LoadScene(SceneId.TeamLogo))
            {
                Debug.LogError("[GameResetService] TeamLogo 씬 로드 실패");
            }
        }
    }
}
