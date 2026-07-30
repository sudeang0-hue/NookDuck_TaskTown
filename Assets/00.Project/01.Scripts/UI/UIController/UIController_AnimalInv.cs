/* 전체 인벤토리와 각 슬롯 UI의 연결을 담당
 * 동물 최초 획득 -> 슬롯 생성/갱신
 * 중복 획득 -> 기존 슬롯 갱신
 * 슬롯 생성 시 Animal_Inv_Page Controller 참조를 주입
 */

using System.Collections.Generic;
using TaskTown.KDH;
using UnityEngine;

namespace UI
{
    public class UIController_AnimalInv : MonoBehaviour
    {
        [Header("동물 인벤토리")]
        private InventoryManager_Animal animalInventory;

        [SerializeField] private SlotUI_AnimalInv animalSlotPrefab;
        [SerializeField] private Transform animalSlotContentRoot;

        [Header("동물 상세 페이지 (씬의 Animal_Inv_Page)")]
        [SerializeField] private UIController_AnimalInvPage animalInvPageController;

        [Header("마을 동물 배치 (미연결 시 Find)")]
        private VillageAnimalSetUI_Manager villageAnimalSet;

        [Header("인스펙터 확인용 인벤토리 리스트")]
        [SerializeField] private List<SlotUI_AnimalInv> slotMaplist = new List<SlotUI_AnimalInv>();
        private readonly Dictionary<string, SlotUI_AnimalInv> slotMap = new Dictionary<string, SlotUI_AnimalInv>();

        private InventoryManager_Tool toolInventory;
        private bool subscribedToToolInventory;
        private bool subscribedToVillagePlacement;

        private void Awake()
        {
            if (animalInventory == null)
                animalInventory = FindFirstObjectByType<InventoryManager_Animal>();

            ResolveToolInventory();
            ResolveVillageAnimalSet();
        }

        private void OnEnable()
        {
            // Inv 패널/컨트롤러 활성화 시 상세 페이지는 항상 닫힌 상태로 시작
            animalInvPageController?.CloseAnimalInvPage();

            if (!TryResolveInventory())
                return;

            animalInventory.OnAnimalInventoryChanged += SyncAllSlots;
            animalInventory.OnAnimalSlotChanged += RefreshSlot;

            SubscribeToolInventoryEvents();
            SubscribeVillagePlacementEvents();

            // 컨트롤러는 상시 활성 매니저에 있으므로, Content가 켜져 있을 때만 즉시 동기화
            SyncAllSlots();
        }

        /// <summary>
        /// 동물 Inv 패널이 열릴 때 호출합니다.
        /// 상세 페이지를 닫은 뒤 슬롯 UI를 동기화합니다.
        /// </summary>
        public void NotifyPanelOpened()
        {
            animalInvPageController?.CloseAnimalInvPage();
            SubscribeToolInventoryEvents();
            SubscribeVillagePlacementEvents();
            SyncAllSlots();
        }

        /// <summary>
        /// 동물 Inv 패널이 닫힐 때 호출합니다.
        /// 열려 있는 Animal_Inv_Page(상세)도 함께 닫습니다.
        /// </summary>
        public void NotifyPanelClosed()
        {
            animalInvPageController?.CloseAnimalInvPage();
        }

        private void OnDisable()
        {
            if (animalInventory != null)
            {
                animalInventory.OnAnimalInventoryChanged -= SyncAllSlots;
                animalInventory.OnAnimalSlotChanged -= RefreshSlot;
            }

            UnsubscribeToolInventoryEvents();
            UnsubscribeVillagePlacementEvents();
        }

        private bool TryResolveInventory()
        {
            if (animalInventory == null)
                animalInventory = InventoryManager_Animal.Instance;

            if (animalInventory == null)
            {
                Debug.LogWarning("[UIController_AnimalInv] toolInventory 가 연결되지 않았습니다.");
                return false;
            }

            if (animalSlotPrefab == null)
            {
                Debug.LogWarning("[UIController_AnimalInv] animalSlotPrefab 이 없습니다.");
                return false;
            }

            if (animalSlotContentRoot == null)
            {
                Debug.LogWarning("[UIController_AnimalInv] animalSlotContentRoot 이 없습니다.");
                return false;
            }

            if (animalInvPageController == null)
            {
                Debug.LogWarning("[UIController_AnimalInv] animalInvPageController 가 연결되지 않았습니다. 슬롯 클릭 시 상세 페이지가 열리지 않습니다.");
            }

            return true;
        }

        private void ResolveToolInventory()
        {
            if (toolInventory == null)
                toolInventory = InventoryManager_Tool.Instance;
        }

        private void ResolveVillageAnimalSet()
        {
            if (villageAnimalSet == null)
                villageAnimalSet = FindFirstObjectByType<VillageAnimalSetUI_Manager>();
        }

