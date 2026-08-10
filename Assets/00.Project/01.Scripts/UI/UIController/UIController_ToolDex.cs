using System.Collections.Generic;
using TaskTown.KDH;
using TMPro;
using Tool.Data;
using UnityEngine;

namespace UI
{
    public class UIController_ToolDex : MonoBehaviour
    {
        [Header("도구 인벤토리")]
        [SerializeField] private InventoryManager_Tool toolInventory;

        [Header("도구 데이터베이스")]
        [SerializeField] private ToolDatabase toolDatabase;

        [Header("도감 슬롯")]
        [SerializeField] private SlotUI_ToolDex toolSlotPrefab;
        [SerializeField] private Transform toolSlotContentRoot;
        [SerializeField] private TMP_Text toolcountMessage;
        [SerializeField, TextArea(2, 4)]
        private string countMessage = "발견한 도구의 수: ";

        [Header("도구 상세 페이지")]
        [SerializeField] private UIController_ToolDexPage toolPageController;

        [Header("Runtime 확인용")]
        [SerializeField]
        private List<SlotUI_ToolDex> createdSlots = new List<SlotUI_ToolDex>();
        private readonly Dictionary<string, SlotUI_ToolDex> slotMap = new Dictionary<string, SlotUI_ToolDex>();

        private bool isInitialized;

        private void Awake()
        {
            //-----------------26.08.05 KDH-------------------------
            toolInventory = InventoryManager_Tool.Instance;
            if (toolInventory == null)
                toolInventory = FindFirstObjectByType<InventoryManager_Tool>();
            //----------------------------------------

            InitializeToolDex();
        }

        private void Start()
        {
            RefreshInventory();
        }

        private void OnEnable()
        {
            // 도감 패널이 열릴 때마다 상세 페이지는 항상 닫힌 상태로 시작
            toolPageController?.CloseToolDexPage();

            SubscribeEvents();

            // 해금 상태 갱신은 Start / NotifyPanelOpened에서만 수행합니다.
            // OnEnable은 DexRecordManager.Awake보다 먼저 호출될 수 있어 Instance null Warning이 납니다.
        }

