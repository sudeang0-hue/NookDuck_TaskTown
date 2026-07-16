/* 테스트 인벤토리의 데이터 담당 범위
 * 동물 슬롯 보관
 * 중복 수량 증가
 * 레벨업 요구 수량 설정
 * 추후 레벨업 요청 처리
 * 동물 데이터 변경 이벤트 발생
 */
using Animal.Data;
using System;
using System.Collections.Generic;
using TaskTown.Gacha;
using UnityEngine;

namespace Test
{

    /// <summary>
    /// 실제 동물 인벤토리가 완성되기 전, UI 갱신을 확인하기 위한 임시 테스트 클래스
    /// </summary>
    public class TestInventory_Animal : MonoBehaviour
    {
        [Header("테스트용 보유 동물")]
        [SerializeField] private List<SlotData_Animal> animalSlots = new List<SlotData_Animal>();
        private Dictionary<string, SlotData_Animal> animalSlotsDic = new Dictionary<string, SlotData_Animal>();

        [Header("순차 획득 테스트")]
        [SerializeField]
        private List<AnimalDataSO> testAnimals = new List<AnimalDataSO>();

        [SerializeField] private int nextTestIndex;

        // 새로운 동물을 최초 획득했을 때 발생
        public event Action<string> OnAnimalAdded;
        // 이미 보유한 동물의 수량 등 데이터가 변경됐을 때 발생합니다.
        public event Action<string> OnAnimalChanged;
        // ui 초기화
        public event Action OnInventoryCleared;
        public IReadOnlyList<SlotData_Animal> AnimalSlots => animalSlots;



        private void Awake()
        {
            InitializeExistingSlots();
        }

        /// <summary>
        /// 이미 리스트에 존재하는 동물 슬롯의
        /// 레벨업 요구 수량을 현재 레벨 기준으로 초기화합니다.
        /// </summary>
        private void InitializeExistingSlots()
        {
            animalSlotsDic.Clear();

            for (int i = animalSlots.Count - 1; i >= 0; i--)
            {
                SlotData_Animal slot = animalSlots[i];

                if (slot == null)
                {
                    animalSlots.RemoveAt(i);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(slot.AnimalId))
                {
                    Debug.LogWarning("[TestInventory_Animal] AnimalId가 비어 있는 슬롯을 제거합니다.");

                    animalSlots.RemoveAt(i);
                    continue;
                }

                if (animalSlotsDic.ContainsKey(slot.AnimalId))
                {
                    Debug.LogWarning($"[TestInventory_Animal] 중복된 동물 슬롯을 제거합니다: {slot.AnimalId}");

                    animalSlots.RemoveAt(i);
                    continue;
                }

                RefreshSlotGrowthData(slot);
                animalSlotsDic.Add(slot.AnimalId, slot);
            }
        }


        /// <summary>
        /// 디버깅용 동물 순차 획득.
        /// </summary>
        public void DebugAddNextAnimal()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[TestInventory_Animal] Play Mode에서 실행해야 합니다.");
                return;
            }


            if (testAnimals == null || testAnimals.Count == 0)
            {
                Debug.LogWarning("[TestInventory_Animal] 테스트 동물 목록이 비어 있습니다.");
                return;
            }

            if (nextTestIndex < 0 || nextTestIndex >= testAnimals.Count)
            {
                Debug.Log("[TestInventory_Animal] 모든 테스트 동물을 추가했습니다.");
                return;
            }

            AnimalDataSO animalData = testAnimals[nextTestIndex];

            nextTestIndex++;

