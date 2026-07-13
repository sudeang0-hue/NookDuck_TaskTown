namespace TaskTown.Gacha
{
    // 동물/도구 공통 레벨업 조건 중 "중복 보유 개수" 요구치를 계산합니다.
    // 기획서 예시: 도구 Lv.1→2: 같은 도구 4개 필요, Lv.2→3: 16개, Lv.3→4: 64개 (4의 거듭제곱).
    public static class LevelUpRequirementCalculator
    {
        private const int DuplicateGrowthFactor = 4;

        // currentLevel에서 다음 레벨로 올리기 위해 필요한 중복 보유 개수입니다.
        public static int GetRequiredDuplicateCount(int currentLevel)
        {
            int level = currentLevel < 1 ? 1 : currentLevel;
            return (int)System.Math.Pow(DuplicateGrowthFactor, level);
        }
    }
}
