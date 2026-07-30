using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 마을 관리 탭에서 쓰이는 진행 상태를(마을 레벨, 사이클 업그레이드, 배치 동물 등)
/// 한곳에서 보관·저장·조회하기 위한 싱글톤 후보입니다.
/// 다른 스크립트와 연결은 되어있지 않습니다.
/// 
/// ■ 왜 두면 협업에 유리한가
/// - 현재: 마을 레벨·사이클은 VillageUpgradeUI_Manager(UI),
///         요소 영구 레벨은 TownUpgradeManager,
///         배치 동물은 VillageAnimalSetUI_Manager(UI),
///         디스크 IO는 SaveManager가 각각 pull.
/// - UI / 세이브 / 가챠·도구상한(ITownLevelProvider)이 같은 숫자를 서로 다른 곳에서 읽어
///   "누가 진실 소스인지" 충돌이 나기 쉽습니다.
/// - VillageSystemManager를 Runtime 데이터 허브로 두면,
///   UI는 표시·입력만, SaveManager는 이 매니저만 직렬화하면 되어 담당 경계가 맑아집니다.
///
/// ■ 이 파일의 범위
/// - 실제 이관 시 권장 순서:
///   1) 이 클래스에 필드·API 확정
///   2) VillageUpgradeUI_Manager / VillageAnimalSetUI_Manager가 여기로 위임
///   3) SaveManager가 Capture/Apply 호출
///   4) ITownLevelProvider 구현을 이 클래스로 이전(또는 래핑)
///
/// ■ TownUpgradeManager와의 관계 (제안)
/// - 클릭/타이핑/도구효율 "영구 레벨·비용·배율 적용"은 계속 TownUpgradeManager 담당 권장
///   (EarnProcessor 연동·밸런스 책임이 이미 있음).
/// - 이 매니저는 "마을 레벨 / 이번 사이클 게이트 / 배치 확정본"만 소유하고,
///   요소 구매 성공 여부는 TownUpgradeManager.TryUpgrade* 결과를 받아 사이클 플래그만 갱신.
/// </summary>
/// 

namespace Manager
{

    public class VillageSystemManager : MonoBehaviour
    {
        public static VillageSystemManager Instance { get; private set; }

        // -------------------------------------------------------------------------
        // [제안] 런타임 상태 — 나중에 세이브 DTO와 1:1 매핑
        // -------------------------------------------------------------------------

        [Header("마을 레벨 (제안: 세이브 townLevel의 진실 소스)")]
        [SerializeField] private int townLevel = 1;

        [Header("사이클 게이트 (제안: 추후 세이브 대상)")]
        [Tooltip("이번 마을 레벨 구간에서 Click 요소 업그레이드를 1회 완료했는지")]
        [SerializeField] private bool cycleClickDone;
        [SerializeField] private bool cycleTypingDone;
        [SerializeField] private bool cycleToolDone;

        [Header("배치 동물 확정본 (제안: VillageAnimalSet의 villageAnimalIds 이관 후보)")]
        [SerializeField] private List<string> placedAnimalIds = new List<string>();

        [Header("배치 용량 규칙 (제안: AnimalSet UI의 unlockedByLevel 이관 후보)")]
        [SerializeField] private int maxPlacementCapacity = 20;
        [SerializeField] private int unlockedSlotsAtTownLevel1 = 5;
        [SerializeField]
        private int[] unlockedSlotsByTownLevel =
        {
        5, 6, 7, 9, 10, 12, 13, 14, 16, 20
    };

        /// <summary>상태가 바뀌면 UI/세이브 구독자가 갱신할 때 사용 (미연동).</summary>
        public event Action OnVillageStateChanged;

        public int TownLevel => Mathf.Max(1, townLevel);
        public bool CycleClickDone => cycleClickDone;
        public bool CycleTypingDone => cycleTypingDone;
        public bool CycleToolDone => cycleToolDone;
        public int CycleCompletedCount =>
            (cycleClickDone ? 1 : 0) + (cycleTypingDone ? 1 : 0) + (cycleToolDone ? 1 : 0);
        public bool IsReadyForVillageLevelUp => CycleCompletedCount >= 3;
        public IReadOnlyList<string> PlacedAnimalIds => placedAnimalIds;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        // -------------------------------------------------------------------------
        // [제안] 조회 API — UI / 가챠 / 도구상한이 읽을 표면
        // -------------------------------------------------------------------------

        /// <summary>해금된 배치 슬롯 수 (마을 레벨 기반). AnimalSet UI GetUnlockedSlotCount 대체 후보.</summary>
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

        // -------------------------------------------------------------------------
        // [제안] 변경 API — UI 버튼이 호출할 표면 (현재 미사용)
        // -------------------------------------------------------------------------

