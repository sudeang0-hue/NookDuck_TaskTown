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
            //-----------------26.08.05 KDH-------------------------
            // 0) 자동저장이 옛 인벤으로 세이브를 다시 쓰지 못하게 먼저 차단 + 디스크 삭제
            if (SaveManager.Instance != null)
                SaveManager.Instance.BeginProgressReset();
            //----------------------------------------

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

            //-----------------26.08.05 KDH-------------------------
            // 5) DDOL 인벤/생산 루트를 즉시 제거 (지연 Destroy면 씬 전환 중 잔존할 수 있음)
            DestroyPersistentInventoryRoot();

            // 다음 Main에서 한 번 더 빈 인벤을 강제합니다.
            EndingMeta.ForceEmptyInventoryOnNextMain = true;

            // 6) 엔딩을 본 적 있으면, 이번 리셋 플로우에서만 난이도 선택
            if (EndingMeta.HasClearedEnding)
                EndingMeta.RequestDifficultySelect();
            //----------------------------------------

            // 7) 팀로고 → 타이틀 → (난이도) → 메인 흐름 재시작
            SceneFlowManager flow = SceneFlowManager.EnsureInstance();
            if (flow == null || !flow.LoadScene(SceneId.TeamLogo))
            {
                Debug.LogError("[GameResetService] TeamLogo 씬 로드 실패");
            }
        }

        //-----------------26.08.05 KDH-------------------------
        /// <summary>
        /// InventoryManager(Animal/Tool) + RealProductionTicker가 올라간 DDOL 루트를 제거합니다.
        /// </summary>
        private static void DestroyPersistentInventoryRoot()
        {
            GameObject root = null;

            if (InventoryManager_Animal.Instance != null)
                root = InventoryManager_Animal.Instance.gameObject;
            else if (InventoryManager_Tool.Instance != null)
                root = InventoryManager_Tool.Instance.gameObject;
            else if (RealProductionTicker.Instance != null)
                root = RealProductionTicker.Instance.gameObject;

            if (root == null)
                return;

            // 같은 프레임 씬 전환 전에 Instance가 비도록 즉시 제거합니다.
            Object.DestroyImmediate(root);
        }
        //----------------------------------------
    }
}
