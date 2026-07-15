using System;
using System.Collections.Generic;
using TaskTown.Gacha;
using Tool.Data;
using UnityEngine;

namespace TaskTown.KDH
{
    public class InventoryManager_Tool : MonoBehaviour
    {
        public static InventoryManager_Tool Instance { get; private set; }

        [Header("도구 런타임 슬롯")]
        [Tooltip("현재 플레이어가 보유한 도구 슬롯 목록")]
        [SerializeField] private List<SlotData_Tool> toolSlotsList = new List<SlotData_Tool>();

        [Header("도구 데이터 베이스")]
        [SerializeField] private ToolDatabase toolDatabase;

        // ID 기반 빠른 조회를 위한 런타임 Dictionary
        private Dictionary<string, SlotData_Tool> toolSlotsDic = new Dictionary<string, SlotData_Tool>();

        // 외부에서 인벤토리 도구 목록을 읽을 수 있도록 제공하는 프로퍼티
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

            InitializeDictionary();
        }

        /// <summary>
        /// 런타임 리스트 기반으로 Dictionary 를 다시 생성
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
        /// 도구 ID에 해당하는 고정 데이터 반환
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
        /// 도구 슬롯 추가. 최초 획득이면 슬롯을 만들고, 중복 획득이면 기존 슬롯 수량 증가
        /// </summary>
        public bool AddToolSlot(ToolDataSO toolData)
        {
            if (toolData == null)
            {
                Debug.LogWarning("[InventoryManager_Tool] 추가할 ToolDataSO가 없습니다.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(toolData.Id))
            {
                Debug.LogWarning($"[InventoryManager_Tool] {toolData.DisplayName} 의 Id 가 비어있습니다.");
                return false;
            }

            if (toolSlotsDic.TryGetValue(toolData.Id, out SlotData_Tool existingSlot))
            {
                existingSlot.AddCount();

                // 성장 수치 계산 시스템 연결
                NotifySlotChanged(existingSlot);

                Debug.Log($"[InventoryManager_Tool] 중복 도구 획득:" +
                    $"{toolData.DisplayName} + 1 / 현재 수량: {existingSlot.CurrentCount}");

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

            NotifySlotChanged(newSlot);

            Debug.Log($"[InventoryManager_Tool] 새로운 도구 획득: {toolData.DisplayName}");

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
        /// 현재 보유 중인 도구의 총 수량 반환.
        /// 보유하지 않은 도구는 0 반환
        /// </summary>
        public int GetToolCount(string toolId)
        {
            return TryGetToolSlot(toolId, out SlotData_Tool slot) ? slot.CurrentCount : 0;
        }

        /// <summary>
        /// 해당 도구를 한 번 이상 획득했는지 확인.
        /// 도감에서는 이 값을 해금 여부로 사용.
        /// </summary>
        public bool IsToolUnlocked(string toolId)
        {
            return TryGetToolSlot(toolId, out _);
        }

        /// <summary>
        /// 도구가 현재 레벨업 가능한지 확인
        /// </summary>
        public bool CanLevelUpTool(string toolId)
        {
            if (!TryGetToolSlot(toolId, out SlotData_Tool slot)) return false;

            if (slot.IsMaxLevel) return false;

            int neededAmount = slot.GetLevelUpCost();

            if (neededAmount <= 0)
            {
                Debug.Log("[InventoryManager_Tool] 레벨업 요구 수량이 0 이하입니다.");
                return false;
            }

            if (slot.CurrentCount < neededAmount)
            {
                Debug.Log($"[InventoryManager_Tool] 레벨업 요구 수량이 부족합니다. 필요 수량: {neededAmount + 1 - slot.CurrentCount}");
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
                Debug.LogWarning($"[InventoryManager_Tool] 보유하지 않은 도구입니다: {toolId}");
                return false;
            }

            if (slot.IsMaxLevel)
            {
                Debug.Log($"[InventoryManager_Tool] 이미 최대 레벨인 도구입니다: {toolId}");
                return false;
            }

            if (!CanLevelUpTool(toolId))
                return false;

            slot.ToolLevelUp();

            // 성장 수치 계산 시스템 연결
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
                Debug.LogWarning($"[InventoryManager_Tool] 보유하지 않은 도구입니다: {toolId}");
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
                Debug.LogWarning($"[InventoryManager_Tool] 보유하지 않은 도구입니다: {toolId}");
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
                Debug.LogWarning($"[InventoryManager_Tool] 보유하지 않은 도구입니다: {toolId}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(animalId))
            {
                Debug.LogWarning("[InventoryManager_Tool] 배치할 AnimalId가 비어 있습니다.");
                return false;
            }

            slot.SetAssignedAnimal(animalId);
            NotifySlotChanged(slot);

            return true;
        }

        /// <summary>
        /// 도구에 배치된 동물을 제거합니다.
        /// </summary>
        public bool TryRemoveAnimalFromTool(string toolId)
        {
            if (!TryGetToolSlot(toolId, out SlotData_Tool slot))
            {
                Debug.LogWarning($"[InventoryManager_Tool] 보유하지 않은 도구입니다: {toolId}");
                return false;
            }

            slot.ClearAssignedAnimal();
            NotifySlotChanged(slot);

            return true;
        }

        /// <summary>
        /// 현재 런타임 도구 데이터를 저장용 슬롯 목록으로 변환합니다.
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

                SlotSaveData_Tool saveData = new SlotSaveData_Tool(
                    slot.ToolData,
                    slot.Level,
                    slot.CurrentCount,
                    slot.CurrentSet,
                    slot.CurrentAnimalSet,
                    slot.CurrentAnimalId);

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

                SlotData_Tool runtimeSlot = new SlotData_Tool(
                    saveData.tooldata,
                    saveData.level,
                    saveData.currentCount,
                    saveData.currentSet,
                    saveData.currentAnimalSet,
                    saveData.currentAnimalId);

                // 성장 수치 계산 시스템 연결
                RefreshSlotGrowthData(runtimeSlot);

                toolSlotsList.Add(runtimeSlot);
                toolSlotsDic.Add(runtimeSlot.ToolId, runtimeSlot);
            }

            NotifyInventoryChanged();
        }

        /// <summary>
        /// 현재 레벨을 기반으로 레벨업 요구 수량과 비용 갱신.
        /// 성장 수치 계산 시스템이 구현되면 연결합니다.
        /// </summary>
        private void RefreshSlotGrowthData(SlotData_Tool slot)
        {
            if (slot == null)
                return;

            int requiredCount = LevelUpRequirementCalculator.GetRequiredDuplicateCount(slot.Level);

            slot.ApplyGrowthData(requiredCount, slot.LevelUpCost, false);
        }

        /// <summary>
        /// 모든 도구 런타임 데이터를 제거합니다.
        /// 새 게임 또는 저장 데이터 로드 전에 사용할 수 있습니다.
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
    }
}
