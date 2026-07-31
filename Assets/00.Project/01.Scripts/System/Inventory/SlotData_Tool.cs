using System;
using TaskTown.Gacha;
using Tool.Data;
using UnityEngine;

namespace TaskTown.KDH
{
    [System.Serializable]
    public class SlotData_Tool
    {
        [Header("도구 슬롯 정보")]
        [SerializeField] private ToolDataSO toolData;
        [SerializeField] private int level = 1;
        [SerializeField] private int currentCount = 0;
        [SerializeField] private bool currentSet = false;
        [SerializeField] private bool currentAnimalSet = false;
        [SerializeField] private string currentAnimalId = null;
        // 26.07.29. KAY 수정
        [SerializeField] private bool hasRevealedSpecialAnimal = false;

        [Header("레벨업 요구 데이터")]
        [SerializeField] private bool isMaxLevel;
        [SerializeField] private int levelUpCost;
        [SerializeField] private int requiredUpgradeCount;


        // TODO : 최종 생산량 ///  (동물 + 도구) * 특화 * 난이도 + (마을 업그레이드?)

        public ToolDataSO ToolData => toolData;
        public string ToolId => toolData != null ? toolData.Id : string.Empty;
        public int Level => level;
        public int CurrentCount => currentCount;
        public bool CurrentSet => currentSet;
        public bool CurrentAnimalSet => currentAnimalSet;
        public string CurrentAnimalId => currentAnimalId != null ? currentAnimalId : string.Empty;
        // 26.07.29. KAY 수정
        public bool HasRevealedSpecialAnimal => hasRevealedSpecialAnimal;
        public bool IsMaxLevel => isMaxLevel;
        public int LevelUpCost => levelUpCost;
        public int RequiredUpgradeCount => requiredUpgradeCount;


        // public SlotData_Tool(ToolDataSO toolData, int level, int currentCount, bool currentSet, bool currentAnimalSet, string currentAnimalId)
        // {
        //     this.toolData = toolData;
        //     this.level = Mathf.Max(1, level);
        //     this.currentCount = Mathf.Max(1, currentCount);
        //     this.currentSet = currentSet;
        //     this.currentAnimalSet = currentAnimalSet;
        //     this.currentAnimalId = currentAnimalId;
        // }
        // 26.07.29. KAY 수정
        public SlotData_Tool(ToolDataSO toolData, int level, int currentCount, bool currentSet, bool currentAnimalSet, string currentAnimalId, bool hasRevealedSpecialAnimal = false)
        {
            this.toolData = toolData;
            this.level = Mathf.Max(1, level);
            this.currentCount = Mathf.Max(1, currentCount);
            this.currentSet = currentSet;
            this.currentAnimalSet = currentAnimalSet;
            this.currentAnimalId = currentAnimalId;
            this.hasRevealedSpecialAnimal = hasRevealedSpecialAnimal;
        }

        /// <summary>
        /// 도구 슬롯을 획득했을 때 개수를 늘립니다.
        /// </summary>
        public void AddCount()
        {
            currentCount++;
        }

        /// <summary>
        /// 도구 레벨을 1 증가시킵니다.
        /// endlessMode가 true면(#19 엔드리스 모드) 레벨 5 상한을 무시하고, 이전에 이미 상한에
        /// 도달해 있던 슬롯도 다시 풀어줍니다.
        /// </summary>
        public void ToolLevelUp(bool endlessMode = false)
        {
            if (!endlessMode && IsMaxLevel) return;

            level++;

            if (endlessMode) isMaxLevel = false;
            else if (level == 5) isMaxLevel = true;
        }

        /// <summary>
        /// 도구 배치 상태를 설정합니다.
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
        /// 도구에 배치된 동물을 해제합니다.
        /// </summary>
        public void ClearAssignedAnimal()
        {
            currentAnimalSet = false;
            currentAnimalId = null;
        }

        /// <summary>
        /// 특화 동물에게 1회 이상 장착되어 이름이 해금된 상태로 표시합니다.
        /// </summary>
        // 26.07.29. KAY 수정
        public void RevealSpecialAnimal()
        {
            hasRevealedSpecialAnimal = true;
        }

        /// <summary>
        /// 다음 레벨로 가기 위해 이번 단계에서 필요한 중복 개수입니다 (표시용).
        /// LevelUpRequirementCalculator(TaskTown.Gacha) 기준값을 그대로 씁니다.
        /// </summary>
        public int GetLevelUpCost()
        {
            int neededCost = LevelUpRequirementCalculator.GetRequiredDuplicateCount(level);
            levelUpCost = neededCost;

            return levelUpCost;
        }

        /// <summary>
        /// 요구 개수, 레벨업 비용, 최대 레벨 여부를 설정합니다.
        /// </summary>
        public void ApplyGrowthData(int requiredCount, int cost, bool maxLevel)
        {
            requiredUpgradeCount = Mathf.Max(0, requiredCount);
            levelUpCost = Math.Max(0, cost);
            isMaxLevel = maxLevel;
        }

        /// <summary>
        /// 레벨업 가능 여부.
        /// 본체 1개를 제외한 나머지 개수가 요구 개수 이상인지 확인합니다.
        /// endlessMode가 true면(#19) 레벨 5 상한 체크를 건너뜁니다.
        /// </summary>
        public bool CanLevelUp(bool endlessMode = false)
        {
            if (!endlessMode && isMaxLevel)
                return false;

            if (requiredUpgradeCount <= 0)
                return false;

            // UIController_AnimalInvPage 표시 개수(CurrentCount - 1)가 요구 개수를 충족했는지 판정
            return (currentCount - 1) >= requiredUpgradeCount;
        }

        /// <summary>
        /// 레벨업에 필요한 개수를 소비합니다.
        /// 본체 1개는 남기고, 요구 개수만큼만 소모합니다.
        /// </summary>
        public bool TryConsumeForLevelUp(bool endlessMode = false)
        {
            if (!CanLevelUp(endlessMode))
                return false;

            currentCount -= requiredUpgradeCount;

            // 본체 1개는 항상 유지
            if (currentCount < 1)
                currentCount = 1;

            return true;
        }

        /// <summary>
        /// 다음 레벨로 올리는 데 필요한 코인 비용입니다 (GachaEntryData 공통 계산식 기준).
        /// </summary>
        public long GetLevelUpCoinCost()
        {
            return toolData != null ? toolData.CalculateLevelUpCoinCost(level) : 0L;
        }

        /// <summary>
        /// 디버그 전용: 재료/코인 없이 레벨을 바로 설정합니다. (최대 5)    26.07.24 KDH 추가
        /// </summary>
        public void DebugSetLevel(int targetLevel)
        {
            level = Mathf.Clamp(targetLevel, 1, 5);
            isMaxLevel = level >= 5;
        }
    }
}
