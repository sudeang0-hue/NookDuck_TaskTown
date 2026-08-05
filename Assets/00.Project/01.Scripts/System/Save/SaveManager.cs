using System.Collections.Generic;
using Manager;
using TaskTown.Gacha;
using TaskTown.Gacha.Demo;
using UI;
using UnityEngine;

namespace TaskTown.KDH
{
    // 코인 / 동물·도구 인벤토리(레벨·개수·장착 상태) / 마을 레벨을 JSON으로 저장·복원합니다.
    // 도감 기록과 오프라인 마지막 접속 시각은 각각 DexRecordManager / OfflineRewardManager가
    // PlayerPrefs로 따로 저장하므로 여기서는 다루지 않습니다.
    //
    // 실행 순서를 앞당겨서(-100), Start()의 로드가 OfflineRewardManager처럼 "복원된 인벤토리"에
    // 의존하는 매니저들의 Start()보다 먼저 끝나도록 보장합니다(오프라인 보상이 복원된 생산량
    // 기준으로 계산되게 하기 위함).
    [DefaultExecutionOrder(-100)]
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        private const float AutoSaveIntervalSeconds = 60f;

        [Tooltip("CoinManager를 연결합니다(코인 저장/복원용).")]
        [SerializeField] private CoinManager coinManager;

        [Tooltip("ITownLevelProvider 구현체. 마을 관리 패널은 VillageUpgradeUI_Manager를 연결하세요.")]
        [SerializeField] private MonoBehaviour townLevelProviderSource;

        private ITownLevelProvider TownLevelProvider => townLevelProviderSource as ITownLevelProvider;

        // -----------------------------------------------------------------------------
        // [ 2026.07.27 - Choi - 튜토리얼 기능 업데이트 ]
        // 기능: TutorialManager가 전달한 진행 상태를 복사해 안전하게 보관합니다.
        // -----------------------------------------------------------------------------
        private TutorialSaveData tutorialProgress = TutorialSaveData.CreateDefault();

        public TutorialSaveData TutorialProgress => tutorialProgress.Copy();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            float loadedProductionRate = LoadGame();

            // 오프라인 보상은 "저장 시점에 기록해 둔 생산량"으로 계산합니다. 실시간 생산량에
            // 의존하면 로드 타이밍(인벤토리 복원 완료 시점)에 따라 0으로 잡힐 수 있어서입니다.
            if (OfflineRewardManager.Instance != null)
                OfflineRewardManager.Instance.CheckOfflineReward(loadedProductionRate);

