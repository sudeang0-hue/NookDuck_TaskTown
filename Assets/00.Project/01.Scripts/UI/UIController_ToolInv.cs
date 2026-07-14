using System.Collections.Generic;
using Test;
using UnityEngine;


namespace UI
{

    public class UIController_ToolInv : MonoBehaviour
    {

        [Header("도구 인벤토리")]
        [Tooltip("임시 클래스이므로 실제 인벤토리 시스템이 완성되면 클래스 변경")]
        [SerializeField] TestInventory_Tool toolInventory;

        [SerializeField] private SlotUI_ToolInv toolSlotPrefab;   // 슬롯 프리팹
        [SerializeField] private Transform toolSlotContentRoot;  // 해당 슬롯을 추가하는 위치


        [Header("인스펙터 확인용 인벤토리 리스트")]
        [SerializeField] private List<SlotUI_ToolInv> slotMaplist = new List<SlotUI_ToolInv>();
        private readonly Dictionary<string, SlotUI_ToolInv> slotMap = new Dictionary<string, SlotUI_ToolInv>();


        private void Start()
        {
            if (toolSlotPrefab == null)
            {
                Debug.LogWarning("[UIController_ToolInv] toolSlotPrefab 이 없습니다.");
                return;
            }

            if (toolSlotContentRoot == null)
            {
                Debug.LogWarning("[UIController_ToolInv] toolSlotContentRoot 이 없습니다.");
                return;
            }

            if (toolInventory == null)
            {
                Debug.LogWarning("[UIController_ToolInv] toolInventory 가 연결되지 않았습니다.");
                return;
            }
        }

        private void OnEnable()
        {
            if (toolInventory == null)
                return;

            toolInventory.OnToolAdded += AddToolSlot;
            toolInventory.OnToolChanged += RefreshSlot;

            InitializeSlots();
        }

        private void OnDisable()
        {
            if (toolInventory == null)
                return;

            toolInventory.OnToolAdded -= AddToolSlot;
            toolInventory.OnToolChanged -= RefreshSlot;

        }

        /// <summary>
        /// 테스트 인벤토리에 이미 존재하는 도구을 기준으로
        /// 보유 도구 UI 슬롯을 생성합니다.
        /// </summary>
        private void InitializeSlots()
        {
            IReadOnlyList<SlotData_Tool> toolSlots = toolInventory.ToolSlots;

            if (toolSlots == null)
                return;

            for (int i = 0; i < toolSlots.Count; i++)
            {
                SlotData_Tool slotData = toolSlots[i];

                if (slotData == null)
                    continue;

                AddToolSlot(slotData.ToolId);
            }

        }



        public void AddToolSlot(string toolId)
        {
            if (string.IsNullOrEmpty(toolId))
                return;

            if (slotMap.ContainsKey(toolId))
            {
                RefreshSlot(toolId);
                return;
            }

            if (toolInventory == null)
                return;

            if (!toolInventory.TryGetToolSlot(toolId, out SlotData_Tool slotData))
            {
                Debug.LogWarning($"[UIController_ToolInv] 도구 슬롯 데이터를 찾지 못했습니다: {toolId}");
                return;
            }

            if (toolSlotPrefab == null || toolSlotContentRoot == null)
                return;


            SlotUI_ToolInv createdSlot = Instantiate(toolSlotPrefab, toolSlotContentRoot);

            createdSlot.Initialize(slotData);

            slotMap.Add(toolId, createdSlot);
            slotMaplist.Add(createdSlot);
        }


        /// <summary>
        /// 슬롯 갱신. 중복 획득 후 갱신
        /// </summary>
        private void RefreshSlot(string toolId)
        {
            if (string.IsNullOrEmpty(toolId))
                return;

            if (!slotMap.TryGetValue(toolId, out SlotUI_ToolInv slotView))
            {
                AddToolSlot(toolId);
                return;
            }

            if (toolInventory == null)
                return;

            if (!toolInventory.TryGetToolSlot(toolId, out SlotData_Tool slotData))
            {
                Debug.LogWarning($"[UIController_ToolInv] 갱신할 도구 데이터를 찾지 못했습니다: {toolId}");

                return;
            }

            slotView.Refresh(slotData);
        }


    }
}