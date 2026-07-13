using UnityEngine;

namespace TaskTown.Gacha
{
    [CreateAssetMenu(menuName = "TaskTown/Gacha/Tool Data", fileName = "New ToolData")]
    public class ToolData : GachaEntryData
    {
        [Header("생산량")]
        [SerializeField] private float baseCoinPerSecond;

        [Header("레벨 보정")]
        [Tooltip("도구 레벨 1당 생산량 증가율. 0.2 = 레벨 1당 20%씩 증가")]
        [SerializeField, Min(0f)] private float levelBonusRatePerLevel = 0.2f;

        [Header("레벨업 비용")]
        [Tooltip("레벨 1 → 2로 올릴 때 필요한 코인 비용")]
        [SerializeField, Min(0)] private long levelUpBaseCost = 100;
        [Tooltip("레벨업할 때마다 코인 비용 증가율. 1.5 = 레벨업마다 50%씩 증가")]
        [SerializeField, Min(1f)] private float levelUpCostIncreaseRate = 1.5f;

        [Header("특화 동물")]
        [SerializeField] private string specialAnimalId;
        [SerializeField, Range(0f, 5f)] private float specialAnimalBonusRate;

        [Header("데스크 타운 표시용")]
        [SerializeField] private GameObject toolPrefab;

        public float BaseCoinPerSecond => baseCoinPerSecond;
        public string SpecialAnimalId => specialAnimalId;
        public float SpecialAnimalBonusRate => specialAnimalBonusRate;
        public GameObject ToolPrefab => toolPrefab;

        // 도구 레벨에 따른 생산량 배율입니다. 레벨 1이면 배율 1(보정 없음).
        public float CalculateLevelMultiplier(int toolLevel)
        {
            return 1f + Mathf.Max(0, toolLevel - 1) * levelBonusRatePerLevel;
        }

        // currentLevel에서 다음 레벨로 올리는 데 필요한 코인 비용입니다.
        public long CalculateLevelUpCoinCost(int currentLevel)
        {
            double cost = levelUpBaseCost * System.Math.Pow(levelUpCostIncreaseRate, Mathf.Max(0, currentLevel - 1));
            return (long)System.Math.Ceiling(cost);
        }
    }
}
