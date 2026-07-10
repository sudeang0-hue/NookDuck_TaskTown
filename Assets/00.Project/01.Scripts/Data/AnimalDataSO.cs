using TaskTown.Gacha;
using UnityEngine;


namespace Animal.Data
{

    [CreateAssetMenu(menuName = "Inventory/Animal/Animal Data SO", fileName = "AnimalDataSO")]
    public class AnimalDataSO : GachaEntryData
    {
        [Header("동물 해금 레벨")]
        [SerializeField] private int unlockLevel;  // 동물 해금 레벨

        [Header("동물 도감 설명글")]
        [TextArea(3, 5)]
        [SerializeField] private string animalDescription;  // 동물 도감 설명글

        [Header("도감 정렬")]
        [SerializeField] private int dexIndex;

        [Header("난이도 전용 동물 여부")]
        [SerializeField] private bool isDifficultyExclusive;
        [SerializeField] private DifficultyType requiredDifficulty = DifficultyType.Normal;


        public int UnlockLevel => unlockLevel;
        public string AnimalDescription => animalDescription;
        public int DexIndex => dexIndex;
        public bool IsDifficultyExclusive => isDifficultyExclusive;
        public DifficultyType RequiredDifficulty => requiredDifficulty;

    }
}

