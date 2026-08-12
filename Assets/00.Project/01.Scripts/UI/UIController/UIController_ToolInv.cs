using System.Collections.Generic;
using Manager;
using TaskTown.KDH;
using TMPro;
using UnityEngine;

namespace UI
{
    public class UIController_ToolInv : MonoBehaviour
    {
        [Header("도구 인벤토리")]
        private InventoryManager_Tool toolInventory;

        [SerializeField] private KAY.SlotUI_ToolInv toolSlotPrefab;
        [SerializeField] private Transform toolSlotContentRoot;

        [SerializeField] private TMP_Text totalCountText;
        [SerializeField] private TMP_Text toolSetText;

        private int totalconunt;
        private int currentToolsetconunt;
        private int maxToolsetconunt;

        [Header("도구 상세 페이지 (씬의 Tool_Inv_Page)")]
        [SerializeField] private UIController_ToolInvPage toolInvPageController;

        [Header("인스펙터 확인용 인벤토리 리스트")]
        [SerializeField] private List<KAY.SlotUI_ToolInv> slotMaplist = new List<KAY.SlotUI_ToolInv>();
        private readonly Dictionary<string, KAY.SlotUI_ToolInv> slotMap = new Dictionary<string, KAY.SlotUI_ToolInv>();

        //------------------26.08.05 KAY 추가 (마을 레벨 설정 연동)---------------------------------
        private VillageSystemManager subscribedVillageSystem;
        //-----------------------------------------------------------------------------

        private void Awake()
        {
            //-----------------26.08.05 KDH-------------------------------------
            // Instance를 우선합니다. FindFirstObjectByType은 리셋 직후 파괴 예정인 씬 복제본을 잡을 수 있습니다.
            TryResolveInventoryReference();
        }

        private void TryResolveInventoryReference()
        {
            if (toolInventory != null)
                return;

            toolInventory = InventoryManager_Tool.Instance;
            //-----------------------------------------------------------------
            if (toolInventory == null)
                toolInventory = FindFirstObjectByType<InventoryManager_Tool>();
        }

        private void OnEnable()
        {
            // Inv 패널/컨트롤러 활성화 시 상세 페이지는 항상 닫힌 상태로 시작 (Animal Inv와 동일)
            toolInvPageController?.CloseToolInvPage();

            //------------------26.08.05 KAY 추가 (마을 레벨 설정 연동)---------------------------------
            TrySubscribeVillageState();
            //-----------------------------------------------------------------------------

            if (!TryResolveInventory())
                return;

            toolInventory.OnToolInventoryChanged += SyncAllSlots;
            toolInventory.OnToolSlotChanged += HandleToolSlotChanged;

            // 컨트롤러는 상시 활성 매니저에 있으므로, Content가 켜져 있을 때만 즉시 동기화
            SyncAllSlots();
        }


        /// <summary>
        /// 도구 Inv 패널이 열릴 때 호출합니다.
        /// 상세 페이지를 닫은 뒤 슬롯 UI를 동기화합니다.
        /// </summary>
        public void NotifyPanelOpened()
        {
            toolInvPageController?.CloseToolInvPage();
            SyncAllSlots();
        }

        /// <summary>
        /// 도구 Inv 패널이 닫힐 때 호출합니다.
        /// 열려 있는 Tool_Inv_Page(상세)도 함께 닫습니다.
        /// </summary>
        public void NotifyPanelClosed()
        {
            toolInvPageController?.CloseToolInvPage();
        }

        private void OnDisable()
        {
            //------------------26.08.05 KAY 추가 (마을 레벨 설정 연동)---------------------------------
            UnsubscribeVillageState();
            //-----------------------------------------------------------------------------

            if (toolInventory == null)
                return;

            toolInventory.OnToolInventoryChanged -= SyncAllSlots;
            toolInventory.OnToolSlotChanged -= HandleToolSlotChanged;
        }

        //------------------26.08.05 KAY 추가 (마을 레벨 설정 연동)---------------------------------
        private void TrySubscribeVillageState()
        {
            if (VillageSystemManager.Instance == null)
                return;

            if (subscribedVillageSystem == VillageSystemManager.Instance)
                return;

            UnsubscribeVillageState();
            subscribedVillageSystem = VillageSystemManager.Instance;
            subscribedVillageSystem.OnVillageStateChanged += OnVillageStateChanged;
        }

