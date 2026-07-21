/* ToolSettingList_root 패널 제어
 * - Open/Close
 * - 테스트 슬롯 클릭 시 패널 닫기
 * - 패널 바깥 클릭 시 패널 닫기
 * (실제 장착/목록 동기화는 이후 단계)
 */

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

    [Tooltip("배치할 수 있는 도구가 없을 때 출력할 텍스트")]
    [SerializeField] private TMP_Text noAvaliableToolText;

    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

    public bool IsOpen => gameObject.activeSelf;

    private void Awake()
    {
        if (toolInventory == null)
            toolInventory = InventoryManager_Tool.Instance;

        if (toolSetPrefab == null)
            Debug.LogWarning("[UIController_ToolSetList] toolSetPrefab 이 할당되지 않았습니다.", this);

        if (toolSetSlotContentRoot == null)
            Debug.LogWarning("[UIController_ToolSetList] toolSetSlotContentRoot 이 연결되지 않았습니다.", this);
    }

    private void OnEnable()
    {
        // Content에 미리 배치된 테스트 슬롯에 Close 콜백을 연결합니다.
        BindExistingSlots();
    }

    private void Update()
    {
        if (!IsOpen)
            return;

        if (!WasPrimaryPressThisFrame())
            return;

        if (IsPointerInsidePanel())
            return;

        // 목록 범위 밖 클릭: 도구 선택 의사가 없는 것으로 판단하고 닫습니다.
        Close();
    }

    /// <summary>
    /// ToolSettingList_root 패널을 엽니다.
    /// </summary>
    public void Open()
    {
        if (gameObject.activeSelf)
        {
            BindExistingSlots();
            return;
        }

        gameObject.SetActive(true);
    }

    /// <summary>
    /// ToolSettingList_root 패널을 닫습니다.
    /// </summary>
    public void Close()
    {
        if (!gameObject.activeSelf)
            return;

        gameObject.SetActive(false);
    }

    /// <summary>
    /// Content에 이미 있는 SlotUI_ToolSet에 선택 콜백을 연결합니다.
    /// </summary>
    private void BindExistingSlots()
    {
        if (toolSetSlotContentRoot == null)
            return;

        SlotUI_ToolSet[] slots = toolSetSlotContentRoot.GetComponentsInChildren<SlotUI_ToolSet>(true);

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                continue;

            slots[i].SetSelectedCallback(HandleToolSlotSelected);
        }
    }

    /// <summary>
    /// 테스트/선택: 슬롯 클릭 시 장착 없이 패널만 닫습니다.
    /// </summary>
    private void HandleToolSlotSelected(SlotData_Tool slotData)
    {
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

    // TODO: Tool 인벤토리 기반으로, 보유 도구 중 동물이 배치되지 않은 도구만 출력
    // TODO: 배치 가능한 도구가 없으면 noAvaliableToolText 표시
}
