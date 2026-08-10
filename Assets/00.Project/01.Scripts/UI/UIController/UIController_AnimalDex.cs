using Animal.Data;
using System.Collections.Generic;
using TaskTown.KDH;
using TMPro;
using UnityEngine;

namespace UI
{
    public class UIController_AnimalDex : MonoBehaviour
    {
        [Header("동물 인벤토리")]
        [SerializeField] private InventoryManager_Animal animalInventory;

        [Header("동물 데이터베이스")]
        [SerializeField] private AnimalDatabase animalDatabase;

        [Header("동물 슬롯")]
        [SerializeField] private SlotUI_AnimalDex animalSlotPrefab;
        [SerializeField] private Transform animalSlotContentRoot;
        [SerializeField] private TMP_Text animalcountMessage;
        [SerializeField, TextArea(2, 4)]
        private string countMessage = "만나본 주민의 수 : ";

        [Header("동물 상세 페이지")]
        [SerializeField] private UIController_AnimalDexPage animalPageController;

        [Header("Runtime 확인용")]
        [SerializeField]
        private List<SlotUI_AnimalDex> createdSlots = new List<SlotUI_AnimalDex>();
        private readonly Dictionary<string, SlotUI_AnimalDex> slotMap = new Dictionary<string, SlotUI_AnimalDex>();

        private bool isInitialized;

        private void Awake()
        {
            //-----------------26.08.05 KDH-------------------------
            animalInventory = InventoryManager_Animal.Instance;
            if (animalInventory == null)
                animalInventory = FindFirstObjectByType<InventoryManager_Animal>();
            //----------------------------------------

            InitializeAnimalDex();
        }
        private void Start()
        {
            RefreshInventory();
        }


        private void OnEnable()
        {
            // 도감 패널이 열릴 때마다 상세 페이지는 항상 닫힌 상태로 시작
            animalPageController?.CloseAnimalDexPage();

            SubscribeEvents();

            // ----------------08.08.KAY (패널 오픈 시 도감 Dif 갱신)------------------
            // 패널을 다시 열 때 현재 난이도 기준 Dif 표시를 맞춥니다.
            if (isInitialized)
                RefreshInventory();
            // ---------------------------------------------------------
        }

        /// <summary>
        /// 도감 패널이 열릴 때 호출합니다.
        /// 상세 페이지를 닫힌 상태로 맞춥니다.
        /// </summary>
        public void NotifyPanelOpened()
        {
            animalPageController?.CloseAnimalDexPage();
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
        /// 도감 슬롯의 해금 상태를 갱신합니다. "이미 본 적 있음" 기록은 DexRecordManager가
        /// 인벤토리와 별개로 영구 저장하므로(난이도 리셋 이후에도 유지), 현재 인벤토리 보유
        /// 여부가 아니라 그 기록을 기준으로 판단합니다.
        /// </summary>
        private void RefreshInventory()
        {
            if (DexRecordManager.Instance == null)
            {
                Debug.LogWarning("[UIController_AnimalDex] DexRecordManager.Instance가 없습니다.");
                RefreshCountMessage();
                return;
            }

            foreach (KeyValuePair<string, SlotUI_AnimalDex> pair in slotMap)
            {
                bool isUnlocked = DexRecordManager.Instance.IsDiscovered(pair.Key);
                pair.Value.SetUnlocked(isUnlocked);
            }

            RefreshCountMessage();
        }

        // ----------------08.08.KAY (도감 해금/Dif 디버그 갱신)------------------
        /// <summary>
        /// 디버그용: 도감 해금 상태를 DexRecordManager 기준으로 다시 그립니다.
        /// </summary>
        public void DebugRefreshUnlockStates()
        {
            RefreshInventory();
        }
        // ---------------------------------------------------------

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

            // 최초 획득 시 MarkDiscovered 구독 순서와 무관하게 카운트가 맞도록 이번 ID를 포함합니다.
            RefreshCountMessage(slotData.AnimalId);
        }

        /// <summary>
        /// animalcountMessage = countMessage + 획득한 적 있는 수 + "/" + AnimalDatabase 등록 수
        /// </summary>
        /// <param name="treatAsDiscoveredId">이번 프레임에 막 획득해 Discovered로 칠할 ID(이벤트 순서 보정용)</param>
        private void RefreshCountMessage(string treatAsDiscoveredId = null)
        {
            if (animalcountMessage == null)
                return;

            int totalCount = 0;
            int discoveredCount = 0;

            IReadOnlyList<AnimalDataSO> animals = animalDatabase != null ? animalDatabase.Animals : null;
            if (animals != null)
            {
                for (int i = 0; i < animals.Count; i++)
                {
                    AnimalDataSO animal = animals[i];
                    if (animal == null || string.IsNullOrEmpty(animal.Id))
                        continue;

                    totalCount++;

                    bool discovered =
                        (DexRecordManager.Instance != null && DexRecordManager.Instance.IsDiscovered(animal.Id))
                        || animal.Id == treatAsDiscoveredId;

                    if (discovered)
                        discoveredCount++;
                }
            }

            animalcountMessage.text = countMessage + discoveredCount + "/" + totalCount;
        }

        /// <summary>
        /// 인벤토리의 동물 획득 이벤트 구독
        /// </summary>
        private void SubscribeEvents()
        {

            if (animalInventory == null)
                return;

            animalInventory.OnAnimalSlotChanged += RefreshSlot;
        }

        /// <summary>
        /// 동물 획득 이벤트 구독 해제
        /// </summary>
        private void UnsubscribeEvents()
        {

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
