using System;
using System.Collections.Generic;
using TaskTown.Gacha;
using Tool.Data;
using UnityEngine;

namespace TaskTown.KDH
{
    [DefaultExecutionOrder(-100)]
    public class InventoryManager_Tool : MonoBehaviour
    {
        public static InventoryManager_Tool Instance { get; private set; }

        [Tooltip("ICoinWallet을 구현한 컴포넌트(CoinManager)를 연결합니다. 비워두면 코인 비용 체크 없이 중복 개수만으로 레벨업합니다.")]
        [SerializeField] private MonoBehaviour coinWalletSource;
        private ICoinWallet CoinWallet => coinWalletSource as ICoinWallet;

        // #18(도구 상한): ITownLevelProvider를 구현한 컴포넌트(VillageUpgradeUI_Manager)를 연결합니다.
        // 비워두면 마을 레벨 1로 취급합니다(GachaManagerBase와 동일한 패턴).
        [Tooltip("ITownLevelProvider(+ IEndlessModeProvider)를 구현한 컴포넌트(VillageUpgradeUI_Manager)를 연결합니다. 비워두면 마을 레벨 1/일반 모드로 취급합니다.")]
        [SerializeField] private MonoBehaviour townLevelProviderSource;
        private ITownLevelProvider townLevelProvider;
        private IEndlessModeProvider endlessModeProvider;

        // 도구 상한(동시에 "동물이 장착된 도구" 슬롯 개수 제한). 마을 레벨을 올리면 늘어납니다.
        // #19 리밸런스: 레벨당 +2씩 무한 증가하는 기존 공식은 실제 보유 가능한 동물+도구 쌍 수를
        // 레벨19~36 부근에서 추월해버려 페이스가 튀는 문제가 있어(사용자 확인),
        // 완만하게 수렴하는 곡선(레벨40에서 30 근처로 수렴)으로 교체했습니다.
        // 공식: toolCapacityBase + toolCapacityAsymptoteRange × (1 - e^(-(레벨-1) / toolCapacityDecayLevels))
        // (Town_Coin_인플레이션_밸런스표.xlsx 시뮬레이션_파라미터 시트와 동일한 값)
        [Header("도구 상한 (#19)")]
        [SerializeField] private int toolCapacityBase = 5;
        [SerializeField] private float toolCapacityAsymptoteRange = 26.01f;
        [SerializeField] private float toolCapacityDecayLevels = 12f;

        [Header("도구 런타임 슬롯")]
        [Tooltip("현재 플레이어가 보유한 도구 슬롯 목록")]
        [SerializeField] private List<SlotData_Tool> toolSlotsList = new List<SlotData_Tool>();

        [Header("도구 데이터 베이스")]
        [SerializeField] private ToolDatabase toolDatabase;

        // ID 기반 슬롯 조회를 위한 런타임 Dictionary
        private Dictionary<string, SlotData_Tool> toolSlotsDic = new Dictionary<string, SlotData_Tool>();

        // 외부에서 인벤토리 슬롯 목록을 읽을 수 있도록 노출하는 프로퍼티
        public IReadOnlyList<SlotData_Tool> ToolSlotsList => toolSlotsList;

        // 도구 인벤토리 데이터가 변경되었을 때 호출
        public event Action OnToolInventoryChanged;
        // 특정 도구 슬롯의 데이터가 변경되었을 때 호출
        public event Action<SlotData_Tool> OnToolSlotChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            DontDestroyOnLoad(gameObject);

            townLevelProvider = townLevelProviderSource as ITownLevelProvider;
            endlessModeProvider = townLevelProviderSource as IEndlessModeProvider;

            InitializeDictionary();
            SortToolSlots();
        }

        private int GetCurrentTownLevel()
        {
            return townLevelProvider != null ? townLevelProvider.CurrentTownLevel : 1;
        }

        // #19: 엔드리스 모드에서는 도구 개별 레벨 5 상한을 해제합니다.
        private bool IsEndlessMode()
        {
            return endlessModeProvider != null && endlessModeProvider.IsEndlessMode;
        }

        /// <summary>
        /// 현재 마을 레벨 기준 도구 상한(동시에 생산 가능한 "동물 장착 도구" 개수)을 반환합니다.
        /// </summary>
        public int GetToolCapacity()
        {
            float value = toolCapacityBase + toolCapacityAsymptoteRange
                * (1f - Mathf.Exp(-(GetCurrentTownLevel() - 1) / toolCapacityDecayLevels));
            return Mathf.RoundToInt(value);
        }

