namespace TaskTown.Gacha
{
    // 동물/도구 공통 레벨업 조건 중 "중복 보유 개수" 요구치를 계산합니다.
    // 인플레이션 밸런스 시뮬레이션(simulate_game.py) 결과 반영: 4의 거듭제곱(4/16/64...)은
    // 레벨업 빈도가 너무 낮아, 2의 거듭제곱(Lv.1→2: 2개, Lv.2→3: 4개, Lv.3→4: 8개...)으로 완화.
    public static class LevelUpRequirementCalculator
    {
        private const int DuplicateGrowthFactor = 2;

        // currentLevel에서 다음 레벨로 올리기 위해 필요한 중복 보유 개수입니다.
        public static int GetRequiredDuplicateCount(int currentLevel)
        {
            int level = currentLevel < 1 ? 1 : currentLevel;
            return (int)System.Math.Pow(DuplicateGrowthFactor, level);
        }
    }
}