        /// <summary>
        /// 도감 패널이 열릴 때 호출합니다.
        /// 상세 페이지를 닫힌 상태로 맞추고, 해금 상태를 최신으로 갱신합니다.
        /// </summary>
        public void NotifyPanelOpened()
        {
            toolPageController?.CloseToolDexPage();

            if (isInitialized)
                RefreshInventory();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        /// <summary>
        /// ToolDatabase에 등록된 전체 도구를 기준으로 도감 슬롯을 생성.
        /// 잠금 상태의 도감 슬롯으로 생성합니다.
        /// </summary>
        public void InitializeToolDex()
        {
            if (isInitialized)
                return;

            if (!ValidateReferences())
                return;

            CreateToolSlots();

            isInitialized = true;
        }

        /// <summary>
        /// ToolDatabase의 도구 수만큼 슬롯을 생성하고
        /// 각 슬롯에 ToolDataSO를 전달합니다.
        /// </summary>
        private void CreateToolSlots()
        {
            IReadOnlyList<ToolDataSO> tools = toolDatabase.Tools;

            if (tools == null || tools.Count == 0)
            {
                Debug.LogWarning("[UIController_ToolDex] ToolDatabase에 등록된 도구가 없습니다.");
                return;
            }

            for (int i = 0; i < tools.Count; i++)
            {
                ToolDataSO toolData = tools[i];

                if (toolData == null)
                {
                    Debug.LogWarning($"[UIController_ToolDex] ToolDatabase의 {i}번째 데이터가 비어 있습니다.");
                    continue;
                }

                string toolId = toolData.Id;

                if (string.IsNullOrEmpty(toolId))
                {
                    Debug.LogWarning($"[UIController_ToolDex] {i}번째 도구 ID가 비어 있습니다.");
                    continue;
                }

                if (slotMap.ContainsKey(toolId))
                {
                    Debug.LogWarning($"[UIController_ToolDex] 중복된 도구 ID입니다: {toolId}");
                    continue;
                }

                SlotUI_ToolDex slot = Instantiate(toolSlotPrefab, toolSlotContentRoot);

                slot.Initialize(toolData, toolPageController, false);

                slotMap.Add(toolId, slot);
                createdSlots.Add(slot);
            }
        }

        /// <summary>
        /// 도감 슬롯의 해금 상태를 갱신합니다. "이미 본 적 있음" 기록은 DexRecordManager가
        /// 인벤토리와 별개로 영구 저장하므로(난이도 리셋 이후에도 유지), 현재 인벤토리 보유
        /// 여부가 아니라 그 기록을 기준으로 판단합니다.
        /// </summary>
        private void RefreshInventory()
        {
            if (DexRecordManager.Instance == null)
            {
                Debug.LogWarning("[UIController_ToolDex] DexRecordManager.Instance가 없습니다.");
                RefreshCountMessage();
                return;
            }

            foreach (KeyValuePair<string, SlotUI_ToolDex> pair in slotMap)
            {
                bool isUnlocked = DexRecordManager.Instance.IsDiscovered(pair.Key);
                pair.Value.SetUnlocked(isUnlocked);
            }

            RefreshCountMessage();
        }

        // ----------------08.08.KAY (도구 도감 해금 디버그 갱신)------------------
        /// <summary>
        /// 디버그용: 도감 해금 상태를 DexRecordManager 기준으로 다시 그립니다.
        /// </summary>
        public void DebugRefreshUnlockStates()
        {
            RefreshInventory();
        }
        // ---------------------------------------------------------

        /// <summary>
        /// 도구 획득 이벤트로 전달된 ID의 도감 슬롯 하나만 갱신합니다.
        /// </summary>
        public void RefreshSlot(SlotData_Tool slotData)
        {
            if (string.IsNullOrEmpty(slotData.ToolId))
                return;

            if (!slotMap.TryGetValue(slotData.ToolId, out SlotUI_ToolDex slot))
            {
                Debug.LogWarning($"[UIController_ToolDex] 도감 슬롯을 찾지 못했습니다: {slotData.ToolId}");
                return;
            }

            slot.SetUnlocked(true);

            // 최초 획득 시 MarkDiscovered 구독 순서와 무관하게 카운트가 맞도록 이번 ID를 포함합니다.
            RefreshCountMessage(slotData.ToolId);
        }

        /// <summary>
        /// toolcountMessage = countMessage + 획득한 적 있는 수 + "/" + ToolDatabase 등록 수
        /// </summary>
        /// <param name="treatAsDiscoveredId">이번 프레임에 막 획득해 Discovered로 칠할 ID(이벤트 순서 보정용)</param>
        private void RefreshCountMessage(string treatAsDiscoveredId = null)
        {
            if (toolcountMessage == null)
                return;

            int totalCount = 0;
            int discoveredCount = 0;

            IReadOnlyList<ToolDataSO> tools = toolDatabase != null ? toolDatabase.Tools : null;
            if (tools != null)
            {
                for (int i = 0; i < tools.Count; i++)
                {
                    ToolDataSO tool = tools[i];
                    if (tool == null || string.IsNullOrEmpty(tool.Id))
                        continue;

                    totalCount++;

                    bool discovered =
                        (DexRecordManager.Instance != null && DexRecordManager.Instance.IsDiscovered(tool.Id))
                        || tool.Id == treatAsDiscoveredId;

                    if (discovered)
                        discoveredCount++;
                }
            }

            toolcountMessage.text = countMessage + discoveredCount + "/" + totalCount;
        }

        /// <summary>
        /// 인벤토리의 도구 획득 이벤트 구독
        /// </summary>
        private void SubscribeEvents()
        {
            if (toolInventory == null)
                return;

            toolInventory.OnToolSlotChanged += RefreshSlot;
        }

        /// <summary>
        /// 도구 획득 이벤트 구독 해제
        /// </summary>
        private void UnsubscribeEvents()
        {
            if (toolInventory == null)
                return;

            toolInventory.OnToolSlotChanged -= RefreshSlot;
        }

        /// <summary>
        /// 필수 Inspector 참조를 검사합니다.
        /// </summary>
        private bool ValidateReferences()
        {
            if (toolDatabase == null)
            {
                Debug.LogWarning("[UIController_ToolDex] ToolDatabase가 연결되지 않았습니다.");
                return false;
            }

            if (toolInventory == null)
            {
                Debug.LogWarning("[UIController_ToolDex] ToolInventory 가 연결되지 않았습니다.");
                return false;
            }

            if (toolSlotPrefab == null)
            {
                Debug.LogWarning("[UIController_ToolDex] 도구 도감 슬롯 Prefab이 연결되지 않았습니다.");
                return false;
            }

            if (toolSlotContentRoot == null)
            {
                Debug.LogWarning("[UIController_ToolDex] 도구 슬롯을 생성할 Content Root가 연결되지 않았습니다.");
                return false;
            }

            if (toolPageController == null)
            {
                Debug.LogWarning("[UIController_ToolDex] 도구 상세 페이지 UIController_ToolDexPage가 연결되지 않았습니다.");
                return false;
            }

            return true;
        }
    }
}
