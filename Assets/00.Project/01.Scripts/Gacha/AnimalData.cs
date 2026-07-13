using UnityEngine;

namespace TaskTown.Gacha
{
    [CreateAssetMenu(menuName = "TaskTown/Gacha/Animal Data", fileName = "New AnimalData")]
    public class AnimalData : GachaEntryData
    {
        [Header("생산량")]
        [SerializeField] private float baseCoinPerSecond = 1f;

        [Header("레벨 보정")]
        [Tooltip("동물 레벨 1당 생산량 증가율. 0.2 = 레벨 1당 20%씩 증가")]
        [SerializeField, Min(0f)] private float levelBonusRatePerLevel = 0.2f;

        [Header("레벨업 비용")]
        [Tooltip("레벨 1 → 2로 올릴 때 필요한 코인 비용")]
        [SerializeField, Min(0)] private long levelUpBaseCost = 100;
        [Tooltip("레벨업할 때마다 코인 비용 증가율. 1.5 = 레벨업마다 50%씩 증가")]
        [SerializeField, Min(1f)] private float levelUpCostIncreaseRate = 1.5f;

        [Header("도감 정렬")]
        [SerializeField] private int dexIndex;

        [Header("난이도 전용 동물 여부")]
        [SerializeField] private bool isDifficultyExclusive;
        [SerializeField] private DifficultyType requiredDifficulty = DifficultyType.Normal;

        public float BaseCoinPerSecond => baseCoinPerSecond;
        public int DexIndex => dexIndex;
        public bool IsDifficultyExclusive => isDifficultyExclusive;
        public DifficultyType RequiredDifficulty => requiredDifficulty;

        // 동물 레벨에 따른 생산량 배율입니다. 레벨 1이면 배율 1(보정 없음).
        public float CalculateLevelMultiplier(int animalLevel)
        {
            return 1f + Mathf.Max(0, animalLevel - 1) * levelBonusRatePerLevel;
        }

        // currentLevel에서 다음 레벨로 올리는 데 필요한 코인 비용입니다.
        public long CalculateLevelUpCoinCost(int currentLevel)
        {
            double cost = levelUpBaseCost * System.Math.Pow(levelUpCostIncreaseRate, Mathf.Max(0, currentLevel - 1));
            return (long)System.Math.Ceiling(cost);
        }
    }
}
