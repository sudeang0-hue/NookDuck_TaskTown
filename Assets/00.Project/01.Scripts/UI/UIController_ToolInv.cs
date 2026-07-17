using System.Collections.Generic;
using TaskTown.KDH;
using UnityEngine;

namespace UI
{
    public class UIController_ToolInv : MonoBehaviour
    {
        [Header("도구 인벤토리")]
        [SerializeField] private InventoryManager_Tool toolInventory;

        [SerializeField] private SlotUI_ToolInv toolSlotPrefab;
        [SerializeField] private Transform toolSlotContentRoot;

        [Header("인스펙터 확인용 인벤토리 리스트")]
        [SerializeField] private List<SlotUI_ToolInv> slotMaplist = new List<SlotUI_ToolInv>();
        private readonly Dictionary<string, SlotUI_ToolInv> slotMap = new Dictionary<string, SlotUI_ToolInv>();

        private void Awake()
        {
            if (toolInventory == null)
                toolInventory = InventoryManager_Tool.Instance;
        }

        private void OnEnable()
        {
            if (!TryResolveInventory())
                return;

            toolInventory.OnToolInventoryChanged += SyncAllSlots;
            toolInventory.OnToolSlotChanged += RefreshSlot;

            // 컨트롤러는 상시 활성 매니저에 있으므로, Content가 켜져 있을 때만 즉시 동기화
            SyncAllSlots();
        }

        private void OnDisable()
        {
            if (toolInventory == null)
                return;

            toolInventory.OnToolInventoryChanged -= SyncAllSlots;
            toolInventory.OnToolSlotChanged -= RefreshSlot;
        }

        private bool TryResolveInventory()
        {
            if (toolInventory == null)
                toolInventory = InventoryManager_Tool.Instance;

            if (toolInventory == null)
            {
                Debug.LogWarning("[UIController_ToolInv] toolInventory 가 연결되지 않았습니다.");
                return false;
            }

            if (toolSlotPrefab == null)
            {
                Debug.LogWarning("[UIController_ToolInv] toolSlotPrefab 이 없습니다.");
                return false;
            }

            if (toolSlotContentRoot == null)
            {
                Debug.LogWarning("[UIController_ToolInv] toolSlotContentRoot 이 없습니다.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Inv 패널 Content가 계층상 활성인지 확인합니다.
        /// 패널이 꺼진 상태에서 슬롯을 만들면 오픈 시 반영이 누락될 수 있습니다.
        /// </summary>
        private bool CanUpdateView()
        {
            return toolSlotContentRoot != null &&
                   toolSlotContentRoot.gameObject.activeInHierarchy;
        }

        /// <summary>
        /// 인벤토리 전체와 슬롯 UI를 동기화합니다.
        /// Inv 패널을 오픈할 때마다 호출해 현재 보유 데이터를 반영합니다.
        /// </summary>
        public void SyncAllSlots()
        {
            if (!TryResolveInventory())
                return;

            if (!CanUpdateView())
                return;

            IReadOnlyList<SlotData_Tool> toolSlots = toolInventory.ToolSlotsList;

            if (toolSlots == null)
                return;

            var activeIds = new HashSet<string>();

            foreach (SlotData_Tool slotData in toolSlots)
            {
                if (slotData == null)
                    continue;

                string toolId = slotData.ToolId;

                if (string.IsNullOrEmpty(toolId))
                    continue;

                activeIds.Add(toolId);
                RefreshSlot(slotData);
            }

            RemoveStaleSlots(activeIds);
        }

        private void RemoveStaleSlots(HashSet<string> activeIds)
        {
            var staleIds = new List<string>();

            foreach (KeyValuePair<string, SlotUI_ToolInv> pair in slotMap)
            {
                if (!activeIds.Contains(pair.Key))
                    staleIds.Add(pair.Key);
            }

            foreach (string staleId in staleIds)
            {
                if (!slotMap.TryGetValue(staleId, out SlotUI_ToolInv staleSlot))
                    continue;

                slotMaplist.Remove(staleSlot);
                slotMap.Remove(staleId);

                if (staleSlot != null)
                    Destroy(staleSlot.gameObject);
            }
        }

        public void AddToolSlot(SlotData_Tool slotData)
        {
            if (slotData == null)
                return;

            string toolId = slotData.ToolId;

            if (string.IsNullOrEmpty(toolId))
                return;

            if (slotMap.ContainsKey(toolId))
                return;

            SlotUI_ToolInv createdSlot = Instantiate(toolSlotPrefab, toolSlotContentRoot);
            createdSlot.Initialize(slotData, toolInventory);

            slotMap.Add(toolId, createdSlot);
            slotMaplist.Add(createdSlot);
        }

        /// <summary>
        /// 슬롯 갱신. 최초 획득 시 생성, 중복 획득 시 수량 갱신
        /// </summary>
        private void RefreshSlot(SlotData_Tool slotData)
        {
            if (slotData == null)
                return;

            if (!CanUpdateView())
                return;

            string toolId = slotData.ToolId;

            if (string.IsNullOrEmpty(toolId))
                return;

            if (!slotMap.TryGetValue(toolId, out SlotUI_ToolInv slotView))
            {
                AddToolSlot(slotData);
                return;
            }

            slotView.Refresh(slotData);
        }
    }
}