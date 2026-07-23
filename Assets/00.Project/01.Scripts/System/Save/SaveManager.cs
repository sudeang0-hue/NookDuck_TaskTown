using System.Collections.Generic;
using System.IO;
using TaskTown.Gacha;
using TaskTown.Gacha.Demo;
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

        private const string SaveFileName = "gamesave.json";
        private const float AutoSaveIntervalSeconds = 60f;

        [Tooltip("CoinManager를 연결합니다(코인 저장/복원용).")]
        [SerializeField] private CoinManager coinManager;

        [Tooltip("ITownLevelProvider를 구현한 컴포넌트(예: DemoTownLevelProvider). 마을 레벨 저장/복원용.")]
        [SerializeField] private MonoBehaviour townLevelProviderSource;

        private ITownLevelProvider TownLevelProvider => townLevelProviderSource as ITownLevelProvider;
        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

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
            return File.Exists(SavePath);
        }

        public void SaveGame()
        {
            GameSaveData data = new GameSaveData();

            if (coinManager != null)
                data.coins = coinManager.Balance;

            if (TownLevelProvider != null)
                data.townLevel = TownLevelProvider.CurrentTownLevel;

            if (RealProductionTicker.Instance != null)
                data.productionRatePerSecond = RealProductionTicker.Instance.CalculateTotalCoinPerSecond();

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

                    data.tools.Add(new ToolSaveEntry
                    {
                        id = slot.ToolId,
                        level = slot.Level,
                        count = slot.CurrentCount,
                        currentSet = slot.CurrentSet,
                        currentAnimalSet = slot.CurrentAnimalSet,
                        currentAnimalId = slot.CurrentAnimalId
                    });
                }
            }

            try
            {
                File.WriteAllText(SavePath, JsonUtility.ToJson(data));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveManager] 저장 실패: {e.Message}");
            }
        }

        // 저장돼 있던 초당 생산량을 반환합니다(오프라인 보상 계산용). 세이브가 없으면 0.
        public float LoadGame()
        {
            if (!File.Exists(SavePath))
                return 0f;

            GameSaveData data;
            try
            {
                data = JsonUtility.FromJson<GameSaveData>(File.ReadAllText(SavePath));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveManager] 로드 실패: {e.Message}");
                return 0f;
            }

            if (data == null)
                return 0f;

            // 코인
            if (coinManager != null)
                coinManager.SetCoin(data.coins);

            // 마을 레벨 (현재는 임시 DemoTownLevelProvider만 세터를 가짐)
            if (townLevelProviderSource is DemoTownLevelProvider demoProvider)
                demoProvider.SetLevel(data.townLevel);

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

                    toolSaves.Add(new SlotSaveData_Tool(
                        so, entry.level, entry.count,
                        entry.currentSet, entry.currentAnimalSet, entry.currentAnimalId));
                }

                toolManager.LoadSaveData(toolSaves);
            }

            return data.productionRatePerSecond;
        }

        // 엔딩 후 난이도 리셋 등에서 진행 데이터를 완전히 초기화할 때 사용합니다.
        public void DeleteSave()
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);
        }
    }
}