        /// <summary>
        /// 현재 동물이 장착되어 생산 중인 도구 슬롯 개수를 반환합니다.
        /// </summary>
        public int GetActiveToolCount()
        {
            int count = 0;
            for (int i = 0; i < toolSlotsList.Count; i++)
            {
                if (toolSlotsList[i] != null && toolSlotsList[i].CurrentAnimalSet)
                    count++;
            }
            return count;
        }

        /// <summary>
        /// 슬롯 리스트 기반으로 Dictionary 를 다시 구성
        /// </summary>
        private void InitializeDictionary()
        {
            toolSlotsDic.Clear();

            for (int i = toolSlotsList.Count - 1; i >= 0; i--)
            {
                SlotData_Tool slot = toolSlotsList[i];

                if (slot == null)
                {
                    toolSlotsList.RemoveAt(i);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(slot.ToolId))
                {
                    Debug.LogWarning("[InventoryManager_Tool] ToolId가 비어 있는 슬롯을 제거합니다.");

                    toolSlotsList.RemoveAt(i);
                    continue;
                }

                if (toolSlotsDic.ContainsKey(slot.ToolId))
                {
                    Debug.LogWarning($"[InventoryManager_Tool] 중복 ToolId 슬롯을 제거합니다: {slot.ToolId}");

                    toolSlotsList.RemoveAt(i);
                    continue;
                }

                toolSlotsDic.Add(slot.ToolId, slot);

                // 성장 수치 계산 시스템 연결
                RefreshSlotGrowthData(slot);
            }
        }

        /// <summary>
        /// 도구 ID 로 해당하는 동물 데이터를 반환
        /// </summary>
        public ToolDataSO GetToolData(string toolId)
        {
            if (string.IsNullOrWhiteSpace(toolId)) return null;

            if (toolDatabase == null)
            {
                Debug.LogWarning("[InventoryManager_Tool] ToolDatabase가 연결되지 않았습니다.");
                return null;
            }

            return toolDatabase.GetToolData(toolId);
        }

        /// <summary>
        /// 도구 슬롯 추가. 최초 획득이면 슬롯을 생성하고, 중복 획득이면 기존 슬롯의 개수를 증가
        /// </summary>
        public bool AddToolSlot(ToolDataSO toolData)
        {
            if (toolData == null)
            {
                Debug.LogWarning("[InventoryManager_Tool] 도구의 ToolDataSO가 없습니다..");
                return false;
            }

            if (string.IsNullOrWhiteSpace(toolData.Id))
            {
                Debug.LogWarning($"[InventoryManager_Tool] {toolData.DisplayName} 도구 Id가 비어있습니다.");
                return false;
            }

            if (toolSlotsDic.TryGetValue(toolData.Id, out SlotData_Tool existingSlot))
            {
                existingSlot.AddCount();

                // 성장 수치 갱신 시스템 연동
                NotifySlotChanged(existingSlot);

                Debug.Log($"[InventoryManager_Tool] 중복 도구 획득: " +
                    $"{toolData.DisplayName} + 1 / 현재 개수: {existingSlot.CurrentCount}");

                return true;
            }

            SlotData_Tool newSlot = new SlotData_Tool(
                toolData,
                level: 1,
                currentCount: 1,
                currentSet: false,
                currentAnimalSet: false,
                currentAnimalId: null);

            RefreshSlotGrowthData(newSlot);

            toolSlotsList.Add(newSlot);
            toolSlotsDic.Add(toolData.Id, newSlot);

            SortToolSlots();
            NotifySlotChanged(newSlot);

            Debug.Log($"[InventoryManager_Tool] 신규 도구 획득: {toolData.DisplayName}");

            return true;
        }

        /// <summary>
        /// 도구 ID로 런타임 슬롯 조회
        /// </summary>
        public bool TryGetToolSlot(string toolId, out SlotData_Tool slot)
        {
            slot = null;

            if (string.IsNullOrWhiteSpace(toolId)) return false;

            return toolSlotsDic.TryGetValue(toolId, out slot);
        }

