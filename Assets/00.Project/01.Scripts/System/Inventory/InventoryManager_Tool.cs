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

        [Tooltip("ICoinWallet을 구현한 컴포넌트(CoinManager)를 연결합니다. 비워두면 코인 비용 체크 없이 중복 개수만으로 레벨업합니다.")]
        [SerializeField] private MonoBehaviour coinWalletSource;
        private ICoinWallet CoinWallet => coinWalletSource as ICoinWallet;

        [Header("���� ��Ÿ�� ����")]
        [Tooltip("���� �÷��̾ ������ ���� ���� ���")]
        [SerializeField] private List<SlotData_Tool> toolSlotsList = new List<SlotData_Tool>();

        [Header("���� ������ ���̽�")]
        [SerializeField] private ToolDatabase toolDatabase;

        // ID ��� ���� ��ȸ�� ���� ��Ÿ�� Dictionary
        private Dictionary<string, SlotData_Tool> toolSlotsDic = new Dictionary<string, SlotData_Tool>();

        // �ܺο��� �κ��丮 ���� ����� ���� �� �ֵ��� �����ϴ� ������Ƽ
        public IReadOnlyList<SlotData_Tool> ToolSlotsList => toolSlotsList;

        // ���� �κ��丮 �����Ͱ� ����Ǿ��� �� ȣ��
        public event Action OnToolInventoryChanged;
        // Ư�� ���� ������ �����Ͱ� ����Ǿ��� �� ȣ��
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
        /// ��Ÿ�� ����Ʈ ������� Dictionary �� �ٽ� ����
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
                    Debug.LogWarning("[InventoryManager_Tool] ToolId�� ��� �ִ� ������ �����մϴ�.");

                    toolSlotsList.RemoveAt(i);
                    continue;
                }

                if (toolSlotsDic.ContainsKey(slot.ToolId))
                {
                    Debug.LogWarning($"[InventoryManager_Tool] �ߺ� ToolId ������ �����մϴ�: {slot.ToolId}");

                    toolSlotsList.RemoveAt(i);
                    continue;
                }

                toolSlotsDic.Add(slot.ToolId, slot);

                // ���� ��ġ ��� �ý��� ����
                RefreshSlotGrowthData(slot);
            }
        }

        /// <summary>
        /// ���� ID�� �ش��ϴ� ���� ������ ��ȯ
        /// </summary>
        public ToolDataSO GetToolData(string toolId)
        {
            if (string.IsNullOrWhiteSpace(toolId)) return null;

            if (toolDatabase == null)
            {
                Debug.LogWarning("[InventoryManager_Tool] ToolDatabase�� ������� �ʾҽ��ϴ�.");
                return null;
            }

            return toolDatabase.GetToolData(toolId);
        }

        /// <summary>
        /// ���� ���� �߰�. ���� ȹ���̸� ������ �����, �ߺ� ȹ���̸� ���� ���� ���� ����
        /// </summary>
        public bool AddToolSlot(ToolDataSO toolData)
        {
            if (toolData == null)
            {
                Debug.LogWarning("[InventoryManager_Tool] �߰��� ToolDataSO�� �����ϴ�.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(toolData.Id))
            {
                Debug.LogWarning($"[InventoryManager_Tool] {toolData.DisplayName} �� Id �� ����ֽ��ϴ�.");
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

            NotifySlotChanged(newSlot);

            Debug.Log($"[InventoryManager_Tool] ���ο� ���� ȹ��: {toolData.DisplayName}");

            return true;
        }

        /// <summary>
        /// ���� ID�� ��Ÿ�� ���� ��ȸ
        /// </summary>
        public bool TryGetToolSlot(string toolId, out SlotData_Tool slot)
        {
            slot = null;

            if (string.IsNullOrWhiteSpace(toolId)) return false;

            return toolSlotsDic.TryGetValue(toolId, out slot);
        }

        /// <summary>
        /// ���� ���� ���� ������ �� ���� ��ȯ.
        /// �������� ���� ������ 0 ��ȯ
        /// </summary>
        public int GetToolCount(string toolId)
        {
            return TryGetToolSlot(toolId, out SlotData_Tool slot) ? slot.CurrentCount : 0;
        }

        /// <summary>
        /// �ش� ������ �� �� �̻� ȹ���ߴ��� Ȯ��.
        /// ���������� �� ���� �ر� ���η� ���.
        /// </summary>
        public bool IsToolUnlocked(string toolId)
        {
            return TryGetToolSlot(toolId, out _);
        }

        /// <summary>
        /// ������ ���� ������ �������� Ȯ��
        /// </summary>
        public bool CanLevelUpTool(string toolId)
        {
            if (!TryGetToolSlot(toolId, out SlotData_Tool slot)) return false;

            if (!slot.CanLevelUp()) return false;

            long coinCost = slot.GetLevelUpCoinCost();

            if (CoinWallet != null && CoinWallet.Balance < coinCost)
            {
                Debug.Log($"[InventoryManager_Tool] not enough coin. needed: {coinCost}, balance: {CoinWallet.Balance}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// ���� ������ �õ�
        /// </summary>
        public bool TryLevelUpTool(string toolId)
        {
            if (!TryGetToolSlot(toolId, out SlotData_Tool slot))
            {
                Debug.LogWarning($"[InventoryManager_Tool] �������� ���� �����Դϴ�: {toolId}");
                return false;
            }

            if (slot.IsMaxLevel)
            {
                Debug.Log($"[InventoryManager_Tool] �̹� �ִ� ������ �����Դϴ�: {toolId}");
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
            if (!slot.TryConsumeForLevelUp())
            {
                Debug.Log($"[InventoryManager_Tool] 재료 소모 실패: {toolId}");
                return false;
            }

            slot.ToolLevelUp();

            // 다음 레벨 요구치 갱신
            RefreshSlotGrowthData(slot);

            NotifySlotChanged(slot);

            Debug.Log($"[InventoryManager_Tool] ���� ������ ����: {toolId} / ���� ���� {slot.Level}");

            return true;
        }

        /// <summary>
        /// ������ ��ġ ���·� �����մϴ�.
        /// </summary>
        public bool TrySetTool(string toolId)
        {
            if (!TryGetToolSlot(toolId, out SlotData_Tool slot))
            {
                Debug.LogWarning($"[InventoryManager_Tool] �������� ���� �����Դϴ�: {toolId}");
                return false;
            }

            slot.SetPlaced(true);
            NotifySlotChanged(slot);

            return true;
        }

        /// <summary>
        /// ���� ��ġ�� �����մϴ�.
        /// </summary>
        public bool TryUnsetTool(string toolId)
        {
            if (!TryGetToolSlot(toolId, out SlotData_Tool slot))
            {
                Debug.LogWarning($"[InventoryManager_Tool] �������� ���� �����Դϴ�: {toolId}");
                return false;
            }

            slot.SetPlaced(false);
            slot.ClearAssignedAnimal();
            NotifySlotChanged(slot);

            return true;
        }

        /// <summary>
        /// ������ ������ ��ġ�մϴ�.
        /// </summary>
        public bool TryAssignAnimalToTool(string toolId, string animalId)
        {
            if (!TryGetToolSlot(toolId, out SlotData_Tool slot))
            {
                Debug.LogWarning($"[InventoryManager_Tool] �������� ���� �����Դϴ�: {toolId}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(animalId))
            {
                Debug.LogWarning("[InventoryManager_Tool] ��ġ�� AnimalId�� ��� �ֽ��ϴ�.");
                return false;
            }

            slot.SetAssignedAnimal(animalId);
            NotifySlotChanged(slot);

            return true;
        }

        /// <summary>
        /// ������ ��ġ�� ������ �����մϴ�.
        /// </summary>
        public bool TryRemoveAnimalFromTool(string toolId)
        {
            if (!TryGetToolSlot(toolId, out SlotData_Tool slot))
            {
                Debug.LogWarning($"[InventoryManager_Tool] �������� ���� �����Դϴ�: {toolId}");
                return false;
            }

            slot.ClearAssignedAnimal();
            NotifySlotChanged(slot);

            return true;
        }

        /// <summary>
        /// ���� ��Ÿ�� ���� �����͸� ����� ���� ������� ��ȯ�մϴ�.
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
        /// ���� �����͸� ������� ��Ÿ�� ���� �κ��丮�� �����մϴ�.
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
                    Debug.LogWarning($"[InventoryManager_Tool] ���� �����Ϳ� �ߺ� ID�� �ֽ��ϴ�: {saveData.tooldata.Id}");
                    continue;
                }

                SlotData_Tool runtimeSlot = new SlotData_Tool(
                    saveData.tooldata,
                    saveData.level,
                    saveData.currentCount,
                    saveData.currentSet,
                    saveData.currentAnimalSet,
                    saveData.currentAnimalId);

                // ���� ��ġ ��� �ý��� ����
                RefreshSlotGrowthData(runtimeSlot);

                toolSlotsList.Add(runtimeSlot);
                toolSlotsDic.Add(runtimeSlot.ToolId, runtimeSlot);
            }

            NotifyInventoryChanged();
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

            slot.ApplyGrowthData(requiredCount, slot.LevelUpCost, false);
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
        /// ���� �κ��丮 ���� ȣ��
        /// </summary>
        private void NotifyInventoryChanged()
        {
            OnToolInventoryChanged?.Invoke();
        }
    }
}
