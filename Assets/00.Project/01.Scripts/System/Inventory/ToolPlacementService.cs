using System.Collections.Generic;
using UnityEngine;


//���� �� ����
// ���� ��ġ
//ToolPlacementService.Instance.PlaceTool("toolId");

// ��ġ�� ������ ���� �Ҵ�
//ToolPlacementService.Instance.AssignAnimal("toolId", "animalId");

// ��Ȳ
//bool placed = ToolPlacementService.Instance.IsToolPlaced("toolId");
//int count = ToolPlacementService.Instance.PlacedToolCount;

// ����
//ToolPlacementService.Instance.UnassignAnimal("toolId");
//ToolPlacementService.Instance.UnplaceTool("toolId");



namespace TaskTown.KDH
{
    /// <summary>
    /// ���� ��ġ / ���� �Ҵ縸 ����ϴ� ���� ������.
    /// InventoryManager_Tool / Animal �� ���� ������ ���� ���⸸ ȣ���ϼ���.
    /// </summary>
    public class ToolPlacementService : MonoBehaviour
    {
        public static ToolPlacementService Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }



        // ---------- ��ȸ ----------
        // #18: ���� ��ġ ��ѵ� ��� ���� ��ġ�� ���� ���Ϸ� ��ȭ (InventoryManager_Tool.GetToolCapacity)
        public int MaxPlacedToolCount => InventoryManager_Tool.Instance != null ? InventoryManager_Tool.Instance.GetToolCapacity() : 0;
        public int PlacedToolCount => GetPlacedTools().Count;
        public bool CanPlaceMoreTools() => PlacedToolCount < MaxPlacedToolCount;
        public bool IsToolPlaced(string toolId) => TryGetTool(toolId, out SlotData_Tool slot) && slot.CurrentSet;

        public bool TryGetAssignedAnimalId(string toolId, out string animalId)
        {
            animalId = string.Empty;

            if (!TryGetTool(toolId, out SlotData_Tool slot)) return false;
            if (!slot.CurrentAnimalSet) return false;

            animalId = slot.CurrentAnimalId;

            return true;
        }

        public List<SlotData_Tool> GetPlacedTools()
        {
            List<SlotData_Tool> result = new List<SlotData_Tool>();

            if (InventoryManager_Tool.Instance == null) return result;

            foreach (SlotData_Tool slot in InventoryManager_Tool.Instance.ToolSlotsList)
            {
                if (slot != null && slot.CurrentSet) result.Add(slot);
            }

            return result;
        }



        // ---------- ��ġ ----------
        public bool PlaceTool(string toolId)
        {
            if (!CanPlaceMoreTools()) return false;
            if (IsToolPlaced(toolId)) return true; // �̹� ��ġ��

            return InventoryManager_Tool.Instance != null && InventoryManager_Tool.Instance.TrySetTool(toolId);
        }

        public bool UnplaceTool(string toolId)
        {
            return InventoryManager_Tool.Instance != null && InventoryManager_Tool.Instance.TryUnsetTool(toolId);
        }



        // ---------- ���� �Ҵ� (��ġ�� ������, ���� 1 : ���� 1) ----------
        public bool AssignAnimal(string toolId, string animalId)
        {
            if (InventoryManager_Tool.Instance == null) return false;
            if (InventoryManager_Animal.Instance == null) return false;

            // 1) ������ ��ġ�Ǿ� �־�� ��
            if (!IsToolPlaced(toolId)) return false;

            // 2) ���� ���� Ȯ��
            if (!InventoryManager_Animal.Instance.TryGetAnimalSlot(animalId, out _)) return false;

            // 3) ���� ������ �ٸ� ������ ������ ����
            ClearAnimalFromAllTools(animalId);

            // 4) �ش� ������ �Ҵ�
            return InventoryManager_Tool.Instance.TryAssignAnimalToTool(toolId, animalId);
        }

        public bool UnassignAnimal(string toolId)
        {
            return InventoryManager_Tool.Instance != null && InventoryManager_Tool.Instance.TryRemoveAnimalFromTool(toolId);
        }



        // ---------- ���� ----------
        private bool TryGetTool(string toolId, out SlotData_Tool slot)
        {
            slot = null;
            return InventoryManager_Tool.Instance != null && InventoryManager_Tool.Instance.TryGetToolSlot(toolId, out slot);
        }

        private void ClearAnimalFromAllTools(string animalId)
        {
            if (InventoryManager_Tool.Instance == null) return;

            foreach (SlotData_Tool slot in InventoryManager_Tool.Instance.ToolSlotsList)
            {
                if (slot == null || !slot.CurrentAnimalSet) continue;
                if (slot.CurrentAnimalId != animalId) continue;

                InventoryManager_Tool.Instance.TryRemoveAnimalFromTool(slot.ToolId);
            }
        }


    }
}