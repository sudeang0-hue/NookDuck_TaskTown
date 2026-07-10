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
    }
}
