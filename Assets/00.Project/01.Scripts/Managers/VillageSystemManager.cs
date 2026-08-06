using System;
using System.Collections.Generic;
using TaskTown.Gacha;
using UnityEngine;

/// <summary>
/// 마을 진행 상태(마을 레벨, 사이클 업그레이드 완료, 엔드리스, 배치 동물)의 런타임 진실 소스.
/// UI는 표시·입력만 담당하고, 이 매니저가 상태를 보관·변경합니다.
///
/// ■ 책임 분리
/// - 클릭/타이핑/도구효율 "영구 레벨·비용·배율" → TownUpgradeManager
/// - 마을 레벨 / 이번 사이클 게이트 / 엔드리스 / 배치 확정본 → VillageSystemManager
/// - 패널 표시·버튼·팝업 → VillageUpgradeUI_Manager 등 UI
///
/// ■ 외부 호환
/// - SaveManager / TownUpgradeManager 등은 기존처럼 VillageUpgradeUI_Manager
///   (ITownLevelProvider 파사드)를 참조해도 됩니다. 파사드가 이 매니저로 위임합니다.
/// - 2026.08.02 - KAY: SaveManager가 Capture/ApplyProgressFromSave로
///   마을 레벨·사이클·엔드리스를 JSON에 저장·복원합니다. 배치 동물은 보류.
/// </summary>
namespace Manager
{
    public class VillageSystemManager : MonoBehaviour, ITownLevelProvider, IEndlessModeProvider
    {
        public static VillageSystemManager Instance { get; private set; }

        private const int RequiredUpgradeTotal = 3;
        private const int NormalModeMaxTownLevel = 10; // 레벨40 확장(#20) 실험 철회, 레벨10으로 되돌림(사용자 확인)

        [Header("마을 레벨")]
        [SerializeField] private int townLevel = 1;

        [Header("사이클 게이트 (해당 마을 레벨 구간에서 요소 1회 완료 여부)")]
        [SerializeField] private bool cycleClickDone;
        [SerializeField] private bool cycleTypingDone;
        [SerializeField] private bool cycleToolDone;

        [Header("엔드리스 모드 (#19)")]
        [Tooltip("레벨10 완주 후 '엔드리스로 계속' 선택 시 켜집니다.")]
        [SerializeField] private bool isEndlessMode;

        [Header("마을 레벨업 비용")]
        [SerializeField] private TownUpgradeCostConfig townUpgradeCostConfig = new TownUpgradeCostConfig();

        //------------------26.08.06 KAY 이관 (마을 재건 완료 비용)---------------------------------
        [Header("마을 재건 완료 비용")]
        [Tooltip("최대 레벨 도달 후 마을 재건/엔드 선택 팝업을 열 때 소모하는 코인")]
        [SerializeField] private long villageCompletionCost;
        //-----------------------------------------------------------------------------

        [Header("배치 동물 확정본")]
        [SerializeField] private List<string> placedAnimalIds = new List<string>();

        [Header("배치 용량 규칙")]
        [SerializeField] private int maxPlacementCapacity = 20;
        [SerializeField] private int unlockedSlotsAtTownLevel1 = 5;
        [SerializeField]
        private int[] unlockedSlotsByTownLevel =
        {
            5, 6, 7, 9, 10, 12, 13, 14, 16, 20
        };

        [Header("마을 레벨업 장식 보상")]
        [Tooltip("인덱스 = (마을 레벨 - 1). 해당 레벨 도달 시 추가되는 장식명. 비어 있으면 UI에서 숨김")]
        [SerializeField] private string[] villageDecoBuilding;

        /// <summary>마을 상태가 바뀌면 UI/세이브 구독자가 갱신할 때 사용합니다.</summary>
        public event Action OnVillageStateChanged;

        // --------- KAY. 08.05 추가 ------------------------
        /// <summary> 엔딩을 보기 전&&엔드리스 모드가 아닐 때, 마을 레벨이 최대치에 도달했을때 사용합니다.</summary>
        public event Action OnVillageLevelMax;

