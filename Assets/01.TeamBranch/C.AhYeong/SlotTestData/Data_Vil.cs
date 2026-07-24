using TaskTown.Gacha;
using TaskTown.KDH;
using UnityEngine;


namespace Test
{

    [System.Serializable]
    
    public class Data_Vil
    {
        public int level;          // 현재 마을 레벨
        public InventoryManager_Animal animal; // 보유 동물
        public InventoryManager_Tool tool;  // 보유 도구
        public DifficultyType difficulty; // 난이도 진행도
        public Time clearTimeSec; // 엔딩까지 걸린 시간


        //public Data_Vil(ToolDataSO tooldata, int level, int currentCount)
        //{
        //    this.tooldata = tooldata;
        //    this.level = level;
        //    this.currentCount = currentCount;
        //}

    }

}
