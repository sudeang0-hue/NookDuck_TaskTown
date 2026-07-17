/* 전체 인벤토리 UIController_AnimalInvPage 와 각 슬롯 UIController_AnimalInvPage 의 연결을 담당
 * 동물 최초 획득 -> UIController_AnimalInvPage 슬롯 갱신
 * 중복 획득 -> 기존 UIController_AnimalInvPage 슬롯 갱신
 */

using System.Collections.Generic;
using TaskTown.KDH;
using UnityEngine;

namespace UI
{

    public class UIController_AnimalInv : MonoBehaviour
    {
        [Header("동물 인벤토리")]
        [SerializeField] private InventoryManager_Animal animalInventory;

        [SerializeField] private SlotUI_AnimalInv animalSlotPrefab;
        [SerializeField] private Transform animalSlotContentRoot;

        [Header("인스펙터 확인용 인벤토리 리스트")]
        [SerializeField] private List<SlotUI_AnimalInv> slotMaplist = new List<SlotUI_AnimalInv>();
        private readonly Dictionary<string, SlotUI_AnimalInv> slotMap = new Dictionary<string, SlotUI_AnimalInv>();

        private void Awake()
        {
            if (animalInventory == null)
                animalInventory = InventoryManager_Animal.Instance;
        }

        private void OnEnable()
        {
            if (!TryResolveInventory())
                return;

            animalInventory.OnAnimalInventoryChanged += SyncAllSlots;
            animalInventory.OnAnimalSlotChanged += RefreshSlot;

            // 컨트롤러는 상시 활성 매니저에 있으므로, Content가 켜져 있을 때만 즉시 동기화
            SyncAllSlots();
        }

        private void OnDisable()
        {
            if (animalInventory == null)
                return;

            animalInventory.OnAnimalInventoryChanged -= SyncAllSlots;
            animalInventory.OnAnimalSlotChanged -= RefreshSlot;
        }

        private bool TryResolveInventory()
        {
            if (animalInventory == null)
                animalInventory = InventoryManager_Animal.Instance;

            if (animalInventory == null)
            {
                Debug.LogWarning("[UIController_AnimalInv] animalInventory 가 연결되지 않았습니다.");
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

            return true;
        }

        /// <summary>
        /// Inv 패널 Content가 계층상 활성인지 확인합니다.
        /// 패널이 꺼진 상태에서 슬롯을 만들면 오픈 시 반영이 누락될 수 있습니다.
        /// </summary>
        private bool CanUpdateView()
        {
            return animalSlotContentRoot != null &&
                   animalSlotContentRoot.gameObject.activeInHierarchy;
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
            createdSlot.Initialize(slotData, animalInventory);

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