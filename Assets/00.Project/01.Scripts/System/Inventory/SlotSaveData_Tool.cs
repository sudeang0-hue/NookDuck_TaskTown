using System.Collections.Generic;
using Tool.Data;

namespace TaskTown.KDH
{
    [System.Serializable]
    public class SlotSaveData_Tool
    {
        public ToolDataSO tooldata;    // 도구 정보
        public int level;          // 현재 도구 레벨
        public int currentCount;   // 현재 보유 수량
        public bool currentSet; // 현재 도구가 배치되었는지 여부
        public bool currentAnimalSet; // 현재 도구에 동물이 배치되었는지 여부
        public string currentAnimalId; // 현재 도구에 배치된 동물의 Id

        public SlotSaveData_Tool(ToolDataSO tooldata, int level, int currentCount, bool currentSet, bool currentAnimalSet, string currentAnimalId)
        {
            this.tooldata = tooldata;
            this.level = level;
            this.currentCount = currentCount;
            this.currentSet = currentSet;
            this.currentAnimalSet = currentAnimalSet;
            this.currentAnimalId = currentAnimalId;
        }
    }

    [System.Serializable]
    public class InventorySaveData_Tool
    {
        public List<SlotSaveData_Tool> tools = new List<SlotSaveData_Tool>();
    }
}
