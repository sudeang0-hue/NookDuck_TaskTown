using System.Collections.Generic;
using UnityEngine;


//쓰는 법 예시
// 도구 배치
//ToolPlacementService.Instance.PlaceTool("toolId");

// 배치된 도구에 동물 할당
//ToolPlacementService.Instance.AssignAnimal("toolId", "animalId");

// 현황
//bool placed = ToolPlacementService.Instance.IsToolPlaced("toolId");
//int count = ToolPlacementService.Instance.PlacedToolCount;

// 해제
//ToolPlacementService.Instance.UnassignAnimal("toolId");
//ToolPlacementService.Instance.UnplaceTool("toolId");



namespace TaskTown.KDH
{
    /// <summary>
    /// 도구 배치 / 동물 할당만 담당하는 얇은 진입점.
    /// InventoryManager_Tool / Animal 을 직접 만지지 말고 여기만 호출하세요.
    /// </summary>
    public class ToolPlacementService : MonoBehaviour
    {
        public static ToolPlacementService Instance { get; private set; }

        [SerializeField] private int maxPlacedToolCount = 3; // 나중에 업그레이드로 증가

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }



        // ---------- 조회 ----------
        public int MaxPlacedToolCount => maxPlacedToolCount;
        public int PlacedToolCount => GetPlacedTools().Count;
        public bool CanPlaceMoreTools() => PlacedToolCount < maxPlacedToolCount;
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



        // ---------- 배치 ----------
        public bool PlaceTool(string toolId)
        {
            if (!CanPlaceMoreTools()) return false;
            if (IsToolPlaced(toolId)) return true; // 이미 배치됨

            return InventoryManager_Tool.Instance != null && InventoryManager_Tool.Instance.TrySetTool(toolId);
        }

        public bool UnplaceTool(string toolId)
        {
            return InventoryManager_Tool.Instance != null && InventoryManager_Tool.Instance.TryUnsetTool(toolId);
        }



        // ---------- 동물 할당 (배치된 도구만, 도구 1 : 동물 1) ----------
        public bool AssignAnimal(string toolId, string animalId)
        {
            if (InventoryManager_Tool.Instance == null) return false;
            if (InventoryManager_Animal.Instance == null) return false;

            // 1) 도구가 배치되어 있어야 함
            if (!IsToolPlaced(toolId)) return false;

            // 2) 동물 보유 확인
            if (!InventoryManager_Animal.Instance.TryGetAnimalSlot(animalId, out _)) return false;

            // 3) 같은 동물이 다른 도구에 있으면 해제
            ClearAnimalFromAllTools(animalId);

            // 4) 해당 도구에 할당
            return InventoryManager_Tool.Instance.TryAssignAnimalToTool(toolId, animalId);
        }

        public bool UnassignAnimal(string toolId)
        {
            return InventoryManager_Tool.Instance != null && InventoryManager_Tool.Instance.TryRemoveAnimalFromTool(toolId);
        }



        // ---------- 내부 ----------
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



        // 업그레이드로 배치 한도 올릴 때
        public void SetMaxPlacedToolCount(int count)
        {
            maxPlacedToolCount = Mathf.Max(1, count);
        }

        //----------------------26.07.27 KDH-------------------------------
        public void IncreaseMaxPlacedToolCount(int amount)
        {
            maxPlacedToolCount = Mathf.Max(1, maxPlacedToolCount + amount);
        }
        //------------------------------------------------------------------
    }
}