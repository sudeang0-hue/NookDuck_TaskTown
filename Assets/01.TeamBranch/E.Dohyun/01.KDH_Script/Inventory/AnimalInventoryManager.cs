namespace TaskTown.KDH
{
    using TaskTown.Gacha;
    using System.Collections.Generic;
    using UnityEngine;

    public class AnimalInventoryManager : MonoBehaviour
    {
        public static AnimalInventoryManager Instance { get; private set; }

        //인벤토리 최대 공간 칸 수
        // public int maxSlots = 99;

        // 실제 동물 데이터를 담아둘 리스트
        private List<AnimalInventorySlot> animalList = new List<AnimalInventorySlot>();

        // 외부에서 인벤토리 동물 목록을 읽을 수 있도록 제공하는 프로퍼티
        public IReadOnlyList<AnimalInventorySlot> AnimalList => animalList;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        //동물 추가 함수
        public bool AddAnimal(GachaEntryData animalData, int amount)
        {
            if (animalData == null) return false;

            //위에 maxSlots 주석 참조 
            //if (!CanReceiveItem(animalData))
            //{
            //    Debug.Log("인벤토리가 가득 차서 동물이 들어올 수 없습니다.");
            //    return false;
            //}

            // 기존 슬롯 중 똑같은 아이템이 있는지 찾아서 채웁니다.
            AnimalInventorySlot existingSlot = animalList.Find(slot => slot.gachaData == animalData);

            if (existingSlot != null)
            {
                existingSlot.currentCount += amount;
            }
            else // 기존 슬롯이 없다면 새 슬롯을 만듭니다.
            {
                animalList.Add(new AnimalInventorySlot(animalData, amount));
            }


            // 아이템이 추가되었음을 UI 등에 알리는 이벤트를 여기에 넣을 수 있습니다.
            Debug.Log($"{animalData.DisplayName}이(가) 총 {amount}가 인벤토리에 추가되었습니다.");
            return true;
        }

        //위에 maxSlots 주석 참조 
        //public bool CanReceiveItem(AnimalData targetData)
        //{
        //    if (animalList.Count < maxSlots) return true;
        //
        //    return false;
        //}

        // 동물을 합성할 때 사용할 함수
        public void RemoveAnimal(AnimalData animalData, int amount)
        {
            if (animalData == null || amount <= 0) return;

            AnimalInventorySlot targetSlot = animalList.Find(slot => slot.gachaData == animalData);

            if (targetSlot != null)
            {
                //소모될 동물을 제외한 하나는 남아있어야 해서 -1을 함
                if (targetSlot.currentCount - 1 < amount)
                {
                    Debug.Log($"인벤토리에서 합성하려는 {animalData.DisplayName}의 갯수가 {amount - (targetSlot.currentCount - 1)}만큼 부족합니다.");
                    return;
                }

                targetSlot.currentCount -= amount;
            }
            else
            {
                Debug.LogWarning($"인벤토리에 합성하려는 {animalData.DisplayName}이(가) 존재하지 않습니다.");
            }
        }
    }
}