        /// <summary>
        /// 도구 보유수량 반환. 미보유 시 0 반환
        /// </summary>
        public int GetToolCount(string toolId)
        {
            return TryGetToolSlot(toolId, out SlotData_Tool slot) ? slot.CurrentCount : 0;
        }

        /// <summary>
        /// 해당 도구를 한 번 이상 획득했는지 확인. 도감 해금 여부로 사용.
        /// </summary>
        public bool IsToolUnlocked(string toolId)
        {
            return TryGetToolSlot(toolId, out _);
        }

        /// <summary>
        /// 도구 레벨업 가능 여부 확인 (중복 개수 + 코인 잔액)
        /// </summary>
        public bool CanLevelUpTool(string toolId)
        {
            if (!TryGetToolSlot(toolId, out SlotData_Tool slot)) return false;

            if (!slot.CanLevelUp(IsEndlessMode())) return false;

            long coinCost = slot.GetLevelUpCoinCost();

            if (CoinWallet != null && CoinWallet.Balance < coinCost)
            {
                Debug.Log($"[InventoryManager_Tool] not enough coin. needed: {coinCost}, balance: {CoinWallet.Balance}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 도구 레벨업 시도
        /// </summary>
        public bool TryLevelUpTool(string toolId)
        {
            if (!TryGetToolSlot(toolId, out SlotData_Tool slot))
            {
                Debug.LogWarning($"[InventoryManager_Tool] 존재하지 않는 슬롯입니다: {toolId}");
                return false;
            }

            bool endless = IsEndlessMode();
            if (!endless && slot.IsMaxLevel)
            {
                Debug.Log($"[InventoryManager_Tool] 이미 최대 레벨인 도구입니다: {toolId}");
                return false;
            }

            if (!CanLevelUpTool(toolId)) return false;

            long coinCost = slot.GetLevelUpCoinCost();

            if (CoinWallet != null && !CoinWallet.TrySpend(coinCost))
            {
                Debug.Log($"[InventoryManager_Tool] failed to spend coin for level up: {toolId}, cost: {coinCost}");
                return false;
            }

            // 재료 소모 후 레벨업 (본체 1개는 유지)
            if (!slot.TryConsumeForLevelUp(endless))
            {
                Debug.Log($"[InventoryManager_Tool] 재료 소모 실패: {toolId}");
                return false;
            }

            slot.ToolLevelUp(endless);

            // 다음 레벨 요구치 갱신
            RefreshSlotGrowthData(slot);

            NotifySlotChanged(slot);

            Debug.Log($"[InventoryManager_Tool] 도구 레벨업 성공: {toolId} / 현재 레벨 {slot.Level}");

            return true;
        }

        /// <summary>
        /// 도구를 배치 상태로 설정합니다.
        /// </summary>
        public bool TrySetTool(string toolId)
        {
            if (!TryGetToolSlot(toolId, out SlotData_Tool slot))
            {
                Debug.LogWarning($"[InventoryManager_Tool] 존재하지 않는 슬롯입니다: {toolId}");
                return false;
            }

            slot.SetPlaced(true);
            NotifySlotChanged(slot);

            return true;
        }

        /// <summary>
        /// 도구 배치를 해제합니다.
        /// </summary>
        public bool TryUnsetTool(string toolId)
        {
            if (!TryGetToolSlot(toolId, out SlotData_Tool slot))
            {
                Debug.LogWarning($"[InventoryManager_Tool] 존재하지 않는 슬롯입니다: {toolId}");
                return false;
            }

            slot.SetPlaced(false);
            slot.ClearAssignedAnimal();
            NotifySlotChanged(slot);

            return true;
        }

        /// <summary>
        /// 도구에 동물을 배치합니다.
        /// </summary>
        public bool TryAssignAnimalToTool(string toolId, string animalId)
        {
            if (!TryGetToolSlot(toolId, out SlotData_Tool slot))
            {
                Debug.LogWarning($"[InventoryManager_Tool] 존재하지 않는 슬롯입니다: {toolId}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(animalId))
            {
                Debug.LogWarning("[InventoryManager_Tool] 배치할 AnimalId가 비어 있습니다.");
                return false;
            }

            // #18(도구 상한): 이 슬롯이 지금 비활성 상태에서 새로 활성화되는 경우에만 상한 체크.
            // 이미 활성 상태인 도구에 다른 동물을 재배치하는 건 활성 슬롯 개수가 늘지 않으므로 통과.
            if (!slot.CurrentAnimalSet && GetActiveToolCount() >= GetToolCapacity())
            {
                Debug.Log($"[InventoryManager_Tool] 도구 상한 초과로 장착 불가: {toolId} (상한 {GetToolCapacity()})");
                return false;
            }

            slot.SetAssignedAnimal(animalId);

            // 26.07.29. KAY 수정
            // 특화 동물에게 장착되면 상세 UI에서 이름을 해금합니다.
            if (slot.ToolData != null &&
                !string.IsNullOrEmpty(slot.ToolData.SpecialAnimalId) &&
                slot.ToolData.SpecialAnimalId == animalId)
            {
                slot.RevealSpecialAnimal();
            }

            NotifySlotChanged(slot);

            return true;
        }

        /// <summary>
        /// 도구에 배치된 동물을 해제합니다.
        /// </summary>
        public bool TryRemoveAnimalFromTool(string toolId)
        {
            if (!TryGetToolSlot(toolId, out SlotData_Tool slot))
            {
                Debug.LogWarning($"[InventoryManager_Tool] 존재하지 않는 슬롯입니다: {toolId}");
                return false;
            }

            slot.ClearAssignedAnimal();
            NotifySlotChanged(slot);

            return true;
        }

        /// <summary>
        /// 현재 도구 슬롯 데이터를 저장용 리스트 형식으로 변환합니다.
        /// </summary>
        public List<SlotSaveData_Tool> CreateSaveData()
        {
            List<SlotSaveData_Tool> saveDataList = new();

            foreach (SlotData_Tool slot in toolSlotsList)
            {
                if (slot == null)
                    continue;

                if (string.IsNullOrWhiteSpace(slot.ToolId))
                    continue;

                // SlotSaveData_Tool saveData = new SlotSaveData_Tool(
                //     slot.ToolData,
                //     slot.Level,
                //     slot.CurrentCount,
                //     slot.CurrentSet,
                //     slot.CurrentAnimalSet,
                //     slot.CurrentAnimalId);
                // 26.07.29. KAY 수정
                SlotSaveData_Tool saveData = new SlotSaveData_Tool(
                    slot.ToolData,
                    slot.Level,
                    slot.CurrentCount,
                    slot.CurrentSet,
                    slot.CurrentAnimalSet,
                    slot.CurrentAnimalId,
                    slot.HasRevealedSpecialAnimal);

                saveDataList.Add(saveData);
            }

            return saveDataList;
        }

        /// <summary>
        /// 저장 데이터를 기반으로 런타임 도구 인벤토리를 복원합니다.
        /// </summary>
        public void LoadSaveData(List<SlotSaveData_Tool> saveDataList)
        {
            toolSlotsList.Clear();
            toolSlotsDic.Clear();

            if (saveDataList == null)
            {
                NotifyInventoryChanged();
                return;
            }

            foreach (SlotSaveData_Tool saveData in saveDataList)
            {
                if (saveData == null)
                    continue;

                if (saveData.tooldata == null || string.IsNullOrWhiteSpace(saveData.tooldata.Id))
                    continue;

                if (saveData.currentCount <= 0)
                    continue;

                if (toolSlotsDic.ContainsKey(saveData.tooldata.Id))
                {
                    Debug.LogWarning($"[InventoryManager_Tool] 저장 데이터에 중복 ID가 있습니다: {saveData.tooldata.Id}");
                    continue;
                }

                // SlotData_Tool runtimeSlot = new SlotData_Tool(
                //     saveData.tooldata,
                //     saveData.level,
                //     saveData.currentCount,
                //     saveData.currentSet,
                //     saveData.currentAnimalSet,
                //     saveData.currentAnimalId);
                // 26.07.29. KAY 수정
                SlotData_Tool runtimeSlot = new SlotData_Tool(
                    saveData.tooldata,
                    saveData.level,
                    saveData.currentCount,
                    saveData.currentSet,
                    saveData.currentAnimalSet,
                    saveData.currentAnimalId,
                    saveData.hasRevealedSpecialAnimal);

                // 구 세이브 호환: 이미 특화 동물이 장착된 상태면 해금으로 보정
                // 26.07.29. KAY 수정
                if (!runtimeSlot.HasRevealedSpecialAnimal &&
                    runtimeSlot.CurrentAnimalSet &&
                    runtimeSlot.ToolData != null &&
                    !string.IsNullOrEmpty(runtimeSlot.ToolData.SpecialAnimalId) &&
                    runtimeSlot.CurrentAnimalId == runtimeSlot.ToolData.SpecialAnimalId)
                {
                    runtimeSlot.RevealSpecialAnimal();
                }

                // 성장 수치 계산 시스템 연결
                RefreshSlotGrowthData(runtimeSlot);

                toolSlotsList.Add(runtimeSlot);
                toolSlotsDic.Add(runtimeSlot.ToolId, runtimeSlot);
            }

            SortToolSlots();
            NotifyInventoryChanged();
        }

        /// <summary>
        /// 등급 높은 순으로만 정렬합니다. (동일 등급 내 Index 정렬은 추후 단계)
        /// </summary>
        private void SortToolSlots()
        {
            toolSlotsList.Sort(CompareToolSlots);
        }

        private static int CompareToolSlots(SlotData_Tool a, SlotData_Tool b)
        {
            ToolDataSO dataA = a != null ? a.ToolData : null;
            ToolDataSO dataB = b != null ? b.ToolData : null;

            if (dataA == null && dataB == null) return 0;
            if (dataA == null) return 1;
            if (dataB == null) return -1;

            return ((int)dataB.Grade).CompareTo((int)dataA.Grade);
        }

        /// <summary>
        /// 현재 레벨을 기준으로 다음 레벨의 요구 개수와 비용을 갱신합니다.
        /// 요구치 계산 시스템은 LevelUpRequirementCalculator 에 위임합니다.
        /// </summary>
        private void RefreshSlotGrowthData(SlotData_Tool slot)
        {
            if (slot == null)
                return;

            int requiredCount = LevelUpRequirementCalculator.GetRequiredDuplicateCount(slot.Level);

            // 기존엔 maxLevel 인자가 항상 false로 고정되어 있어서, ToolLevelUp()이 레벨5에서 설정한
            // isMaxLevel을 이 호출이 곧바로 다시 풀어버리는 버그가 있었습니다(#19 작업 중 발견).
            // 실제 레벨 기준으로 다시 계산하도록 수정 - 엔드리스 모드에서는 항상 false(상한 없음).
            bool isMax = !IsEndlessMode() && slot.Level >= 5;
            slot.ApplyGrowthData(requiredCount, slot.LevelUpCost, isMax);
        }

        /// <summary>
        /// 모든 도구 런타임 데이터를 삭제합니다.
        /// 새 게임 또는 저장 데이터 로드 전에 사용될 수 있습니다.
        /// </summary>
        public void ClearToolInventory()
        {
            toolSlotsList.Clear();
            toolSlotsDic.Clear();

            NotifyInventoryChanged();
        }

        private void NotifySlotChanged(SlotData_Tool slot)
        {
            OnToolSlotChanged?.Invoke(slot);
            OnToolInventoryChanged?.Invoke();
        }

        /// <summary>
        /// 도구 인벤토리 변경 호출
        /// </summary>
        private void NotifyInventoryChanged()
        {
            OnToolInventoryChanged?.Invoke();
        }


        /// <summary>
        /// 디버그 전용: 지정 개수만큼 도구를 지급합니다.    26.07.24 KDH 추가
        /// </summary>
        public bool DebugAddTool(string toolId, int count)
        {
            ToolDataSO data = GetToolData(toolId);
            if (data == null)
            {
                Debug.LogWarning($"[InventoryManager_Tool] 존재하지 않는 ToolId: {toolId}");
                return false;
            }
            count = Mathf.Max(1, count);
            for (int i = 0; i < count; i++)
                AddToolSlot(data);
            return true;
        }
        /// <summary>
        /// 디버그 전용: 보유 도구의 레벨을 바로 설정합니다. 미보유면 1개 지급 후 설정.     26.07.24 KDH 추가
        /// </summary>
        public bool DebugSetToolLevel(string toolId, int targetLevel)
        {
            if (!TryGetToolSlot(toolId, out SlotData_Tool slot))
            {
                if (!DebugAddTool(toolId, 1))
                    return false;
                if (!TryGetToolSlot(toolId, out slot))
                    return false;
            }
            slot.DebugSetLevel(targetLevel);
            RefreshSlotGrowthData(slot);
            NotifySlotChanged(slot);
            return true;
        }
    }
}
