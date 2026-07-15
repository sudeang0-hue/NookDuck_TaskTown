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

        //인벤토리 최대 공간 칸 수
        // public int maxSlots = 999;


        [Header("동물 런타임 슬롯")]
        [Tooltip("현재 플레이어가 보유한 동물 슬롯 목록")]
        [SerializeField] private List<SlotData_Animal> animalSlotsList = new List<SlotData_Animal>();

        [Header("동물 데이터 베이스")]
        [SerializeField] private AnimalDatabase animalDatabase;

        // ID 기반 빠른 조회를 위한 런타임 Dictionary
        private Dictionary<string, SlotData_Animal> animalSlotsDic = new Dictionary<string, SlotData_Animal>();

        // 외부에서 인벤토리 동물 목록을 읽을 수 있도록 제공하는 프로퍼티
        public IReadOnlyList<SlotData_Animal> AnimalSlotsList => animalSlotsList;

        // 동물 인벤토리 데이터가 변경되었을때 호출
        public event Action OnAnimalInventoryChanged;
        // 특정 동물 슬롯의 데이터가 변경되었을때 호출
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
        /// 런타임 리스트 기반으로 Dictionary 를 다시 생성
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

                // 성장 수치 계산 시스템 연결
                RefreshSlotGrowthData(slot);
            }
        }

        /// <summary>
        /// 동물 ID 에 해당하는 고정 데이터 반환
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
        /// 동물 슬롯 추가. 최초 획득이면 슬롯을 만들고, 중복 획득이면 기존 슬롯 수량 증가
        /// </summary>
        public bool AddAnimalSlot(AnimalDataSO animalData)   // 가챠로 나온 결과를 하나씩 넣는다면 굳이 amount를 생각할 필요가 없을것 같아 수정함
        {
            if (animalData == null)
            {
                Debug.LogWarning("[InventroyManager_Animal] 추가할 AnimalDataSO가 없습니다.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(animalData.Id))
            {
                Debug.LogWarning($"[InventroyManager_Animal] {animalData.DisplayName} 의 Id 가 비어있습니다.");
                return false;
            }

            //if (!CanReceiveAnimal(AnimalDataSO))
            //{
            //    Debug.Log("인벤토리가 가득 차서 동물이 들어올 수 없습니다.");
            //    return false;
            //}

            if (animalSlotsDic.TryGetValue(animalData.Id, out SlotData_Animal existingSlot))
            {
                existingSlot.AddCount();

                // 성창 수치 계산 시스템 연결
                NotifySlotChanged(existingSlot);

                Debug.Log($"[InventoryManager_Animal] 중복 동물 획득:" +
                    $"{animalData.DisplayName} + 1 / 현재 수량: {existingSlot.CurrentCount}");

                return true;
            }

            SlotData_Animal newSlot = new SlotData_Animal(animalData, level: 1, currentCount: 1);

            RefreshSlotGrowthData(newSlot);

            animalSlotsList.Add(newSlot);
            animalSlotsDic.Add(animalData.Id, newSlot);

            NotifySlotChanged(newSlot);

            Debug.Log($"[InventoryManager_Animal] 새로운 동물 획득: {animalData.DisplayName}");

            return true;

            /*
            // 기존 슬롯 중 똑같은 아이템이 있는지 찾아서 채웁니다.
            //AnimalInventorySlot existingSlot = animalList.Find(slot => slot.gachaData == animalData);
            //
            //if (existingSlot != null)
            //{
            //    existingSlot.currentCount += amount;
            //}
            //else // 기존 슬롯이 없다면 새 슬롯을 만듭니다.
            //{
            //    animalList.Add(new AnimalInventorySlot(animalData, amount));
            //}

            // 아이템이 추가되었음을 UI 등에 알리는 이벤트를 여기에 넣을 수 있습니다.
            //Debug.Log($"{animalData.DisplayName}이(가) 총 {amount}가 인벤토리에 추가되었습니다.");
            //return true;
            */
        }

        /*
        //위에 maxSlots 주석 참조 
        //public bool CanReceiveItem(AnimalData targetData)
        //{
        //    if (animalList.Count < maxSlots) return true;
        //
        //    return false;
        //}
        */

        /// <summary>
        /// 동물 ID로 런타임 슬롯 조화
        /// </summary>
        public bool TryGetAnimalSlot(string animalId, out SlotData_Animal slot)
        {
            slot = null;

            if (string.IsNullOrWhiteSpace(animalId)) return false;

            return animalSlotsDic.TryGetValue(animalId, out slot);
        }

        /// <summary>
        /// 현제 보유중인 동물의 총 수량 반환
        /// 보유하지 않은 동물은 0 반환 
        /// </summary>
        public int GetAnimalCount(string animalId)
        {
            return TryGetAnimalSlot(animalId, out SlotData_Animal slot) ? slot.CurrentCount : 0;
        }

        /// <summary>
        /// 해당 동물을 한 번 이상 획득했는지 확인.
        /// 도감에서는 이 값을 해금 여부로 사용.
        /// </summary>
        private bool IsAnimalUnlocked(string animalId)
        {
            return TryGetAnimalSlot(animalId, out _);
        }

        /// <summary>
        /// 동물이 현재 레벨업 가능한지 확인
        /// </summary>
        public bool CanLevelUpAnimal(string animalId)
        {
            if (!TryGetAnimalSlot(animalId, out SlotData_Animal slot)) return false;

            if (slot.IsMaxLevel) return false;

            int neededAmount = slot.GetLevelUpCost();

            if (neededAmount <= 0)
            {
                Debug.Log("레벨업 요구 수량이 0이하 입니다.");
                return false;
            }

            if (slot.CurrentCount < neededAmount) // 예: 렙업에 4마리가 필요하면 기본 1마리에 중복 4마리를 더해 5마리 필요
            {
                Debug.Log($"[InventoryManager_Animal] 레벨업 요구 수량이 부족합니다. 필요 수량: {neededAmount + 1 - slot.CurrentCount}");
                return false;
            }

            return true;
        }

        public bool TryLevelUpAnimal(string animalId)
        {
            if (!TryGetAnimalSlot(animalId, out SlotData_Animal slot))
            {
                Debug.LogWarning($"[InventoryManager_Animal] 보유하지 않은 동물입니다: {animalId}");
                return false;
            }

            if (slot.IsMaxLevel)
            {
                Debug.Log($"[InventoryManager_Animal] 이미 최대 레벨인 동물입니다: {animalId}");
                return false;
            }

            //if (!slot.TryConsumeCount())
            //{
            //    int materialCount = Mathf.Max(0, slot.CurrentCount - 1);
            //
            //    Debug.Log($"[InventoryManager_Animal]" +
            //        $"레벨업 재료 부족: {animalId} / 보유 수량 {materialCount} / 필요 수량 {requiredCount}");
            //
            //    return false;
            //}

            if (CanLevelUpAnimal(animalId) == true) slot.AnimalLevelUp();
            else return false;

            // 성장 수치 계산 시스템 연결
            RefreshSlotGrowthData(slot);

            NotifySlotChanged(slot);

            Debug.Log($"[InventoryManager_Animal] 동물 레벨업 성공: {animalId} / 현재 레벨 {slot.Level}");

            return true;
        }

        /*
// 동물을 합성할 때 사용할 함수
//public void RemoveAnimal(AnimalData animalData, int amount)
//{
//    if (animalData == null || amount <= 0) return;
//
//    AnimalInventorySlot targetSlot = animalList.Find(slot => slot.gachaData == animalData);
//
//    if (targetSlot != null)
//    {
//        //소모될 동물을 제외한 하나는 남아있어야 해서 -1을 함
//        if (targetSlot.currentCount - 1 < amount)
//        {
//            Debug.Log($"인벤토리에서 합성하려는 {animalData.DisplayName}의 갯수가 {amount - (targetSlot.currentCount - 1)}만큼 부족합니다.");
//            return;
//        }
//
//        targetSlot.currentCount -= amount;
//    }
//    else
//    {
//        Debug.LogWarning($"인벤토리에 합성하려는 {animalData.DisplayName}이(가) 존재하지 않습니다.");
//    }
//}
*/


        /// <summary>
        /// 현재 런타임 동물 데이터를 저장용 슬롯 목록으로 변환합니다.
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

                // 성장 수치 계산 시스템 연결
                // RefreshSlotGrowthData(runtimeSlot);

                animalSlotsList.Add(runtimeSlot);
                animalSlotsDic.Add(runtimeSlot.AnimalId, runtimeSlot);
            }

            NotifyInventoryChanged();
        }

        /// <summary>
        /// 현재 레벨을 기반으로 레벨업 요구 수량과 비용 갱신.
        /// 성장 수치 계산 시스템이 구현되면 연결합니다.
        /// </summary>
        private void RefreshSlotGrowthData(SlotData_Animal slot)
        {
            if (slot == null)
                return;
            
            int requiredCount = LevelUpRequirementCalculator.GetRequiredDuplicateCount(slot.Level);

            slot.ApplyGrowthData(requiredCount, slot.LevelUpCost, false);

            Debug.Log("현재 레벨을 기반으로 레벨업 요구 수량과 비용을 갱신");
        }


        /// <summary>
        /// 모든 동물 런타임 데이터를 제거합니다.
        /// 새 게임 또는 저장 데이터 로드 전에 사용할 수 있습니다.
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
    }
}