        public int TownLevel => Mathf.Max(1, townLevel);

        /// <summary>ITownLevelProvider — 도구 상한/뽑기/세이브가 참조.</summary>
        public int CurrentTownLevel => TownLevel;

        public bool CycleClickDone => cycleClickDone;
        public bool CycleTypingDone => cycleTypingDone;
        public bool CycleToolDone => cycleToolDone;

        public int CycleCompletedCount =>
            (cycleClickDone ? 1 : 0) + (cycleTypingDone ? 1 : 0) + (cycleToolDone ? 1 : 0);

        public bool IsReadyForVillageLevelUp => CycleCompletedCount >= RequiredUpgradeTotal;

        /// <summary>IEndlessModeProvider</summary>
        public bool IsEndlessMode => isEndlessMode;

        /// <summary>
        /// 마을 레벨이 상한(10)에 도달했는지.
        /// 엔드리스여도 마을 레벨 자체는 10에서 고정됩니다.
        /// </summary>
        public bool IsVillageLevelMaxed => TownLevel >= NormalModeMaxTownLevel;

        public IReadOnlyList<string> PlacedAnimalIds => placedAnimalIds;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // -------------------------------------------------------------------------
        // 조회
        // -------------------------------------------------------------------------

        public bool IsTrackDoneInCycle(VillageElementTrack track)
        {
            switch (track)
            {
                case VillageElementTrack.Click:
                    return cycleClickDone;
                case VillageElementTrack.Typing:
                    return cycleTypingDone;
                case VillageElementTrack.ToolEfficiency:
                    return cycleToolDone;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 이번 사이클에서 해당 트랙을 구매할 수 있는지(엔드리스면 사이클 제한 없음).
        /// </summary>
        public bool CanPurchaseTrackInCycle(VillageElementTrack track)
        {
            if (isEndlessMode)
                return true;

            return !IsTrackDoneInCycle(track);
        }

        /// <summary>해금된 배치 슬롯 수 (마을 레벨 기반).</summary>
        public int GetUnlockedPlacementSlotCount()
        {
            int level = TownLevel;
            if (unlockedSlotsByTownLevel != null && unlockedSlotsByTownLevel.Length > 0)
            {
                int index = Mathf.Clamp(level - 1, 0, unlockedSlotsByTownLevel.Length - 1);
                return Mathf.Clamp(unlockedSlotsByTownLevel[index], 0, maxPlacementCapacity);
            }

            return Mathf.Clamp(unlockedSlotsAtTownLevel1 + (level - 1), 0, maxPlacementCapacity);
        }

        /// <summary>
        /// 지정 마을 레벨에 추가되는 장식명.
        /// 비어 있거나 인덱스가 없으면 null을 반환합니다. (UI는 해당 RewardText를 숨김)
        /// </summary>
        public string GetVillageDecoBuildingName(int townLevel)
        {
            if (villageDecoBuilding == null || villageDecoBuilding.Length == 0)
                return null;

            int index = Mathf.Max(1, townLevel) - 1;
            if (index < 0 || index >= villageDecoBuilding.Length)
                return null;

            string name = villageDecoBuilding[index];
            return string.IsNullOrWhiteSpace(name) ? null : name;
        }

        /// <summary>현재 마을 레벨 기준 레벨업 필요 코인.</summary>
        public long GetVillageLevelUpCost()
        {
            if (townUpgradeCostConfig == null)
                return 0;

            return townUpgradeCostConfig.GetCostForTownLevel(TownLevel);
        }

        //------------------26.08.06 KAY 이관 (마을 재건 완료 비용)---------------------------------
        /// <summary>최대 레벨 도달 후 재건/엔드 선택 팝업 오픈에 필요한 코인.</summary>
        public long GetVillageCompletionCost()
        {
            return villageCompletionCost < 0L ? 0L : villageCompletionCost;
        }
        //-----------------------------------------------------------------------------

        // -------------------------------------------------------------------------
        // 변경
        // -------------------------------------------------------------------------

        /// <summary>
        /// 사이클에서 해당 요소 완료 처리만 기록.
        /// 실제 코인 차감·영구 레벨업은 TownUpgradeManager.TryUpgrade* 성공 후에만 호출하세요.
        /// 엔드리스 모드에서는 사이클 플래그를 세우지 않습니다.
        /// </summary>
        public bool TryMarkCycleTrackDone(VillageElementTrack track)
        {
            if (isEndlessMode)
                return true;

            switch (track)
            {
                case VillageElementTrack.Click:
                    if (cycleClickDone) return false;
                    cycleClickDone = true;
                    break;
                case VillageElementTrack.Typing:
                    if (cycleTypingDone) return false;
                    cycleTypingDone = true;
                    break;
                case VillageElementTrack.ToolEfficiency:
                    if (cycleToolDone) return false;
                    cycleToolDone = true;
                    break;
                default:
                    return false;
            }

            RaiseStateChanged();
            return true;
        }

        /// <summary>
        /// 필수 3종 완료 + 비용 지불 가능 시 마을 레벨업을 수행합니다.
        /// 엔드리스 여부와 무관하게 레벨10(완주)에서 멈춥니다.
        /// </summary>
        public bool TryVillageLevelUp()
        {
            if (IsVillageLevelMaxed)
                return false;

            if (!IsReadyForVillageLevelUp)
                return false;

            long cost = GetVillageLevelUpCost();
            if (CoinManager.Instance == null || !CoinManager.Instance.TrySpend(cost))
                return false;

            townLevel = TownLevel + 1;
            ResetCycleFlagsOnly();
            RaiseStateChanged();

            // --------- KAY. 08.05 추가 ------------------------
            // 최대 도달 + 엔드리스 아님 → 1회성 통지
            if (IsVillageLevelMaxed && !isEndlessMode)
                OnVillageLevelMax?.Invoke();

            return true;
        }

        /// <summary>세이브 townLevel 복원. 사이클 완료 플래그는 건드리지 않습니다.</summary>
        public void SetTownLevelFromSave(int level)
        {
            SetTownLevel(level, resetCycle: false);
        }

        /// <summary>마을 레벨 강제 설정. resetCycle이면 사이클 플래그도 초기화.</summary>
        public void SetTownLevel(int level, bool resetCycle = false)
        {
            townLevel = Mathf.Max(1, level);
            if (resetCycle)
                ResetCycleFlagsOnly();
            RaiseStateChanged();
        }

        /// <summary>배치 확정본 교체 (AnimalSet Confirm 시).</summary>
        public void SetPlacedAnimalIds(IReadOnlyList<string> animalIds)
        {
            ReplacePlacedAnimalIds(animalIds);

            RaiseStateChanged();
        }

        // -----------------------------------------------------------------------------
        // [ 2026.08.06 - Choi - 마을 동물 배치 저장 연동 ]
        // 기능: 저장값과 UI 확정값을 동일한 규칙으로 보정해 시스템 확정본에 반영합니다.
        // -----------------------------------------------------------------------------
        private void ReplacePlacedAnimalIds(IReadOnlyList<string> animalIds)
        {
            placedAnimalIds.Clear();
            if (animalIds == null)
                return;

            int count = Mathf.Min(animalIds.Count, Mathf.Max(0, maxPlacementCapacity));
            for (int i = 0; i < count; i++)
                placedAnimalIds.Add(animalIds[i] ?? string.Empty);
        }

        /// <summary>레벨10 완주 후 "엔드리스로 계속" 선택 시 호출.</summary>
        public void EnableEndlessMode()
        {
            if (isEndlessMode)
                return;

            isEndlessMode = true;
            RaiseStateChanged();
        }

        /// <summary>디버그/테스트 전용. 엔드리스 모드 강제 토글.</summary>
        public void DebugSetEndlessMode(bool value)
        {
            if (isEndlessMode == value)
                return;

            isEndlessMode = value;
            RaiseStateChanged();
        }

        /// <summary>디버그용. 마을 레벨을 지정값으로 두고 사이클을 리셋합니다.</summary>
        public void DebugSetTownLevel(int level)
        {
            SetTownLevel(level, resetCycle: true);
        }

        /// <summary>사이클 플래그만 리셋(마을 레벨은 유지).</summary>
        public void ResetCycleFlags()
        {
            ResetCycleFlagsOnly();
            RaiseStateChanged();
        }

        private void ResetCycleFlagsOnly()
        {
            cycleClickDone = false;
            cycleTypingDone = false;
            cycleToolDone = false;
        }

        private void RaiseStateChanged()
        {
            OnVillageStateChanged?.Invoke();
        }

        // -------------------------------------------------------------------------
        // 세이브 스냅샷 (SaveManager 연동)
        // -------------------------------------------------------------------------

        [Serializable]
        public class VillageSaveSnapshot
        {
            public int townLevel = 1;
            public bool cycleClickDone;
            public bool cycleTypingDone;
            public bool cycleToolDone;
            public bool isEndlessMode;
            // null이면 Apply 시 기존 배치 목록을 유지합니다(진행 상태 전용 호출 호환).
            public List<string> placedAnimalIds;
        }

        public VillageSaveSnapshot CaptureSaveSnapshot()
        {
            return new VillageSaveSnapshot
            {
                townLevel = TownLevel,
                cycleClickDone = cycleClickDone,
                cycleTypingDone = cycleTypingDone,
                cycleToolDone = cycleToolDone,
                isEndlessMode = isEndlessMode,
                placedAnimalIds = new List<string>(placedAnimalIds)
            };
        }

        /// <summary>
        /// 세이브에서 복원한 진행 상태(레벨/사이클/엔드리스)를 적용합니다.
        /// placedAnimalIds가 null이면 배치 확정본은 유지합니다.
        /// </summary>
        public void ApplySaveSnapshot(VillageSaveSnapshot snapshot)
        {
            if (snapshot == null)
                return;

            townLevel = Mathf.Max(1, snapshot.townLevel);
            cycleClickDone = snapshot.cycleClickDone;
            cycleTypingDone = snapshot.cycleTypingDone;
            cycleToolDone = snapshot.cycleToolDone;
            isEndlessMode = snapshot.isEndlessMode;

            // null은 기존 진행 상태 전용 호출과의 하위 호환을 위해 배치를 유지합니다.
            if (snapshot.placedAnimalIds != null)
                ReplacePlacedAnimalIds(snapshot.placedAnimalIds);

            RaiseStateChanged();
        }

        // 2026.08.02 - KAY - SaveManager가 GameSaveData 필드만으로 진행 상태를 복원할 때 사용
        /// <summary>
        /// 마을 레벨·사이클 게이트·엔드리스만 복원합니다. 배치 동물은 변경하지 않습니다.
        /// </summary>
        public void ApplyProgressFromSave(
            int savedTownLevel,
            bool savedCycleClickDone,
            bool savedCycleTypingDone,
            bool savedCycleToolDone,
            bool savedIsEndlessMode)
        {
            ApplySaveSnapshot(new VillageSaveSnapshot
            {
                townLevel = savedTownLevel,
                cycleClickDone = savedCycleClickDone,
                cycleTypingDone = savedCycleTypingDone,
                cycleToolDone = savedCycleToolDone,
                isEndlessMode = savedIsEndlessMode,
                placedAnimalIds = null
            });
        }
    }

    /// <summary>사이클 게이트용 요소 트랙 구분.</summary>
    public enum VillageElementTrack
    {
        Click = 0,
        Typing = 1,
        ToolEfficiency = 2
    }
}
