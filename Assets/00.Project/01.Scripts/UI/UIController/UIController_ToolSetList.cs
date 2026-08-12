/* ToolSettingList_root 패널 제어
 * - Open/Close: Set Tool / Change Tool에서 "목록 표시"만 담당 (animalId를 pending으로 보관)
 * - 목록 필터: CurrentAnimalSet인 도구 제외
 *   (미장착 동물 Set Tool → 타인 장착 도구 숨김 / 장착 동물 Change Tool → 본인 도구 숨김)
 * - 장착 불가 안내: 보유 0 → hasZeroTool / 상한 도달(신규 장착) → maxToolCount (슬롯 미생성)
 * - 실제 장착: tool_set_slot의 cover_btn 클릭 → HandleToolSlotSelected → TryAssignAnimalToTool
 * - 패널 바깥 클릭 시 패널 닫기
 */

using System;
using System.Collections.Generic;
using TaskTown.KDH;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class UIController_ToolSetList : MonoBehaviour
{
    [SerializeField] private InventoryManager_Tool toolInventory;

    [SerializeField] private SlotUI_ToolSet toolSetPrefab;
    [SerializeField] private Transform toolSetSlotContentRoot;

    [Tooltip("도구를 장착할 수 없을때 표시할 텍스트")]
    [SerializeField] private TMP_Text noAvaliableToolText;

    [SerializeField, TextArea(2, 4)]
    private string hasZeroTool = "갖고 있는 도구가 없어요\n뽑기에서 도구를 얻어보세요";
    [SerializeField, TextArea(2, 4)]
    private string maxToolCount = "더이상 도구를 배치할 수 없어요\n마을을 성장시켜서 제한을 늘려보세요";

    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();
    private readonly List<SlotUI_ToolSet> spawnedSlots = new List<SlotUI_ToolSet>();

    // 현재 이 패널에서 도구를 골라 장착시킬 대상 동물 ID
    private string pendingAnimalId;
    // 장착 성공 후(패널 닫기 전) 호출할 콜백 - 호출한 쪽(UIController_AnimalInvPage)의 화면 갱신용
    private Action onAssigned;

    public bool IsOpen => gameObject.activeSelf;

    private void Awake()
    {
        if (toolInventory == null)
            toolInventory = InventoryManager_Tool.Instance;

        if (toolSetPrefab == null)
            Debug.LogWarning("[UIController_ToolSetList] toolSetPrefab이 할당되지 않았습니다.", this);

        if (toolSetSlotContentRoot == null)
            Debug.LogWarning("[UIController_ToolSetList] toolSetSlotContentRoot이 연결되지 않았습니다.", this);
    }

    private void OnEnable()
    {
        PopulateSlots();
    }

    private void Update()
    {
        if (!IsOpen)
            return;

        if (!WasPrimaryPressThisFrame())
            return;

        if (IsPointerInsidePanel())
            return;

        // 패널 바깥 클릭: 별도 배치 의사가 없는 것으로 판단하고 닫습니다.
        Close();
    }

    /// <summary>
    /// 목록만 엽니다. 장착은 하지 않으며, animalId를 pending으로 보관합니다.
    /// </summary>
    /// <param name="animalId">cover_btn 선택 시 장착시킬 동물 ID</param>
    /// <param name="onAssignedCallback">장착 성공 시(패널 닫기 전) 호출할 콜백</param>
    public void Open(string animalId = null, Action onAssignedCallback = null)
    {
        pendingAnimalId = animalId;
        onAssigned = onAssignedCallback;

        // 다른 패널(동물 상세페이지 등) 위에 겹쳐 보이도록 항상 맨 앞으로 가져옵니다.
        transform.SetAsLastSibling();

        if (gameObject.activeSelf)
        {
            PopulateSlots();
            return;
        }

        gameObject.SetActive(true);
    }

    /// <summary>
    /// ToolSettingList_root 패널을 닫습니다.
    /// </summary>
    public void Close()
    {
        pendingAnimalId = null;
        onAssigned = null;

        if (!gameObject.activeSelf)
            return;

        gameObject.SetActive(false);
    }

    /// <summary>
    /// 장착 가능한 도구만 슬롯으로 채웁니다. 열 때마다 최신 목록으로 다시 생성합니다.
    /// - Set Tool(미장착 동물): 다른 동물이 장착한 도구는 제외
    /// - Change Tool(장착 동물): 자신이 장착 중인 도구도 제외
    /// → CurrentAnimalSet == true 인 도구는 목록에 넣지 않습니다.
    /// - 보유 도구 0 / 상한 도달(신규 장착)이면 슬롯을 만들지 않고 noAvaliableToolText를 표시합니다.
    /// </summary>
    private void PopulateSlots()
    {
        if (toolInventory == null)
            toolInventory = InventoryManager_Tool.Instance;

        if (toolInventory == null || toolSetPrefab == null || toolSetSlotContentRoot == null)
            return;

        ClearSlots();
        HideNoAvailableToolText();

        IReadOnlyList<SlotData_Tool> toolSlots = toolInventory.ToolSlotsList;
        int ownedToolCount = toolSlots != null ? toolSlots.Count : 0;

        // 보유 도구가 없으면 슬롯 없이 안내 문구만 표시
        if (ownedToolCount == 0)
        {
            ShowNoAvailableToolText(hasZeroTool);
            return;
        }

        int activeToolCount = toolInventory.GetActiveToolCount();
        int toolCapacity = toolInventory.GetToolCapacity();

        // 상한 도달 + 신규 장착(미장착 동물)이면 슬롯 생성 없이 안내만 표시
        // Change Tool(이미 장착된 동물)은 교체 가능하므로 목록을 계속 채웁니다.
        if (activeToolCount >= toolCapacity && !IsPendingAnimalAlreadyEquipped())
        {
            ShowNoAvailableToolText(maxToolCount);
            return;
        }

        int createdCount = 0;

        for (int i = 0; i < toolSlots.Count; i++)
        {
            SlotData_Tool slotData = toolSlots[i];

            if (slotData == null || string.IsNullOrEmpty(slotData.ToolId) || slotData.ToolData == null)
                continue;

            // 이미 장착된 도구는 Set Tool / Change Tool 목록에서 제외
            if (!IsToolAvailableForPendingAnimal(slotData))
                continue;

            SlotUI_ToolSet createdSlot = Instantiate(toolSetPrefab, toolSetSlotContentRoot);
            createdSlot.Initialize(slotData, HandleToolSlotSelected);
            spawnedSlots.Add(createdSlot);
            createdCount++;
        }

        // 선택 가능한 도구가 없으면 안내 텍스트만 표시
        if (createdCount == 0)
            ShowNoAvailableToolText(null);
    }

    /// <summary>
    /// pending 동물이 이미 어떤 도구에 장착되어 있는지 여부 (Change Tool 경로)
    /// </summary>
    private bool IsPendingAnimalAlreadyEquipped()
    {
        if (string.IsNullOrEmpty(pendingAnimalId) || toolInventory == null)
            return false;

        IReadOnlyList<SlotData_Tool> toolSlots = toolInventory.ToolSlotsList;
        if (toolSlots == null)
            return false;

        for (int i = 0; i < toolSlots.Count; i++)
        {
            SlotData_Tool toolSlot = toolSlots[i];
            if (toolSlot != null &&
                toolSlot.CurrentAnimalSet &&
                toolSlot.CurrentAnimalId == pendingAnimalId)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 장착 불가 안내 텍스트를 표시합니다. message가 null이면 기존 문구를 유지합니다.
    /// </summary>
    private void ShowNoAvailableToolText(string message)
    {
        if (noAvaliableToolText == null)
            return;

        if (message != null)
            noAvaliableToolText.text = message;

        noAvaliableToolText.gameObject.SetActive(true);
    }

    private void HideNoAvailableToolText()
    {
        if (noAvaliableToolText != null)
            noAvaliableToolText.gameObject.SetActive(false);
    }

    /// <summary>
    /// pending 동물 기준으로 목록에 넣을 수 있는 도구인지 판정합니다.
    /// 미장착 도구만 true. (타인 장착·본인 장착 모두 false)
    /// </summary>
    private static bool IsToolAvailableForPendingAnimal(SlotData_Tool slotData)
    {
        if (slotData == null)
            return false;

        // CurrentAnimalSet이면 누구든(자신 포함) 이미 사용 중 → 목록에서 제외
        return !slotData.CurrentAnimalSet;
    }

    /// <summary>
    /// 이전에 생성한 SlotUI_ToolSet와 Content 하위의 잔여 ToolSet 슬롯을 제거합니다.
    /// (에디터에 남아 있던 슬롯이 빈 인벤에서도 보이지 않도록)
    /// </summary>
    private void ClearSlots()
    {
        for (int i = 0; i < spawnedSlots.Count; i++)
        {
            if (spawnedSlots[i] != null)
                Destroy(spawnedSlots[i].gameObject);
        }

        spawnedSlots.Clear();

        if (toolSetSlotContentRoot == null)
            return;

        for (int i = toolSetSlotContentRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = toolSetSlotContentRoot.GetChild(i);
            if (child == null)
                continue;

            // Content에 다른 UI(안내 텍스트 등)가 있어도 ToolSet 슬롯만 제거
            if (child.GetComponent<SlotUI_ToolSet>() != null)
                Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// 도구 슬롯 클릭 시, 대기 중인 동물을 그 도구에 장착하고 패널을 닫습니다.
    /// 같은 동물이 다른 도구에 이미 장착되어 있으면 먼저 해제합니다.
    /// </summary>
    private void HandleToolSlotSelected(SlotData_Tool slotData)
    {
        if (slotData != null && !string.IsNullOrEmpty(pendingAnimalId) && toolInventory != null)
        {
            ClearAnimalFromOtherTools(pendingAnimalId, slotData.ToolId);
            toolInventory.TryAssignAnimalToTool(slotData.ToolId, pendingAnimalId);
            onAssigned?.Invoke();
        }

        Close();
    }

    /// <summary>
    /// 동물이 다른 도구에 장착되어 있으면 해제합니다. (변경 장착 시 중복 장착 방지)
    /// </summary>
    private void ClearAnimalFromOtherTools(string animalId, string keepToolId)
    {
        if (toolInventory == null || string.IsNullOrEmpty(animalId))
            return;

        IReadOnlyList<SlotData_Tool> toolSlots = toolInventory.ToolSlotsList;

        if (toolSlots == null)
            return;

        for (int i = 0; i < toolSlots.Count; i++)
        {
            SlotData_Tool toolSlot = toolSlots[i];

            if (toolSlot == null)
                continue;

            if (!toolSlot.CurrentAnimalSet || toolSlot.CurrentAnimalId != animalId)
                continue;

            if (toolSlot.ToolId == keepToolId)
                continue;

            toolInventory.TryRemoveAnimalFromTool(toolSlot.ToolId);
        }
    }

    private bool IsPointerInsidePanel()
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = GetPointerScreenPosition()
        };

        raycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, raycastResults);

        for (int i = 0; i < raycastResults.Count; i++)
        {
            GameObject hitObject = raycastResults[i].gameObject;
            if (hitObject == null)
                continue;

            if (hitObject.transform == transform || hitObject.transform.IsChildOf(transform))
                return true;
        }

        return false;
    }

    private static Vector2 GetPointerScreenPosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            return Mouse.current.position.ReadValue();

        if (Touchscreen.current != null)
            return Touchscreen.current.primaryTouch.position.ReadValue();
#endif
        return Input.mousePosition;
    }

    private static bool WasPrimaryPressThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;

        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButtonDown(0);
#else
        return false;
#endif
    }
}