            InvokeRepeating(nameof(SaveGame), AutoSaveIntervalSeconds, AutoSaveIntervalSeconds);
        }

        private void OnDestroy()
        {
            CancelInvoke(nameof(SaveGame));
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (isPaused)
                SaveGame();
        }

        private void OnApplicationQuit()
        {
            SaveGame();
        }

        public bool HasSave()
        {
            return GameSaveStorage.Exists();
        }

        public void SaveGame()
        {
            GameSaveData data = new GameSaveData();

            // -----------------------------------------------------------------------------
            // [ 2026.07.27 - Choi - 튜토리얼 기능 업데이트 ]
            // 기능: 현재 튜토리얼 진행 상태를 기존 게임 저장 JSON에 함께 포함합니다.
            // -----------------------------------------------------------------------------
            data.tutorial = tutorialProgress.Copy();

            if (coinManager != null)
                data.coins = coinManager.Balance;

            // 마을 레벨: 관리 패널이 있으면 그쪽을 진실 소스로 저장
            // VillageUpgradeUI_Manager villageUpgradeForSave = FindFirstObjectByType<VillageUpgradeUI_Manager>();
            // if (villageUpgradeForSave != null)
            //     data.townLevel = villageUpgradeForSave.UiTownLevel;
            // else if (TownLevelProvider != null)
            //     data.townLevel = TownLevelProvider.CurrentTownLevel;

            // 2026.08.02 - KAY - VillageSystemManager 스냅샷으로 마을 레벨/사이클/엔드리스 저장
            WriteVillageProgressToSaveData(data);

            if (RealProductionTicker.Instance != null)
                data.productionRatePerSecond = RealProductionTicker.Instance.CalculateTotalCoinPerSecond();

            if (TownUpgradeManager.Instance != null)
            {
                data.clickUpgradeLevel = TownUpgradeManager.Instance.ClickLevel;
                data.typingUpgradeLevel = TownUpgradeManager.Instance.TypingLevel;
                data.toolEfficiencyUpgradeLevel = TownUpgradeManager.Instance.ToolEfficiencyLevel;
            }

            if (InventoryManager_Animal.Instance != null)
            {
                foreach (SlotData_Animal slot in InventoryManager_Animal.Instance.AnimalSlotsList)
                {
                    if (slot == null || string.IsNullOrEmpty(slot.AnimalId))
                        continue;

                    data.animals.Add(new AnimalSaveEntry
                    {
                        id = slot.AnimalId,
                        level = slot.Level,
                        count = slot.CurrentCount
                    });
                }
            }

            if (InventoryManager_Tool.Instance != null)
            {
                foreach (SlotData_Tool slot in InventoryManager_Tool.Instance.ToolSlotsList)
                {
                    if (slot == null || string.IsNullOrEmpty(slot.ToolId))
                        continue;

                    // data.tools.Add(new ToolSaveEntry
                    // {
                    //     id = slot.ToolId,
                    //     level = slot.Level,
                    //     count = slot.CurrentCount,
                    //     currentSet = slot.CurrentSet,
                    //     currentAnimalSet = slot.CurrentAnimalSet,
                    //     currentAnimalId = slot.CurrentAnimalId
                    // });
                    // 26.07.29. KAY 수정
                    data.tools.Add(new ToolSaveEntry
                    {
                        id = slot.ToolId,
                        level = slot.Level,
                        count = slot.CurrentCount,
                        currentSet = slot.CurrentSet,
                        currentAnimalSet = slot.CurrentAnimalSet,
                        currentAnimalId = slot.CurrentAnimalId,
                        hasRevealedSpecialAnimal = slot.HasRevealedSpecialAnimal
                    });
                }
            }

            if (!GameSaveStorage.Save(data, out string errorMessage))
                Debug.LogError($"[SaveManager] 저장 실패: {errorMessage}");
        }

        // 저장돼 있던 초당 생산량을 반환합니다(오프라인 보상 계산용). 세이브가 없으면 0.
        public float LoadGame()
        {
            if (!GameSaveStorage.Exists())
                return 0f;

            GameSaveLoadStatus loadStatus = GameSaveStorage.Load(
                out GameSaveData data,
                out string errorMessage);

            if (loadStatus == GameSaveLoadStatus.Failed)
            {
                Debug.LogError($"[SaveManager] 로드 실패: {errorMessage}");
                return 0f;
            }

            if (data == null)
                return 0f;

            // -----------------------------------------------------------------------------
            // [ 2026.07.27 - Choi - 튜토리얼 기능 업데이트 ]
            // 기능: 저장 파일에서 불러온 튜토리얼 진행 상태를 런타임 복사본으로 복원합니다.
            // -----------------------------------------------------------------------------
            tutorialProgress = data.tutorial.Copy();

            // 코인
            if (coinManager != null)
                coinManager.SetCoin(data.coins);

            // 클릭/타이핑/도구효율 업그레이드 레벨 복원 (TownUpgradeManager, 이슈 #72)
            if (TownUpgradeManager.Instance != null)
            {
                TownUpgradeManager.Instance.LoadLevels(
                    data.clickUpgradeLevel, data.typingUpgradeLevel, data.toolEfficiencyUpgradeLevel);
            }

            // 마을 레벨 복원: 요소 레벨 적용 후 UI에 townLevel 반영 (사이클 플래그는 추후 세이브)
            // ApplyTownLevelFromSave(data.townLevel);

            // 2026.08.02 - KAY - 마을 레벨+사이클+엔드리스를 VillageSystemManager에 복원 후 UI Refresh
            ApplyVillageProgressFromSave(data);

            // 동물 인벤토리: 저장된 ID로 SO를 다시 조회해서 복원
            InventoryManager_Animal animalManager = InventoryManager_Animal.Instance;
            if (animalManager != null)
            {
                List<SlotSaveData_Animal> animalSaves = new List<SlotSaveData_Animal>();
                foreach (AnimalSaveEntry entry in data.animals)
                {
                    Animal.Data.AnimalDataSO so = animalManager.GetAnimalData(entry.id);
                    if (so == null)
                        continue;

                    animalSaves.Add(new SlotSaveData_Animal(so, entry.level, entry.count));
                }

                animalManager.LoadSaveData(animalSaves);
            }

            // 도구 인벤토리: 저장된 ID로 SO를 다시 조회해서 복원(장착 상태 포함)
            InventoryManager_Tool toolManager = InventoryManager_Tool.Instance;
            if (toolManager != null)
            {
                List<SlotSaveData_Tool> toolSaves = new List<SlotSaveData_Tool>();
                foreach (ToolSaveEntry entry in data.tools)
                {
                    Tool.Data.ToolDataSO so = toolManager.GetToolData(entry.id);
                    if (so == null)
                        continue;

                    // toolSaves.Add(new SlotSaveData_Tool(
                    //     so, entry.level, entry.count,
                    //     entry.currentSet, entry.currentAnimalSet, entry.currentAnimalId));
                    // 26.07.29. KAY 수정
                    toolSaves.Add(new SlotSaveData_Tool(
                        so, entry.level, entry.count,
                        entry.currentSet, entry.currentAnimalSet, entry.currentAnimalId,
                        entry.hasRevealedSpecialAnimal));
                }

                toolManager.LoadSaveData(toolSaves);
            }

            return data.productionRatePerSecond;
        }

        // -----------------------------------------------------------------------------
        // [ 2026.07.27 - Choi - 튜토리얼 기능 업데이트 ]
        // 기능: TutorialManager와 SaveManager가 진행 상태를 복사본으로 교환합니다.
        // -----------------------------------------------------------------------------
        /// <summary>
        /// TutorialManager가 갱신한 진행 상태를 다음 저장에 포함합니다.
        /// 외부에서 전달된 인스턴스는 복사하여 보관합니다.
        /// </summary>
        public void SetTutorialProgress(TutorialSaveData progress)
        {
            tutorialProgress = progress?.Copy() ?? TutorialSaveData.CreateDefault();
        }

        public TutorialSaveData GetTutorialProgressCopy()
        {
            return tutorialProgress.Copy();
        }

        // -----------------------------------------------------------------------------
        // [ 2026.08.03 - Choi - 튜토리얼 다시 보기 ]
        // 기능: 게임 저장은 유지하면서 튜토리얼 진행만 초기화하고 일회성 보상 기록은 보존합니다.
        // -----------------------------------------------------------------------------
        public TutorialSaveData ResetTutorialProgressForReplay()
        {
            tutorialProgress = tutorialProgress.CreateReplayProgress();
            return tutorialProgress.Copy();
        }

        /// <summary>
        /// 세이브 townLevel을 마을 관리 패널(및 하위 호환 Demo)에 적용합니다.
        /// </summary>
        
        // 2026.08.02 - KAY - VillageSystemManager → GameSaveData 진행 상태 기록
        private void WriteVillageProgressToSaveData(GameSaveData data)
        {
            if (data == null)
                return;

            VillageSystemManager villageSystem = VillageSystemManager.Instance;
            if (villageSystem == null)
                villageSystem = FindFirstObjectByType<VillageSystemManager>();

            if (villageSystem != null)
            {
                VillageSystemManager.VillageSaveSnapshot snapshot = villageSystem.CaptureSaveSnapshot();
                data.townLevel = snapshot.townLevel;
                data.cycleClickCount = snapshot.cycleClickCount;
                data.cycleTypingCount = snapshot.cycleTypingCount;
                data.cycleToolCount = snapshot.cycleToolCount;
                data.isEndlessMode = snapshot.isEndlessMode;
                return;
            }

            // System이 없을 때만 UI/Inspector 폴백 (구경로와 동일)
            VillageUpgradeUI_Manager villageUpgradeForSave = FindFirstObjectByType<VillageUpgradeUI_Manager>();
            if (villageUpgradeForSave != null)
                data.townLevel = villageUpgradeForSave.UiTownLevel;
            else if (TownLevelProvider != null)
                data.townLevel = TownLevelProvider.CurrentTownLevel;
        }

        // 2026.08.02 - KAY - GameSaveData → VillageSystemManager 복원 후 UI Refresh
        private void ApplyVillageProgressFromSave(GameSaveData data)
        {
            if (data == null)
                return;

            int level = Mathf.Max(1, data.townLevel);

            VillageSystemManager villageSystem = VillageSystemManager.Instance;
            if (villageSystem == null)
                villageSystem = FindFirstObjectByType<VillageSystemManager>();

            if (villageSystem != null)
            {
                villageSystem.ApplyProgressFromSave(
                    level,
                    data.cycleClickCount,
                    data.cycleTypingCount,
                    data.cycleToolCount,
                    data.isEndlessMode);
            }
            else
            {
                Debug.LogWarning(
                    "[SaveManager] VillageSystemManager가 없어 마을 사이클/엔드리스를 복원할 수 없습니다. townLevel만 UI에 시도합니다.");

                if (townLevelProviderSource is VillageUpgradeUI_Manager villageFromInspector)
                    villageFromInspector.SetTownLevelFromSave(level);
                else
                {
                    VillageUpgradeUI_Manager villageUpgradeFallback =
                        FindFirstObjectByType<VillageUpgradeUI_Manager>();
                    if (villageUpgradeFallback != null)
                        villageUpgradeFallback.SetTownLevelFromSave(level);
                }
            }

            // 로드 직후 UI는 SaveManager 기준으로 한 번 강제 Refresh
            // (OnVillageStateChanged 구독 전에 Load가 끝날 수 있음)
            VillageUpgradeUI_Manager villageUpgradeUi = null;
            if (townLevelProviderSource is VillageUpgradeUI_Manager fromInspector)
                villageUpgradeUi = fromInspector;
            else
                villageUpgradeUi = FindFirstObjectByType<VillageUpgradeUI_Manager>();

            if (villageUpgradeUi != null)
                villageUpgradeUi.RefreshUIAfterSaveRestore();

            // DemoTownLevelProvider가 따로 있으면 동기화 (가챠 등 기존 연결 유지)
            if (townLevelProviderSource is DemoTownLevelProvider demoProvider)
                demoProvider.SetLevel(level);
        }

        // 엔딩 후 난이도 리셋 등에서 진행 데이터를 완전히 초기화할 때 사용합니다.
        public void DeleteSave()
        {
            if (!GameSaveStorage.Delete(out string errorMessage))
                Debug.LogError($"[SaveManager] 저장 파일 삭제 실패: {errorMessage}");
        }
    }
}
