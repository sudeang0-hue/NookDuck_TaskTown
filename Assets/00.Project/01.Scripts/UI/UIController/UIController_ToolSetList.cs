/* ToolSettingList_root 패널 제어
 * - Open/Close
 * - 보유 도구 목록을 슬롯으로 채워서 표시
 * - 슬롯 클릭 시 현재 배치 중인 동물을 그 도구에 장착(TryAssignAnimalToTool)
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

    [Tooltip("보유한 도구가 없을 때 대신 표시할 텍스트")]
    [SerializeField] private TMP_Text noAvaliableToolText;

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
    /// ToolSettingList_root 패널을 엽니다.
    /// </summary>
    /// <param name="animalId">이 패널에서 도구를 골라 장착시킬 동물 ID</param>
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
    /// 보유 도구 목록으로 슬롯을 채웁니다. 열 때마다 최신 목록으로 다시 생성합니다.
    /// </summary>
    private void PopulateSlots()
    {
        if (toolInventory == null)
            toolInventory = InventoryManager_Tool.Instance;

        if (toolInventory == null || toolSetPrefab == null || toolSetSlotContentRoot == null)
            return;

        ClearSlots();

        IReadOnlyList<SlotData_Tool> toolSlots = toolInventory.ToolSlotsList;

        if (toolSlots != null)
        {
            for (int i = 0; i < toolSlots.Count; i++)
            {
                SlotData_Tool slotData = toolSlots[i];

                if (slotData == null || string.IsNullOrEmpty(slotData.ToolId))
                    continue;

                SlotUI_ToolSet createdSlot = Instantiate(toolSetPrefab, toolSetSlotContentRoot);
                createdSlot.Initialize(slotData, HandleToolSlotSelected);
                spawnedSlots.Add(createdSlot);
            }
        }

        if (noAvaliableToolText != null)
            noAvaliableToolText.gameObject.SetActive(spawnedSlots.Count == 0);
    }

    private void ClearSlots()
    {
        for (int i = 0; i < spawnedSlots.Count; i++)
        {
            if (spawnedSlots[i] != null)
                Destroy(spawnedSlots[i].gameObject);
        }

        spawnedSlots.Clear();
    }

    /// <summary>
    /// 도구 슬롯 클릭 시, 대기 중인 동물을 그 도구에 장착하고 패널을 닫습니다.
    /// </summary>
    private void HandleToolSlotSelected(SlotData_Tool slotData)
    {
        if (slotData != null && !string.IsNullOrEmpty(pendingAnimalId) && toolInventory != null)
        {
            toolInventory.TryAssignAnimalToTool(slotData.ToolId, pendingAnimalId);
            onAssigned?.Invoke();
        }

        Close();
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
