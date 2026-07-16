namespace TaskTown.Gacha
{
    // 동물 레벨, 도구 레벨, 특화 보너스, 마을 업그레이드, 난이도 보정값을 반영한 최종 생산량을 계산합니다.
    //
    // 동물과 도구는 각각 단독으로도 자체 생산량을 가진다(둘 중 하나만 있어도 값을 반환한다).
    // 특화 보너스는 장착된 동물이 도구의 특화 동물과 일치할 때만(둘 다 있을 때만) 붙으며,
    // 동물+도구 합산 생산량 전체에 곱해진다.
    //
    // 최종 생산량 = (동물 생산량(동물 레벨 보정) + 도구 생산량(도구 레벨 보정)) x 특화 보너스
    //             x 난이도 보정 x 마을 업그레이드 보정
    //
    // 난이도 보정과 마을 업그레이드 보정은 동물/도구 각각이 아니라 합산된 전체 생산량에 적용한다.
    // 마을 업그레이드 보정은 TownUpgradeEffectConfig가 계산한 배율을 townUpgradeMultiplier로 받는다.
    //
    // animal/tool은 GachaEntryData(공통 베이스)로 받습니다. 구체 타입(AnimalDataSO/ToolDataSO 등)은
    // Assembly-CSharp에 있고 TaskTown.Gacha 어셈블리는 그쪽을 참조할 수 없어서(순환 참조),
    // 여기서는 GachaEntryData 공통 필드/메서드만 쓰고, 특화 보너스는 ISpecialToolEntry 인터페이스로 처리합니다.
    public static class FinalProductionCalculator
    {
        public static float CalculateCoinPerSecond(
            GachaEntryData animal,
            GachaEntryData tool,
            int animalLevel,
            int toolLevel,
            DifficultyType difficulty,
            DifficultyProductionTable difficultyTable,
            float townUpgradeMultiplier)
        {
            if (animal == null && tool == null) return 0f;

            float animalProduction = animal != null ? animal.BaseCoinPerSecond * animal.CalculateLevelMultiplier(animalLevel) : 0f;
            float toolProduction = tool != null ? tool.BaseCoinPerSecond * tool.CalculateLevelMultiplier(toolLevel) : 0f;

            // 특화 보너스는 동물/도구가 둘 다 있고 서로 매칭될 때만 붙는다(둘 중 하나만 있으면 자동으로 1배).
            float specialBonusMultiplier = SpecialBonusCalculator.GetBonusMultiplier(tool, animal);
            float combinedProduction = (animalProduction + toolProduction) * specialBonusMultiplier;

            float difficultyMultiplier = difficultyTable != null ? difficultyTable.GetMultiplier(difficulty) : 1f;

            return combinedProduction * difficultyMultiplier * townUpgradeMultiplier;
        }
    }
}
