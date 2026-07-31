using System.Collections.Generic;
using System.Text;
using Animal.Data;
using TaskTown.Gacha;
using UnityEngine;
/// <summary>
/// 가챠 → 동물 인벤토리 연동 테스트용 스크립트.
/// 씬에 InventoryManager_Animal, AnimalGachaManager가 있어야 합니다.
/// </summary>
namespace TaskTown.KDH
{
    public class Test_KDH_gacha : MonoBehaviour
    {
        [Header("가챠 (선택)")]
        [SerializeField] private AnimalGachaManager animalGachaManager;
        [Header("수동 테스트용 동물 데이터")]
        [SerializeField] private AnimalDataSO testAnimal01;
        [SerializeField] private AnimalDataSO testAnimal02;
        [Header("레벨업 테스트용 중복 추가 횟수")]
        [SerializeField] private int duplicateAddCount = 4;

        [SerializeField] private InventoryManager_Animal inventory;
        private void Awake()
        {
            if (inventory == null)
            {
                Debug.LogError("[Test_KDH_gacha] InventoryManager_Animal.Instance가 없습니다. 씬에 매니저를 배치하세요.");
                return;
            }
            // 인벤토리 변경 이벤트 구독 (UIController_AnimalInvPage 연동 전 로그 확인용)
            inventory.OnAnimalInventoryChanged += HandleInventoryChanged;
            inventory.OnAnimalSlotChanged += HandleSlotChanged;
            if (animalGachaManager != null)
            {
                animalGachaManager.OnGachaResolved += HandleGachaResolved;
            }
        }
        private void OnDestroy()
        {
            if (inventory != null)
            {
                inventory.OnAnimalInventoryChanged -= HandleInventoryChanged;
                inventory.OnAnimalSlotChanged -= HandleSlotChanged;
            }
            if (animalGachaManager != null)
            {
                animalGachaManager.OnGachaResolved -= HandleGachaResolved;
            }
        }
        private void Start()
        {
            PrintKeyGuide();
            PrintInventoryState("Start");
        }
        private void Update()
        {
            if (inventory == null) return;
            // G: 가챠 1회 (AnimalGachaManager 연결 시)
            if (Input.GetKeyDown(KeyCode.G))
            {
                RollGachaOnce();
            }
            // 1: testAnimal01 추가
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                TryAddAnimal(testAnimal01);
            }
            // 2: testAnimal02 추가
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                TryAddAnimal(testAnimal02);
            }
            // D: 현재 선택 동물01 중복 추가 (레벨업 재료 테스트)
            if (Input.GetKeyDown(KeyCode.D))
            {
                AddDuplicates(testAnimal01, duplicateAddCount);
            }
            // L: testAnimal01 레벨업 시도
            if (Input.GetKeyDown(KeyCode.L))
            {
                TryLevelUp(testAnimal01);
            }
            // P: 인벤토리 상태 출력
            if (Input.GetKeyDown(KeyCode.P))
            {
                PrintInventoryState("Manual Print");
            }
            // S: 저장 데이터 생성 후 로드 (세이브/로드 테스트)
            if (Input.GetKeyDown(KeyCode.S))
            {
                TestSaveAndLoad();
            }
            // C: 인벤토리 초기화
            if (Input.GetKeyDown(KeyCode.C))
            {
                inventory.ClearAnimalInventory();
                Debug.Log("[Test_KDH_gacha] 인벤토리 초기화");
            }
        }
        /// <summary>
        /// 가챠 결과를 인벤토리에 반영하는 핵심 연동 지점
        /// </summary>
        private void HandleGachaResolved(GachaResult result)
        {
            if (result.Entry is not AnimalDataSO animalData)
            {
                Debug.LogWarning($"[Test_KDH_gacha] AnimalDataSO가 아닌 결과: {result.Entry?.GetType().Name}");
                return;
            }
            bool added = inventory.AddAnimalSlot(animalData);
            Debug.Log($"[Test_KDH_gacha] 가챠 결과 인벤 추가: {animalData.DisplayName} / 성공={added}");
            PrintInventoryState("After Gacha");
        }
        private void RollGachaOnce()
        {
            if (animalGachaManager == null)
            {
                Debug.LogWarning("[Test_KDH_gacha] AnimalGachaManager가 연결되지 않았습니다.");
                return;
            }
            GachaResult result = animalGachaManager.Roll();
            // Roll() 내부에서 OnGachaResolved도 호출되므로 HandleGachaResolved가 실행됩니다.
            Debug.Log($"[Test_KDH_gacha] 가챠 실행: [{result.Grade}] {result.Entry?.DisplayName}");
        }
        private void TryAddAnimal(AnimalDataSO animalData)
        {
            if (animalData == null)
            {
                Debug.LogWarning("[Test_KDH_gacha] AnimalDataSO가 비어 있습니다.");
                return;
            }
            bool success = inventory.AddAnimalSlot(animalData);
            Debug.Log($"[Test_KDH_gacha] AddAnimalSlot: {animalData.DisplayName} / 성공={success} / 수량={inventory.GetAnimalCount(animalData.Id)}");
        }
        private void AddDuplicates(AnimalDataSO animalData, int count)
        {
            if (animalData == null) return;
            for (int i = 0; i < count; i++)
            {
                inventory.AddAnimalSlot(animalData);
            }
            Debug.Log($"[Test_KDH_gacha] {animalData.DisplayName} 중복 {count}회 추가 / 현재 수량={inventory.GetAnimalCount(animalData.Id)}");
        }
        private void TryLevelUp(AnimalDataSO animalData)
        {
            if (animalData == null) return;
            string id = animalData.Id;
            bool canLevelUp = inventory.CanLevelUpAnimal(id);
            Debug.Log($"[Test_KDH_gacha] CanLevelUp({id}) = {canLevelUp}");
            if (!canLevelUp) return;
            bool leveled = inventory.TryLevelUpAnimal(id);
            Debug.Log($"[Test_KDH_gacha] TryLevelUp({id}) = {leveled}");
            PrintInventoryState("After LevelUp");
        }
        /// <summary>
        /// CreateSaveData → LoadSaveData 왕복 테스트
        /// </summary>
        private void TestSaveAndLoad()
        {
            List<SlotSaveData_Animal> saveData = inventory.CreateSaveData();
            Debug.Log($"[Test_KDH_gacha] 저장 데이터 생성: {saveData.Count}슬롯");
            inventory.ClearAnimalInventory();
            inventory.LoadSaveData(saveData);
            Debug.Log("[Test_KDH_gacha] LoadSaveData 완료");
            PrintInventoryState("After Save/Load");
        }
        private void HandleInventoryChanged()
        {
            Debug.Log("[Test_KDH_gacha] OnAnimalInventoryChanged 발생");
        }
        private void HandleSlotChanged(SlotData_Animal slot)
        {
            Debug.Log($"[Test_KDH_gacha] OnAnimalSlotChanged: {slot.AnimalData.DisplayName} Lv.{slot.Level} x{slot.CurrentCount}");
        }
        private void PrintInventoryState(string tag)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"===== Inventory [{tag}] =====");
            IReadOnlyList<SlotData_Animal> slots = inventory.AnimalSlotsList;
            if (slots.Count == 0)
            {
                sb.AppendLine("(비어 있음)");
            }
            else
            {
                for (int i = 0; i < slots.Count; i++)
                {
                    SlotData_Animal slot = slots[i];
                    sb.AppendLine(
                        $"[{i}] {slot.AnimalData.DisplayName} (Id:{slot.AnimalId}) " +
                        $"Lv.{slot.Level} x{slot.CurrentCount} " +
                        $"Max={slot.IsMaxLevel} NextCost={slot.GetLevelUpCost()}");
                }
            }
            Debug.Log(sb.ToString());
        }
        private void PrintKeyGuide()
        {
            Debug.Log(
                "[Test_KDH_gacha] 키 가이드\n" +
                "G: 가챠 1회\n" +
                "1/2: testAnimal01/02 추가\n" +
                "D: testAnimal01 중복 추가\n" +
                "L: testAnimal01 레벨업\n" +
                "P: 인벤 출력\n" +
                "S: Save/Load 테스트\n" +
                "C: 인벤 초기화");
        }
    }
}