        /// <summary>
        /// 사이클에서 해당 요소 완료 처리만 기록.
        /// 실제 코인 차감·영구 레벨업은 TownUpgradeManager.TryUpgrade* 성공 후에만 호출하는 것을 권장.
        /// </summary>
        public bool TryMarkCycleTrackDone(VillageElementTrack track)
        {
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
        /// 마을 레벨업 성공 후 호출. 사이클 플래그 리셋 + townLevel++.
        /// 비용 차감은 CoinManager / UI에서 선행하는 흐름을 권장 (현 VillageUpgradeUI_Manager와 동일).
        /// </summary>
        public bool TryAdvanceTownLevelAfterPaid()
        {
            if (!IsReadyForVillageLevelUp)
                return false;

            townLevel = TownLevel + 1;
            ResetCycleFlagsOnly();
            RaiseStateChanged();
            return true;
        }

        /// <summary>배치 확정본 교체 (AnimalSet Confirm 시).</summary>
        public void SetPlacedAnimalIds(IReadOnlyList<string> animalIds)
        {
            placedAnimalIds.Clear();
            if (animalIds != null)
            {
                for (int i = 0; i < animalIds.Count; i++)
                    placedAnimalIds.Add(animalIds[i] ?? string.Empty);
            }

            RaiseStateChanged();
        }

        /// <summary>세이브/디버그용 마을 레벨 강제 설정. 사이클은 유지(로드 시) 또는 별도 리셋.</summary>
        public void SetTownLevel(int level, bool resetCycle = false)
        {
            townLevel = Mathf.Max(1, level);
            if (resetCycle)
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
        // [제안] 세이브 DTO — GameSaveData에 넣거나 nested로 직렬화할 후보
        // (SaveManager 연동은 하지 않음)
        // -------------------------------------------------------------------------

        [Serializable]
        public class VillageSaveSnapshot
        {
            public int townLevel = 1;
            public bool cycleClickDone;
            public bool cycleTypingDone;
            public bool cycleToolDone;
            public List<string> placedAnimalIds = new List<string>();
            // 요소 영구 레벨은 기존 GameSaveData.clickUpgradeLevel 등을 유지하거나
            // 여기로 옮길지 팀 합의 필요 (TownUpgradeManager와 중복 저장 주의).
        }

        /// <summary>현재 런타임 → 스냅샷 (SaveManager가 나중에 호출).</summary>
        public VillageSaveSnapshot CaptureSaveSnapshot()
        {
            return new VillageSaveSnapshot
            {
                townLevel = TownLevel,
                cycleClickDone = cycleClickDone,
                cycleTypingDone = cycleTypingDone,
                cycleToolDone = cycleToolDone,
                placedAnimalIds = new List<string>(placedAnimalIds)
            };
        }

        /// <summary>스냅샷 → 런타임 (LoadGame 시 호출 후보).</summary>
        public void ApplySaveSnapshot(VillageSaveSnapshot snapshot)
        {
            if (snapshot == null)
                return;

            townLevel = Mathf.Max(1, snapshot.townLevel);
            cycleClickDone = snapshot.cycleClickDone;
            cycleTypingDone = snapshot.cycleTypingDone;
            cycleToolDone = snapshot.cycleToolDone;

            placedAnimalIds.Clear();
            if (snapshot.placedAnimalIds != null)
                placedAnimalIds.AddRange(snapshot.placedAnimalIds);

            RaiseStateChanged();
        }

        // -------------------------------------------------------------------------
        // [제안] 이관 체크리스트 (문서용 — 코드 실행 없음)
        // -------------------------------------------------------------------------
        // [ ] VillageUpgradeUI_Manager.uiTownLevel / clickDone* → 이 클래스 필드로 이전
        // [ ] VillageAnimalSetUI_Manager.villageAnimalIds / unlockedByLevel → 이전
        // [ ] SaveManager Save/Load가 CaptureSaveSnapshot / ApplySaveSnapshot 사용
        // [ ] ITownLevelProvider를 이 클래스가 구현 (또는 VillageUpgradeUI가 Instance에 위임)
        // [ ] InventoryManager_Tool / GachaManagerBase의 townLevelProviderSource를 이 오브젝트로 교체
        // [ ] DontDestroyOnLoad 여부·씬 배치를 CoinManager/TownUpgradeManager와 맞춤
        // [ ] 사이클 저장 시점(자동저장 60초 / 레벨업 직후) 팀 합의
    }

    /// <summary>사이클 게이트용 요소 트랙 구분 (제안).</summary>
    public enum VillageElementTrack
    {
        Click = 0,
        Typing = 1,
        ToolEfficiency = 2
    }
}