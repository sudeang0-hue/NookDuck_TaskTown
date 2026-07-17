using Animal.Data;
using System;
using System.Collections.Generic;
using TaskTown.KDH;
using UnityEngine;

namespace UI
{
    public class UIController_AnimalDex : MonoBehaviour
    {
        [Header("테스트용 동물 인벤토리\n추후 실제 인벤토리 클래스로 연결")]
        [SerializeField] private InventoryManager_Animal animalInventory;

        [Header("동물 데이터베이스")]
        [SerializeField] private AnimalDatabase animalDatabase;

        [Header("도감 슬롯")]
        [SerializeField] private SlotUI_AnimalDex animalSlotPrefab;
        [SerializeField] private Transform animalSlotContentRoot;

        [Header("동물 상세 페이지")]
        [SerializeField] private UIController_AnimalDexPage animalPageController;

        [Header("Runtime 확인용")]
        [SerializeField]
        private List<SlotUI_AnimalDex> createdSlots = new List<SlotUI_AnimalDex>();
        private readonly Dictionary<string, SlotUI_AnimalDex> slotMap = new Dictionary<string, SlotUI_AnimalDex>();

        private bool isInitialized;

        private void Awake()
        {
            if (animalInventory == null)
            {
                animalInventory = InventoryManager_Animal.Instance;
            }

            InitializeAnimalDex();
        }
        private void Start()
        {
            RefreshInventory();
        }


        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        /// <summary>
        /// AnimalDatabase에 등록된 전체 동물을 기준으로 도감 슬롯을 생성.
        /// 잠금 상태의 도감 슬롯으로 생성합니다.
        /// </summary>
        public void InitializeAnimalDex()
        {
            if (isInitialized)
                return;

            if (!ValidateReferences())
                return;

            CreateAnimalSlots();

            isInitialized = true;
        }


        /// <summary>
        /// AnimalDatabase의 동물 수만큼 슬롯을 생성하고
        /// 각 슬롯에 AnimalDataSO를 전달합니다.
        /// </summary>
        private void CreateAnimalSlots()
        {
            IReadOnlyList<AnimalDataSO> animals = animalDatabase.Animals;

            if (animals == null || animals.Count == 0)
            {
                Debug.LogWarning("[UIController_AnimalDex] AnimalDatabase에 등록된 동물이 없습니다.");
                return;
            }

            for (int i = 0; i < animals.Count; i++)
            {
                AnimalDataSO animalData = animals[i];

                if (animalData == null)
                {
                    Debug.LogWarning($"[UIController_AnimalDex] AnimalDatabase의 {i}번째 데이터가 비어 있습니다.");
                    continue;
                }

                string animalId = animalData.Id;

                if (string.IsNullOrEmpty(animalId))
                {
                    Debug.LogWarning( $"[UIController_AnimalDex] {i}번째 동물 ID가 비어 있습니다.");
                    continue;
                }

                if (slotMap.ContainsKey(animalId))
                {
                    Debug.LogWarning($"[UIController_AnimalDex] 중복된 동물 ID입니다: {animalId}");
                    continue;
                }

                SlotUI_AnimalDex slot = Instantiate(animalSlotPrefab, animalSlotContentRoot);

                // slot.Initialize(toolData, animalPageController);
                slot.Initialize(animalData, animalPageController, false);

                slotMap.Add(animalId, slot);
                createdSlots.Add(slot);

            }
        }

        /// <summary>
        /// 현재 동물 인벤토리에 존재하는 동물의
        /// 도감 슬롯을 해금 상태로 갱신합니다.
        /// </summary>
        private void RefreshInventory()
        {

            if (animalInventory == null)
            {
                Debug.LogWarning("[UIController_AnimalDex] InventoryManager_Animal.Instance가 없습니다.");
                return;
            }

            foreach (KeyValuePair<string, SlotUI_AnimalDex> pair in slotMap)
            {
                bool isUnlocked = animalInventory.TryGetAnimalSlot(pair.Key, out _);

                pair.Value.SetUnlocked(isUnlocked);
            }

        }

        /// <summary>
        /// 동물 획득 이벤트로 전달된 ID의 도감 슬롯 하나만 갱신합니다.
        /// </summary>
        public void RefreshSlot(SlotData_Animal slotData)
        {
            if (string.IsNullOrEmpty(slotData.AnimalId))
                return;

            if (!slotMap.TryGetValue(slotData.AnimalId, out SlotUI_AnimalDex slot))
            {
                Debug.LogWarning($"[UIController_AnimalDex] 도감 슬롯을 찾지 못했습니다: {slotData.AnimalId}");
                return;
            }

            slot.SetUnlocked(true);
        }

        /// <summary>
        /// 인벤토리의 동물 획득 이벤트 구독
        /// </summary>
        private void SubscribeEvents()
        {
            //InventoryManager_Animal manager = InventoryManager_Animal.Instance;

            if (animalInventory == null)
                return;

            animalInventory.OnAnimalSlotChanged += RefreshSlot;
        }

        /// <summary>
        /// 동물 획득 이벤트 구독 해제
        /// </summary>
        private void UnsubscribeEvents()
        {
            //InventoryManager_Animal manager = InventoryManager_Animal.Instance;

            if (animalInventory == null)
                return;

            animalInventory.OnAnimalSlotChanged -= RefreshSlot;
        }


        /// <summary>
        /// 필수 Inspector 참조를 검사합니다.
        /// </summary>
        private bool ValidateReferences()
        {
            if (animalDatabase == null)
            {
                Debug.LogWarning("[UIController_AnimalDex] AnimalDatabase가 연결되지 않았습니다.");
                return false;
            }
            
            if (animalInventory == null)
            {
                Debug.LogWarning("[UIController_AnimalDex] AnimalInventory 가 연결되지 않았습니다.");
                return false;
            }

            if (animalSlotPrefab == null)
            {
                Debug.LogWarning("[UIController_AnimalDex] 동물 도감 슬롯 Prefab이 연결되지 않았습니다.");
                return false;
            }

            if (animalSlotContentRoot == null)
            {
                Debug.LogWarning("[UIController_AnimalDex] 동물 슬롯을 생성할 Content Root가 연결되지 않았습니다.");
                return false;
            }
            
            if (animalPageController == null)
            {
                Debug.LogWarning("[UIController_AnimalDex] 동물 상세 페이지 UIController_AnimalDexPage가 연결되지 않았습니다.");

                return false;
            }

            return true;
        }


    }
}
