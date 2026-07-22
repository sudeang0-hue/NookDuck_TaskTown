using System.Collections.Generic;

namespace TaskTown.KDH
{
    // 디스크(JSON)에 저장하기 위한 게임 진행 데이터입니다. 인벤토리 슬롯이 들고 있는
    // ScriptableObject(AnimalDataSO/ToolDataSO)는 세션 간 JSON 직렬화가 안 되므로,
    // 여기서는 문자열 ID만 저장하고 로드 시 데이터베이스에서 다시 조회합니다.
    [System.Serializable]
    public class GameSaveData
    {
        public long coins;
        public int townLevel = 1;
        public List<AnimalSaveEntry> animals = new List<AnimalSaveEntry>();
        public List<ToolSaveEntry> tools = new List<ToolSaveEntry>();
    }

    [System.Serializable]
    public class AnimalSaveEntry
    {
        public string id;
        public int level;
        public int count;
    }

    [System.Serializable]
    public class ToolSaveEntry
    {
        public string id;
        public int level;
        public int count;
        public bool currentSet;
        public bool currentAnimalSet;
        public string currentAnimalId;
    }
}
