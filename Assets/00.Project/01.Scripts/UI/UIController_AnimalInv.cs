/* 전체 인벤토리 UI 와 각 슬롯 UI 의 연결을 담당
 * 동물 최초 획득 -> UI 슬롯 갱신
 * 중복 획득 -> 기존 UI 슬롯 갱신
 */

using System.Collections.Generic;
using Test;
using UnityEngine;

namespace UI
{

    public class UIController_AnimalInv : MonoBehaviour
    {
        
        [Header("동물 인벤토리")]
        [Tooltip("임시 클래스이므로 실제 인벤토리 시스템이 완성되면 클래스 변경")]
        [SerializeField] TestInventory_Animal animalInventory;

        [SerializeField] private SlotUI_AnimalInv animalSlotPrefab;   // 슬롯 프리팹
        [SerializeField] private Transform animalSlotContentRoot;  // 해당 슬롯을 추가하는 위치


        [Header("인스펙터 확인용 인벤토리 리스트")]
        [SerializeField] private List<SlotUI_AnimalInv> slotMaplist = new List<SlotUI_AnimalInv>();
        private readonly Dictionary<string, SlotUI_AnimalInv> slotMap = new Dictionary<string, SlotUI_AnimalInv>();


        private void Start()
        {
            if (animalSlotPrefab == null)
            {
                Debug.LogWarning("[UIController_AnimalInv] animalSlotPrefab 이 없습니다.");
                return;
            }

            if (animalSlotContentRoot == null)
            {
                Debug.LogWarning("[UIController_AnimalInv] animalSlotContentRoot 이 없습니다.");
                return;
            }

            if (animalInventory == null)
            {
                Debug.LogWarning("[UIController_AnimalInv] animalInventory 가 연결되지 않았습니다.");
                return;
            }
        }

        private void OnEnable()
        {
            if (animalInventory == null)
                return;

            animalInventory.OnAnimalAdded += AddAnimalSlot;
            animalInventory.OnAnimalChanged += RefreshSlot;

            InitializeSlots();
        }

        private void OnDisable()
        {
            if (animalInventory == null)
                return;

            animalInventory.OnAnimalAdded -= AddAnimalSlot;
            animalInventory.OnAnimalChanged -= RefreshSlot;

        }

        /// <summary>
        /// 테스트 인벤토리에 이미 존재하는 동물을 기준으로
        /// 보유 동물 UI 슬롯을 생성합니다.
        /// </summary>
        private void InitializeSlots()
        {
            IReadOnlyList<SlotData_Animal> animalSlots = animalInventory.AnimalSlots;

            if (animalSlots == null)
                return;

            for (int i = 0; i < animalSlots.Count; i++)
            {
                SlotData_Animal slotData = animalSlots[i];

                if (slotData == null)
                    continue;

                AddAnimalSlot(slotData.AnimalId);
            }

        }



        public void AddAnimalSlot(string animalId)
        {
            if (string.IsNullOrEmpty(animalId))
                return;

            if (slotMap.ContainsKey(animalId))
            {
                RefreshSlot(animalId);
                return;
            }

            if (animalInventory == null)
                return;

            if (!animalInventory.TryGetAnimalSlot(animalId, out SlotData_Animal slotData))
            {
                Debug.LogWarning($"[UIController_AnimalInv] 동물 슬롯 데이터를 찾지 못했습니다: {animalId}");
                return;
            }

            if (animalSlotPrefab == null || animalSlotContentRoot == null)
                return;


            SlotUI_AnimalInv createdSlot = Instantiate(animalSlotPrefab, animalSlotContentRoot);

            createdSlot.Initialize(slotData);

            slotMap.Add(animalId, createdSlot);
            slotMaplist.Add(createdSlot);
        }


        /// <summary>
        /// 슬롯 갱신. 중복 획득 후 갱신
        /// </summary>
        private void RefreshSlot(string animalId)
        {
            if (string.IsNullOrEmpty(animalId))
                return;

            if (!slotMap.TryGetValue(animalId, out SlotUI_AnimalInv slotView))
            {
                AddAnimalSlot(animalId);
                return;
            }

            if (animalInventory == null)
                return;

            if (!animalInventory.TryGetAnimalSlot(animalId, out SlotData_Animal slotData))
            {
                Debug.LogWarning($"[UIController_AnimalInv] 갱신할 동물 데이터를 찾지 못했습니다: {animalId}");

                return;
            }

            slotView.Refresh(slotData);
        }

    }


}
