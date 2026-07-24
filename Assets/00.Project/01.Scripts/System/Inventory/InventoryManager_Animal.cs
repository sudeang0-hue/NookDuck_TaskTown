using Animal.Data;
using System;
using System.Collections.Generic;
using TaskTown.Gacha;
using UnityEngine;

namespace TaskTown.KDH
{
    public class InventoryManager_Animal : MonoBehaviour
    {
        public static InventoryManager_Animal Instance { get; private set; }

        [Tooltip("ICoinWallet을 구현한 컴포넌트(CoinManager)를 연결합니다. 비워두면 코인 비용 체크 없이 중복 개수만으로 레벨업합니다.")]
        [SerializeField] private MonoBehaviour coinWalletSource;
        private ICoinWallet CoinWallet => coinWalletSource as ICoinWallet;

        //인벤토리 최대 슬롯 수
        // public int maxSlots = 999;


        [Header("동물 런타임 슬롯")]
        [Tooltip("현재 플레이어가 보유한 동물 슬롯 목록")]
        [SerializeField] private List<SlotData_Animal> animalSlotsList = new List<SlotData_Animal>();

        [Header("동물 데이터 베이스")]
        [SerializeField] private AnimalDatabase animalDatabase;

        // ID 기반 슬롯 조회를 위한 런타임 Dictionary
        private Dictionary<string, SlotData_Animal> animalSlotsDic = new Dictionary<string, SlotData_Animal>();

        // 외부에서 인벤토리 슬롯 목록을 읽을 수 있도록 노출하는 프로퍼티
        public IReadOnlyList<SlotData_Animal> AnimalSlotsList => animalSlotsList;

        // 동물 인벤토리 데이터가 변경되었을 때 호출
        public event Action OnAnimalInventoryChanged;
        // 특정 동물 슬롯의 데이터가 변경되었을 때 호출
        public event Action<SlotData_Animal> OnAnimalSlotChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            DontDestroyOnLoad(gameObject);

            InitializeDictionary();
        }

        /// <summary>
        /// 슬롯 리스트 기반으로 Dictionary 를 다시 구성
        /// </summary>
        private void InitializeDictionary()
        {
            animalSlotsDic.Clear();

            for (int i = animalSlotsList.Count - 1; i >= 0; i--)
            {
                SlotData_Animal slot = animalSlotsList[i];

                if (slot == null)
                {
                    animalSlotsList.RemoveAt(i);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(slot.AnimalId))
                {
                    Debug.LogWarning("[InventoryManager_Animal] AnimalId가 비어 있는 슬롯을 제거합니다.");

                    animalSlotsList.RemoveAt(i);
                    continue;
                }

                if (animalSlotsDic.ContainsKey(slot.AnimalId))
                {
                    Debug.LogWarning($"[InventoryManager_Animal] 중복 AnimalId 슬롯을 제거합니다: {slot.AnimalId}");

                    animalSlotsList.RemoveAt(i);
                    continue;
                }

                animalSlotsDic.Add(slot.AnimalId, slot);

                // 레벨업 요구치 갱신
                RefreshSlotGrowthData(slot);
            }
        }

        /// <summary>
        /// 동물 ID 로 해당하는 동물 데이터를 반환
        /// </summary>
        public AnimalDataSO GetAnimalData(string animalId)
        {
            if (string.IsNullOrWhiteSpace(animalId)) return null;

            if (animalDatabase == null)
            {
                Debug.LogWarning("[InventoryManager_Animal] AnimalDatabase가 연결되지 않았습니다.");
                return null;
            }

            return animalDatabase.GetAnimalData(animalId);
        }

