using Tool.Data;

namespace Test
{
    [System.Serializable]
    public class SlotSaveData_Tool
    {

        public ToolDataSO tooldata;    // 도구정보
        public int level;          // 현재 동물 레벨
        public int currentCount;   // 현재 보유 수량

        public SlotSaveData_Tool (ToolDataSO tooldata, int level, int currentCount)
        {
            this.tooldata = tooldata;
            this.level = level;
            this.currentCount = currentCount;
        }
    }
}