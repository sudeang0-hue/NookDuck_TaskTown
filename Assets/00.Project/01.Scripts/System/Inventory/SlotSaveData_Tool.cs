using System.Collections.Generic;
using Tool.Data;

namespace TaskTown.KDH
{
    [System.Serializable]
    public class SlotSaveData_Tool
    {
        public ToolDataSO tooldata;    // ���� ����
        public int level;          // ���� ���� ����
        public int currentCount;   // ���� ���� ����
        public bool currentSet; // ���� ������ ��ġ�Ǿ��ִ��� ����
        public bool currentAnimalSet; // ���� ������ ������ ��ġ�Ǿ����� ����
        public string currentAnimalId; // ���� ������ ������ ������ Id
        // 26.07.29. KAY ����
        public bool hasRevealedSpecialAnimal; // Ưȭ ���� �̸� �ر� ����

     // public SlotSaveData_Tool(ToolDataSO tooldata, int level, int currentCount, bool currentSet, bool currentAnimalSet, string currentAnimalId)
        // {
        //     this.tooldata = tooldata;
        //     this.level = level;
        //     this.currentCount = currentCount;
        //     this.currentSet = currentSet;
        //     this.currentAnimalSet = currentAnimalSet;
        //     this.currentAnimalId = currentAnimalId;
        // }
        // 26.07.29. KAY ����
        public SlotSaveData_Tool(ToolDataSO tooldata, int level, int currentCount, bool currentSet, bool currentAnimalSet, string currentAnimalId, bool hasRevealedSpecialAnimal = false)
        {
            this.tooldata = tooldata;
            this.level = level;
            this.currentCount = currentCount;
            this.currentSet = currentSet;
            this.currentAnimalSet = currentAnimalSet;
            this.currentAnimalId = currentAnimalId;
            this.hasRevealedSpecialAnimal = hasRevealedSpecialAnimal;
        }
    }

    [System.Serializable]
    public class InventorySaveData_Tool
    {
        public List<SlotSaveData_Tool> tools = new List<SlotSaveData_Tool>();
    }
}
