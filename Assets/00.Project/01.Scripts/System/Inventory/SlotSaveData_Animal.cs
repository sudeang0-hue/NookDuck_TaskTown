using System.Collections.Generic;
using Animal.Data;

namespace TaskTown.KDH
{
    [System.Serializable]
    public class SlotSaveData_Animal
    {
        public AnimalDataSO animaldata;    // 동물 정보
        public int level;          // 현재 동물 레벨
        public int currentCount;   // 현재 보유 수량

        public SlotSaveData_Animal(AnimalDataSO animaldata, int level, int currentCount)
        {
            this.animaldata = animaldata;
            this.level = level;
            this.currentCount = currentCount;
        }
    }

    [System.Serializable]
    public class InventorySaveData_Animal
    {
        public List<SlotSaveData_Animal> animals = new List<SlotSaveData_Animal>();
    }
}