            AddAnimal(animalData);
        }



        public void DebugDeleteAnimal()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[TestInventory_Animal] Play Mode에서 실행해야 합니다.");
                return;
            }

            animalSlots.Clear();
            nextTestIndex = 0;

            OnInventoryCleared?.Invoke();

            Debug.Log("[TestInventory_Animal] 테스트용 동물 인벤토리를 비웠습니다.");
        }


        /// <summary>
        /// 동물을 획득합니다.
        /// 최초 획득이면 슬롯을 생성하고, 중복 획득이면 기존 슬롯의 수량을 증가시킵니다.
        /// </summary>
        public void AddAnimal(AnimalDataSO animalData, int amount = 1)
        {
            if (animalData == null)
            {
                Debug.LogWarning("[TestInventory_Animal] 추가할 동물 데이터가 없습니다.");
                return;
            }

            if (string.IsNullOrEmpty(animalData.Id))
            {
                Debug.LogWarning("[TestInventory_Animal] 동물 ID가 비어 있습니다.");
                return;
            }

            if (amount <= 0)
            {
                Debug.LogWarning("[TestInventory_Animal] 획득 수량은 1 이상이어야 합니다.");
                return;
            }

            if (TryGetAnimalSlot(animalData.Id, out SlotData_Animal slotData))
            {
                slotData.AddCount(amount);

                OnAnimalChanged?.Invoke(animalData.Id);

                Debug.Log($"[TestInventory_Animal] 중복 동물 획득: {animalData.DisplayName} / 현재 수량: {slotData.CurrentCount}");
                return;
            }

            SlotData_Animal newSlot = new SlotData_Animal(animalData, 1,amount);

            RefreshSlotGrowthData(newSlot);

            animalSlots.Add(newSlot);
            animalSlotsDic.Add(animalData.Id, newSlot);

            OnAnimalAdded?.Invoke(animalData.Id);

            Debug.Log($"[TestInventory_Animal] 신규 동물 획득: {animalData.DisplayName} / 수량: {amount}");
        }

        /// <summary>
        /// 해당 ID의 동물 슬롯을 찾습니다.
        /// </summary>
        public bool TryGetAnimalSlot(
            string animalId,
            out SlotData_Animal slotData)
        {
            slotData = null;

            if (string.IsNullOrEmpty(animalId))
                return false;

            for (int i = 0; i < animalSlots.Count; i++)
            {
                SlotData_Animal currentSlot = animalSlots[i];

                if (currentSlot == null)
                    continue;

                if (currentSlot.AnimalId != animalId)
                    continue;

                slotData = currentSlot;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 현재 동물 레벨을 기준으로 다음 레벨업에 필요한
        /// 총 보유 수량을 계산하여 런타임 슬롯에 적용
        /// </summary>
        private void RefreshSlotGrowthData(SlotData_Animal slot)
        {
            if (slot == null)
                return;

            int requiredCount = LevelUpRequirementCalculator.GetRequiredDuplicateCount(slot.Level);

            slot.ApplyGrowthData(requiredCount, slot.LevelUpCost, false);
        }



        /// <summary>
        /// 도감에서 사용할 획득 여부 확인입니다.
        /// </summary>
        public bool HasAcquiredAnimal(string animalId)
        {
            return TryGetAnimalSlot(animalId, out _);
        }

        public bool TryLevelUpAnimal(string animalId)
        {
            Debug.Log("[TestInventory_Animal] 동물 레벨업 실행. 레벨업이 성공하면 슬롯 갱신.");

#region 레벨업으로 인한 동물 소모 계산 테스트 코드

            if (string.IsNullOrWhiteSpace(animalId))
            {
                Debug.LogWarning("[TestInventory_Animal] 레벨업할 AnimalId가 비어 있습니다.");
                return false;
            }

            if (!TryGetAnimalSlot(animalId, out SlotData_Animal slotData))
            {
                Debug.LogWarning( $"[TestInventory_Animal] 레벨업할 동물 슬롯을 찾지 못했습니다: {animalId}");

                return false;
            }

            if (!slotData.CanLevelUp())
            {
                Debug.Log($"[TestInventory_Animal] 레벨업 조건 부족: " +
                    $"{animalId} / " +
                    $"현재 수량 {slotData.CurrentCount} / " +
                    $"필요 수량 {slotData.RequiredUpgradeCount}");

                return false;
            }

            int previousLevel = slotData.Level;
            int previousCount = slotData.CurrentCount;

            if (!slotData.TryConsumeForLevelUp())
            {
                Debug.LogWarning($"[TestInventory_Animal] 레벨업 수량 소비에 실패했습니다: {animalId}");
                return false;
            }

            slotData.IncreaseLevel();

            // 레벨이 변경되었으므로 다음 레벨 요구 수량을 다시 계산
            RefreshSlotGrowthData(slotData);

            Debug.Log(
                $"[TestInventory_Animal] 동물 레벨업 성공: " +
                $"{animalId} / " +
                $"Lv.{previousLevel} → Lv.{slotData.Level} / " +
                $"수량 {previousCount} → {slotData.CurrentCount} / " +
                $"다음 필요 수량 {slotData.RequiredUpgradeCount}");

#endregion

            // 데이터 변경 완료 후 UI 갱신 요청
            OnAnimalChanged?.Invoke(animalId);
            return true;
        }
    }
}
