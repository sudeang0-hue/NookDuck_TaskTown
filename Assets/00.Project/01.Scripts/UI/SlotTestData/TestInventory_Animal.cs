using Animal.Data;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Test.UI
{

    /// <summary>
    /// 실제 동물 인벤토리가 완성되기 전, UI 갱신을 확인하기 위한 임시 테스트 클래스
    /// </summary>
    public class TestInventory_Animal : MonoBehaviour
    {
        [Header("테스트용 보유 동물")]
        [SerializeField] private List<SlotData_Animal> animalSlots = new List<SlotData_Animal>();


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
                Debug.LogWarning(
                    "[TestInventory_Animal] 추가할 동물 데이터가 없습니다.");

                return;
            }

            if (string.IsNullOrEmpty(animalData.Id))
            {
                Debug.LogWarning(
                    "[TestInventory_Animal] 동물 ID가 비어 있습니다.");

                return;
            }

            if (amount <= 0)
            {
                Debug.LogWarning(
                    "[TestInventory_Animal] 획득 수량은 1 이상이어야 합니다.");

                return;
            }

            if (TryGetAnimalSlot(animalData.Id, out SlotData_Animal slotData))
            {
                slotData.AddCount(amount);

                OnAnimalChanged?.Invoke(animalData.Id);

                Debug.Log(
                    $"[TestInventory_Animal] 중복 동물 획득: " +
                    $"{animalData.DisplayName} / " +
                    $"현재 수량: {slotData.CurrentCount}");

                return;
            }

            SlotData_Animal newSlot = new SlotData_Animal(  animalData, 1,amount);

            animalSlots.Add(newSlot);

            OnAnimalAdded?.Invoke(animalData.Id);

            Debug.Log(
                $"[TestInventory_Animal] 신규 동물 획득: " +
                $"{animalData.DisplayName} / 수량: {amount}");
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
        /// 도감에서 사용할 획득 여부 확인입니다.
        /// </summary>
        public bool HasAcquiredAnimal(string animalId)
        {
            return TryGetAnimalSlot(animalId, out _);
        }

    }
}
