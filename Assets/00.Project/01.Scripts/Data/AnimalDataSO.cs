using TaskTown.Gacha;
using UnityEngine;


namespace Animal.Data
{
    // 생산량/레벨업/해금 레벨은 공통 베이스(GachaEntryData)에서 상속받습니다.
    [CreateAssetMenu(menuName = "Inventory/Animal/Animal Data SO", fileName = "AnimalDataSO")]
    public class AnimalDataSO : GachaEntryData, IDifficultyGated
    {
        [Header("도감 설명")]
        [TextArea(3, 5)]
        [SerializeField] private string animalDescription;

        [Header("정렬 순서")]
        [SerializeField] private int dexIndex;

        [Header("난이도 전용 여부")]
        [SerializeField] private bool isDifficultyExclusive;
        [SerializeField] private DifficultyType requiredDifficulty = DifficultyType.Normal;


        public string AnimalDescription => animalDescription;
        public int DexIndex => dexIndex;
        public bool IsDifficultyExclusive => isDifficultyExclusive;
        public DifficultyType RequiredDifficulty => requiredDifficulty;
    }
}