        private void SubscribeToolInventoryEvents()
        {
            ResolveToolInventory();
            if (toolInventory == null || subscribedToToolInventory)
                return;

            toolInventory.OnToolSlotChanged += HandleToolSlotChanged;
            toolInventory.OnToolInventoryChanged += RefreshAllStatusIcons;
            subscribedToToolInventory = true;
        }

        private void UnsubscribeToolInventoryEvents()
        {
            if (toolInventory == null || !subscribedToToolInventory)
                return;

            toolInventory.OnToolSlotChanged -= HandleToolSlotChanged;
            toolInventory.OnToolInventoryChanged -= RefreshAllStatusIcons;
            subscribedToToolInventory = false;
        }

        private void SubscribeVillagePlacementEvents()
        {
            ResolveVillageAnimalSet();
            if (villageAnimalSet == null || subscribedToVillagePlacement)
                return;

            villageAnimalSet.OnVillagePlacementChanged += RefreshAllStatusIcons;
            subscribedToVillagePlacement = true;
        }

        private void UnsubscribeVillagePlacementEvents()
        {
            if (villageAnimalSet == null || !subscribedToVillagePlacement)
                return;

            villageAnimalSet.OnVillagePlacementChanged -= RefreshAllStatusIcons;
            subscribedToVillagePlacement = false;
        }

        private void HandleToolSlotChanged(SlotData_Tool _)
        {
            RefreshAllStatusIcons();
        }

        /// <summary>
        /// 도구 장착/마을 배치 변경 시 슬롯 상태 아이콘만 갱신합니다.
        /// </summary>
        private void RefreshAllStatusIcons()
        {
            if (!CanUpdateView())
                return;

            foreach (KeyValuePair<string, SlotUI_AnimalInv> pair in slotMap)
            {
                if (pair.Value != null)
                    pair.Value.RefreshStatusIcons();
            }
        }

        /// <summary>
        /// Inv 패널 Content가 계층상 활성인지 확인합니다.
        /// </summary>
        private bool CanUpdateView()
        {
            return animalSlotContentRoot != null &&
                   animalSlotContentRoot.gameObject.activeInHierarchy;
        }

        /// <summary>
        /// 인벤토리 전체와 슬롯 UI를 동기화합니다.
        /// </summary>
        public void SyncAllSlots()
        {
            if (!TryResolveInventory())
                return;

            if (!CanUpdateView())
                return;

            IReadOnlyList<SlotData_Animal> animalSlots = animalInventory.AnimalSlotsList;
            var activeIds = new HashSet<string>();

            foreach (SlotData_Animal slotData in animalSlots)
            {
                if (slotData == null)
                    continue;

                string animalId = slotData.AnimalId;

                if (string.IsNullOrEmpty(animalId))
                    continue;

                activeIds.Add(animalId);
                RefreshSlot(slotData);
            }

            RemoveStaleSlots(activeIds);
        }

        public void RemoveStaleSlots(HashSet<string> activeIds)
        {
            var staleIds = new List<string>();

            foreach (KeyValuePair<string, SlotUI_AnimalInv> pair in slotMap)
            {
                if (!activeIds.Contains(pair.Key))
                    staleIds.Add(pair.Key);
            }

            foreach (string staleId in staleIds)
            {
                if (!slotMap.TryGetValue(staleId, out SlotUI_AnimalInv staleSlot))
                    continue;

                slotMaplist.Remove(staleSlot);
                slotMap.Remove(staleId);

                if (staleSlot != null)
                    Destroy(staleSlot.gameObject);
            }
        }

        public void AddAnimalSlot(SlotData_Animal slotData)
        {
            if (slotData == null)
                return;

            string animalId = slotData.AnimalId;

            if (string.IsNullOrEmpty(animalId))
                return;

            if (slotMap.ContainsKey(animalId))
                return;

            SlotUI_AnimalInv createdSlot = Instantiate(animalSlotPrefab, animalSlotContentRoot);
            createdSlot.Initialize(slotData, animalInventory, animalInvPageController);

            slotMap.Add(animalId, createdSlot);
            slotMaplist.Add(createdSlot);
        }

        /// <summary>
        /// 슬롯 갱신. 최초 획득 시 생성, 중복 획득 시 수량 갱신
        /// </summary>
        public void RefreshSlot(SlotData_Animal slotData)
        {
            if (slotData == null)
                return;

            if (!CanUpdateView())
                return;

            string animalId = slotData.AnimalId;

            if (string.IsNullOrEmpty(animalId))
                return;

            if (!slotMap.TryGetValue(animalId, out SlotUI_AnimalInv slotView))
            {
                AddAnimalSlot(slotData);
                return;
            }

            slotView.Refresh(slotData);
        }
    }
}
