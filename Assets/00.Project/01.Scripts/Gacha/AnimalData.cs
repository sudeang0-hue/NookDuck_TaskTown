using UnityEngine;

namespace TaskTown.Gacha
{
    [CreateAssetMenu(menuName = "TaskTown/Gacha/Animal Data", fileName = "New AnimalData")]
    public class AnimalData : GachaEntryData
    {
        [Header("도감 정렬")]
        [SerializeField] private int dexIndex;

        [Header("난이도 전용 동물 여부")]
        [SerializeField] private bool isDifficultyExclusive;
        [SerializeField] private DifficultyType requiredDifficulty = DifficultyType.Normal;

        public int DexIndex => dexIndex;
        public bool IsDifficultyExclusive => isDifficultyExclusive;
        public DifficultyType RequiredDifficulty => requiredDifficulty;
    }
}