        private void UnsubscribeVillageState()
        {
            if (subscribedVillageSystem == null)
                return;

            subscribedVillageSystem.OnVillageStateChanged -= OnVillageStateChanged;
            subscribedVillageSystem = null;
        }

        private void OnVillageStateChanged()
        {
            // 도구 상한 텍스트만 갱신 (슬롯 전체 재생성은 불필요)
            RefreshCountTexts();
        }
        //-----------------------------------------------------------------------------

        private void HandleToolSlotChanged(SlotData_Tool slotData)
        {
            RefreshSlot(slotData);

            if (CanUpdateView())
                RefreshCountTexts();
        }

        /// <summary>
        /// 인벤 보유 종류 수 / 동물 장착 도구 수·상한 텍스트를 갱신합니다.
        /// </summary>
        private void RefreshCountTexts()
        {
            totalconunt = CountOwnedTools();
            currentToolsetconunt = toolInventory != null ? toolInventory.GetActiveToolCount() : 0;
            maxToolsetconunt = toolInventory != null ? toolInventory.GetToolCapacity() : 0;

            if (totalCountText != null)
                totalCountText.text = "도구 수량: " + totalconunt.ToString();

            if (toolSetText != null)
                toolSetText.text = "주민에게 배치된 도구: " + currentToolsetconunt + "/" + maxToolsetconunt;
        }

        /// <summary>
        /// 인벤토리에 보유 중인 도구 종류(슬롯) 수를 반환합니다.
        /// </summary>
        private int CountOwnedTools()
        {
            if (toolInventory == null)
                return 0;

            IReadOnlyList<SlotData_Tool> toolSlots = toolInventory.ToolSlotsList;
            if (toolSlots == null)
                return 0;

            int count = 0;
            for (int i = 0; i < toolSlots.Count; i++)
            {
                SlotData_Tool slotData = toolSlots[i];
                if (slotData == null || string.IsNullOrEmpty(slotData.ToolId))
                    continue;

                count++;
            }

            return count;
        }

        private bool TryResolveInventory()
        {
            //-----------------26.08.05 KDH-------------------------
            ///Before
            //if (toolInventory == null)
            //    toolInventory = InventoryManager_Tool.Instance;

            ///After
            TryResolveInventoryReference();
            //----------------------------------------

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

            if (toolInvPageController == null)
            {
                Debug.LogWarning("[UIController_ToolInv] toolInvPageController 가 연결되지 않았습니다. 슬롯 클릭 시 상세 페이지가 열리지 않습니다.");
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
            int siblingIndex = 0;

            foreach (SlotData_Tool slotData in toolSlots)
            {
                if (slotData == null)
                    continue;

                string toolId = slotData.ToolId;

                if (string.IsNullOrEmpty(toolId))
                    continue;

                activeIds.Add(toolId);
                RefreshSlot(slotData);

                // 매니저 정렬 순서를 UI 형제 순서에 반영
                if (slotMap.TryGetValue(toolId, out KAY.SlotUI_ToolInv slotView) && slotView != null)
                    slotView.transform.SetSiblingIndex(siblingIndex++);
            }

            RemoveStaleSlots(activeIds);
            RefreshCountTexts();
        }

        private void RemoveStaleSlots(HashSet<string> activeIds)
        {
            var staleIds = new List<string>();

            foreach (KeyValuePair<string, KAY.SlotUI_ToolInv> pair in slotMap)
            {
                if (!activeIds.Contains(pair.Key))
                    staleIds.Add(pair.Key);
            }

            foreach (string staleId in staleIds)
            {
                if (!slotMap.TryGetValue(staleId, out KAY.SlotUI_ToolInv staleSlot))
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

            KAY.SlotUI_ToolInv createdSlot = Instantiate(toolSlotPrefab, toolSlotContentRoot);
            createdSlot.Initialize(slotData, toolInventory, toolInvPageController);

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

            if (!slotMap.TryGetValue(toolId, out KAY.SlotUI_ToolInv slotView))
            {
                AddToolSlot(slotData);
                return;
            }

            slotView.Refresh(slotData);
        }
    }
}