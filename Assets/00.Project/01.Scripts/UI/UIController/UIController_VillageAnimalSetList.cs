using System;
using System.Collections.Generic;
using TaskTown.KDH;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace UI
{
    /// <summary>
    /// SetAnimalList_Panel 제어 (UIController_ToolSetList와 유사).
    /// 인벤 동물 중 이미 배치된(중복) animalId는 목록에서 제외합니다.
    /// 슬롯 클릭 시 선택 상태 + 콜백(드래프트 배치)을 호출합니다.
    /// </summary>
    public class UIController_VillageAnimalSetList : MonoBehaviour
    {
        [SerializeField] private InventoryManager_Animal animalInventory;
        [SerializeField] private SlotUI_VillageAnimalSetList animalSetListPrefab;
        [SerializeField] private Transform animalSetSlotContentRoot;
        [SerializeField] private TMP_Text noAvailableAnimalText;

        private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();
        private readonly List<SlotUI_VillageAnimalSetList> spawnedSlots = new List<SlotUI_VillageAnimalSetList>();

        private int pendingVillageSlotIndex = -1;
        private string selectedAnimalId;
        private Action<int, string> onAnimalSelected;
        private Func<string, bool> isAnimalExcluded;

        public bool IsOpen => gameObject.activeSelf;
        public int PendingVillageSlotIndex => pendingVillageSlotIndex;
        public string SelectedAnimalId => selectedAnimalId;

        private void Awake()
        {
            if (animalInventory == null)
                animalInventory = InventoryManager_Animal.Instance;

            if (animalSetListPrefab == null)
                Debug.LogWarning("[UIController_VillageAnimalSetList] animalSetListPrefab이 없습니다.", this);

            if (animalSetSlotContentRoot == null)
                Debug.LogWarning("[UIController_VillageAnimalSetList] animalSetSlotContentRoot가 없습니다.", this);
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

            Close();
        }

        /// <summary>
        /// 목록을 엽니다.
        /// </summary>
        /// <param name="villageSlotIndex">배치 대상 슬롯</param>
        /// <param name="onAnimalSelectedCallback">선택 시 콜백</param>
        /// <param name="isExcluded">true면 목록에서 제외 (이미 배치된 ID 가드)</param>
        public void Open(
            int villageSlotIndex,
            Action<int, string> onAnimalSelectedCallback = null,
            Func<string, bool> isExcluded = null)
        {
            pendingVillageSlotIndex = villageSlotIndex;
            onAnimalSelected = onAnimalSelectedCallback;
            isAnimalExcluded = isExcluded;
            selectedAnimalId = null;

            transform.SetAsLastSibling();

            if (gameObject.activeSelf)
            {
                PopulateSlots();
                return;
            }

            gameObject.SetActive(true);
        }

        public void Close()
        {
            pendingVillageSlotIndex = -1;
            selectedAnimalId = null;
            onAnimalSelected = null;
            isAnimalExcluded = null;
            ClearSelectionVisual();

            if (!gameObject.activeSelf)
                return;

            gameObject.SetActive(false);
        }

        private void PopulateSlots()
        {
            if (animalInventory == null)
                animalInventory = InventoryManager_Animal.Instance;

            if (animalInventory == null || animalSetListPrefab == null || animalSetSlotContentRoot == null)
                return;

            ClearSlots();

            IReadOnlyList<SlotData_Animal> animalSlots = animalInventory.AnimalSlotsList;
            int createdCount = 0;

            if (animalSlots != null && animalSlots.Count > 0)
            {
                for (int i = 0; i < animalSlots.Count; i++)
                {
                    SlotData_Animal slotData = animalSlots[i];

                    if (slotData == null || string.IsNullOrEmpty(slotData.AnimalId) || slotData.AnimalData == null)
                        continue;

                    if (!IsAvailableForPlacementAnimal(slotData))
                        continue;

                    SlotUI_VillageAnimalSetList created = Instantiate(animalSetListPrefab, animalSetSlotContentRoot);
                    created.Initialize(slotData, HandleAnimalSlotSelected);
                    spawnedSlots.Add(created);
                    createdCount++;
                }
            }

            if (noAvailableAnimalText != null)
                noAvailableAnimalText.gameObject.SetActive(createdCount == 0);
        }

        /// <summary>
        /// 이미 배치된 animalId는 목록에 넣지 않습니다. (중복 배치 가드)
        /// </summary>
        private bool IsAvailableForPlacementAnimal(SlotData_Animal slotData)
        {
            if (slotData == null || string.IsNullOrEmpty(slotData.AnimalId))
                return false;

            if (isAnimalExcluded != null && isAnimalExcluded(slotData.AnimalId))
                return false;

            return true;
        }

        private void ClearSlots()
        {
            for (int i = 0; i < spawnedSlots.Count; i++)
            {
                if (spawnedSlots[i] != null)
                    Destroy(spawnedSlots[i].gameObject);
            }

            spawnedSlots.Clear();

            if (animalSetSlotContentRoot == null)
                return;

            for (int i = animalSetSlotContentRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = animalSetSlotContentRoot.GetChild(i);
                if (child == null)
                    continue;

                if (child.GetComponent<SlotUI_VillageAnimalSetList>() != null)
                    Destroy(child.gameObject);
            }
        }

        private void HandleAnimalSlotSelected(SlotData_Animal slotData)
        {
            if (slotData == null || string.IsNullOrEmpty(slotData.AnimalId))
                return;

            // 선택 직전 한 번 더 중복 가드
            if (isAnimalExcluded != null && isAnimalExcluded(slotData.AnimalId))
            {
                Debug.LogWarning(
                    $"[UIController_VillageAnimalSetList] 이미 배치된 동물입니다: {slotData.AnimalId}",
                    this);
                return;
            }

            selectedAnimalId = slotData.AnimalId;
            ApplySelectionVisual(selectedAnimalId);

            int slotIndex = pendingVillageSlotIndex;
            string animalId = selectedAnimalId;
            Action<int, string> callback = onAnimalSelected;
            callback?.Invoke(slotIndex, animalId);
        }

        private void ApplySelectionVisual(string animalId)
        {
            for (int i = 0; i < spawnedSlots.Count; i++)
            {
                SlotUI_VillageAnimalSetList slot = spawnedSlots[i];
                if (slot == null)
                    continue;

                bool selected = slot.CurrentSlotData != null &&
                                slot.CurrentSlotData.AnimalId == animalId;
                slot.SetSelected(selected);
            }
        }

        private void ClearSelectionVisual()
        {
            for (int i = 0; i < spawnedSlots.Count; i++)
            {
                if (spawnedSlots[i] != null)
                    spawnedSlots[i].SetSelected(false);
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
}