        /// <summary>
        /// 동물 슬롯 추가. 최초 획득이면 슬롯을 생성하고, 중복 획득이면 기존 슬롯의 개수를 증가
        /// </summary>
        public bool AddAnimalSlot(AnimalDataSO animalData)
        {
            if (animalData == null)
            {
                Debug.LogWarning("[InventoryManager_Animal] 추가할 AnimalDataSO가 없습니다.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(animalData.Id))
            {
                Debug.LogWarning($"[InventoryManager_Animal] {animalData.DisplayName} 의 Id 가 비어있습니다.");
                return false;
            }

            if (animalSlotsDic.TryGetValue(animalData.Id, out SlotData_Animal existingSlot))
            {
                existingSlot.AddCount();

                NotifySlotChanged(existingSlot);

                Debug.Log($"[InventoryManager_Animal] 중복 동물 획득: " +
                    $"{animalData.DisplayName} + 1 / 현재 개수: {existingSlot.CurrentCount}");

                return true;
            }

            SlotData_Animal newSlot = new SlotData_Animal(animalData, level: 1, currentCount: 1);

            RefreshSlotGrowthData(newSlot);

            animalSlotsList.Add(newSlot);
            animalSlotsDic.Add(animalData.Id, newSlot);

            NotifySlotChanged(newSlot);

            Debug.Log($"[InventoryManager_Animal] 신규 동물 획득: {animalData.DisplayName}");

            return true;
        }

        /// <summary>
        /// 동물 ID로 런타임 슬롯 조회
        /// </summary>
        public bool TryGetAnimalSlot(string animalId, out SlotData_Animal slot)
        {
            slot = null;

            if (string.IsNullOrWhiteSpace(animalId)) return false;

            return animalSlotsDic.TryGetValue(animalId, out slot);
        }

        /// <summary>
        /// 동물 보유수량 반환. 미보유 시 0 반환
        /// </summary>
        public int GetAnimalCount(string animalId)
        {
            return TryGetAnimalSlot(animalId, out SlotData_Animal slot) ? slot.CurrentCount : 0;
        }

        /// <summary>
        /// 해당 동물을 한 번 이상 획득했는지 확인. 도감 해금 여부로 사용.
        /// </summary>
        private bool IsAnimalUnlocked(string animalId)
        {
            return TryGetAnimalSlot(animalId, out _);
        }

        /// <summary>
        /// 동물 레벨업 가능 여부 확인 (중복 개수 + 코인 잔액)
        /// </summary>
        public bool CanLevelUpAnimal(string animalId)
        {
            if (!TryGetAnimalSlot(animalId, out SlotData_Animal slot)) return false;

            if (!slot.CanLevelUp()) return false;

            long coinCost = slot.GetLevelUpCoinCost();

            if (CoinWallet != null && CoinWallet.Balance < coinCost)
            {
                Debug.Log($"[InventoryManager_Animal] not enough coin. needed: {coinCost}, balance: {CoinWallet.Balance}");
                return false;
            }

            return true;
        }

        public bool TryLevelUpAnimal(string animalId)
        {
            if (!TryGetAnimalSlot(animalId, out SlotData_Animal slot))
            {
                Debug.LogWarning($"[InventoryManager_Animal] 존재하지 않는 슬롯입니다: {animalId}");
                return false;
            }

            if (slot.IsMaxLevel)
            {
                Debug.Log($"[InventoryManager_Animal] 이미 최대 레벨인 동물입니다: {animalId}");
                return false;
            }

            if (!CanLevelUpAnimal(animalId)) return false;

            long coinCost = slot.GetLevelUpCoinCost();

            if (CoinWallet != null && !CoinWallet.TrySpend(coinCost))
            {
                Debug.Log($"[InventoryManager_Animal] failed to spend coin for level up: {animalId}, cost: {coinCost}");
                return false;
            }

            // 재료 소모 후 레벨업 (본체 1개는 유지)
            if (!slot.TryConsumeForLevelUp())
            {
                Debug.Log($"[InventoryManager_Animal] 재료 소모 실패: {animalId}");
                return false;
            }

            slot.AnimalLevelUp();

            // 다음 레벨 요구치 갱신
            RefreshSlotGrowthData(slot);

            NotifySlotChanged(slot);

            Debug.Log($"[InventoryManager_Animal] 동물 레벨업 성공: {animalId} / 현재 레벨 {slot.Level}");

            return true;
        }

        /// <summary>
        /// 현재 동물 슬롯 데이터를 저장용 리스트 형식으로 변환합니다.
        /// </summary>
        public List<SlotSaveData_Animal> CreateSaveData()
        {
            List<SlotSaveData_Animal> saveDataList = new();

            foreach (SlotData_Animal slot in animalSlotsList)
            {
                if (slot == null)
                    continue;

                if (string.IsNullOrWhiteSpace(slot.AnimalId))
                    continue;

                SlotSaveData_Animal saveData =
                    new SlotSaveData_Animal(slot.AnimalData, slot.Level, slot.CurrentCount);

                saveDataList.Add(saveData);
            }

            return saveDataList;
        }

        /// <summary>
        /// 저장 데이터를 기반으로 런타임 동물 인벤토리를 복원합니다.
        /// </summary>
        public void LoadSaveData(List<SlotSaveData_Animal> saveDataList)
        {
            animalSlotsList.Clear();
            animalSlotsDic.Clear();

            if (saveDataList == null)
            {
                NotifyInventoryChanged();
                return;
            }

            foreach (SlotSaveData_Animal saveData in saveDataList)
            {
                if (saveData == null)
                    continue;

                if (string.IsNullOrWhiteSpace(saveData.animaldata.Id))
                    continue;

                if (saveData.currentCount <= 0)
                    continue;

                if (animalSlotsDic.ContainsKey(saveData.animaldata.Id))
                {
                    Debug.LogWarning($"[InventoryManager_Animal] 저장 데이터에 중복 ID가 있습니다: {saveData.animaldata.Id}");
                    continue;
                }

                SlotData_Animal runtimeSlot =
                    new SlotData_Animal(saveData.animaldata, saveData.level, saveData.currentCount);

                RefreshSlotGrowthData(runtimeSlot);

                animalSlotsList.Add(runtimeSlot);
                animalSlotsDic.Add(runtimeSlot.AnimalId, runtimeSlot);
            }

            NotifyInventoryChanged();
        }

        /// <summary>
        /// 현재 레벨을 기준으로 다음 레벨의 요구 개수와 비용을 갱신합니다.
        /// 요구치 계산 시스템은 LevelUpRequirementCalculator 에 위임합니다.
        /// </summary>
        private void RefreshSlotGrowthData(SlotData_Animal slot)
        {
            if (slot == null)
                return;

            int requiredCount = LevelUpRequirementCalculator.GetRequiredDuplicateCount(slot.Level);

            slot.ApplyGrowthData(requiredCount, slot.LevelUpCost, false);
        }

        /// <summary>
        /// 모든 동물 런타임 데이터를 삭제합니다.
        /// 새 게임 또는 저장 데이터 로드 전에 사용될 수 있습니다.
        /// </summary>
        public void ClearAnimalInventory()
        {
            animalSlotsList.Clear();
            animalSlotsDic.Clear();

            NotifyInventoryChanged();
        }

        private void NotifySlotChanged(SlotData_Animal slot)
        {
            OnAnimalSlotChanged?.Invoke(slot);
            OnAnimalInventoryChanged?.Invoke();
        }

        /// <summary>
        /// 동물 인벤토리 변경 호출
        /// </summary>
        private void NotifyInventoryChanged()
        {
            OnAnimalInventoryChanged?.Invoke();
        }



        /// <summary>
        /// 디버그 전용: 지정 개수만큼 동물을 지급합니다.        26.07.24 KDH 추가
        /// </summary>
        public bool DebugAddAnimal(string animalId, int count)
        {
            AnimalDataSO data = GetAnimalData(animalId);
            if (data == null)
            {
                Debug.LogWarning($"[InventoryManager_Animal] 존재하지 않는 AnimalId: {animalId}");
                return false;
            }
            count = Mathf.Max(1, count);
            for (int i = 0; i < count; i++)
                AddAnimalSlot(data);
            return true;
        }
        /// <summary>
        /// 디버그 전용: 보유 동물의 레벨을 바로 설정합니다. 미보유면 1개 지급 후 설정.      26.07.24 KDH 추가
        /// </summary>
        public bool DebugSetAnimalLevel(string animalId, int targetLevel)
        {
            if (!TryGetAnimalSlot(animalId, out SlotData_Animal slot))
            {
                if (!DebugAddAnimal(animalId, 1))
                    return false;
                if (!TryGetAnimalSlot(animalId, out slot))
                    return false;
            }
            slot.DebugSetLevel(targetLevel);
            RefreshSlotGrowthData(slot);
            NotifySlotChanged(slot);
            return true;
        }
    }
}
