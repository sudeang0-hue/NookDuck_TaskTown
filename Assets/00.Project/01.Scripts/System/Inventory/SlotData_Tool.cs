using Tool.Data;
using UnityEngine;

namespace TaskTown.KDH
{
    [System.Serializable]
    public class SlotData_Tool
    {
        [Header("저장 데이터")]
        [SerializeField] private ToolDataSO toolData;
        [SerializeField] private int level = 1;
        [SerializeField] private int currentCount = 0;
        [SerializeField] private bool currentSet = false;
        [SerializeField] private bool currentAnimalSet = false;
        [SerializeField] private string currentAnimalId = null;

        [Header("레벨업 계산 데이터")]
        [SerializeField] private bool isMaxLevel;
        [SerializeField] private int levelUpCost;


        // TODO : 총 생산량 ///  (동물 + 도구) * 특화 * 난이도 + (마을 업그레이드?)

        public ToolDataSO ToolData => toolData;
        public string ToolId => toolData != null ? toolData.Id : string.Empty;
        public int Level => level;
        public int CurrentCount => currentCount;
        public bool CurrentSet => currentSet;
        public bool CurrentAnimalSet => currentAnimalSet;
        public string CurrentAnimalId => currentAnimalId != null ? currentAnimalId : string.Empty;
        public bool IsMaxLevel => isMaxLevel;
        public int LevelUpCost => levelUpCost;


        public SlotData_Tool(ToolDataSO toolData, int level, int currentCount, bool currentSet, bool currentAnimalSet, string currentAnimalId)
        {
            this.toolData = toolData;
            this.level = Mathf.Max(1, level);
            this.currentCount = Mathf.Max(1, currentCount);
            this.currentSet = currentSet;
            this.currentAnimalSet = currentAnimalSet;
            this.currentAnimalId = currentAnimalId;
        }

        /// <summary>
        /// 동일한 도구를 획득했을 때 보유 수량을 증가.
        /// </summary>
        public void AddCount()
        {
            currentCount++;
        }

        /// <summary>
        /// 도구 레벨을 1 증가시킵니다.
        /// </summary>
        public void ToolLevelUp()
        {
            if (IsMaxLevel) return;

            level++;

            if (level == 5) isMaxLevel = true;
        }

        /// <summary>
        /// 도구 배치 상태를 변경합니다.
        /// </summary>
        public void SetPlaced(bool isPlaced)
        {
            currentSet = isPlaced;
        }

        /// <summary>
        /// 도구에 동물을 배치합니다.
        /// </summary>
        public void SetAssignedAnimal(string animalId)
        {
            if (string.IsNullOrWhiteSpace(animalId))
            {
                ClearAssignedAnimal();
                return;
            }

            currentAnimalSet = true;
            currentAnimalId = animalId;
        }

        /// <summary>
        /// 도구에 배치된 동물을 제거합니다.
        /// </summary>
        public void ClearAssignedAnimal()
        {
            currentAnimalSet = false;
            currentAnimalId = null;
        }

        // 임시 기능(대체 가능)
        /// <summary>
        /// 레벨업에 필요한 코스트를 구합니다.
        /// </summary>
        public int GetLevelUpCost()
        {
            if (level <= 0) return 99999;

            int neededCost;

            if (level == 1) neededCost = 4;
            else if (level == 2) neededCost = 16;
            else if (level == 3) neededCost = 64;
            else if (level == 4) neededCost = 256;
            else neededCost = 99999;

            levelUpCost = neededCost;



            return levelUpCost;
        }


    }
}
