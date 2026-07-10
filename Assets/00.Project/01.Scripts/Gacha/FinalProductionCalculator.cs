namespace TaskTown.Gacha
{
    // 동물 레벨, 도구 레벨, 특화 보너스, 마을 업그레이드, 난이도 보정값을 반영한 최종 생산량을 계산합니다.
    //
    // 동물은 도구 없이도 자체 생산량을 가진다(tool이 null이어도 동물 생산량만으로 값을 반환한다).
    // 도구가 있으면 동물에게 장착된 것으로 보고 추가 생산량을 더해주며, 장착된 동물이 그 도구의
    // 특화 동물과 일치하면 동물+도구 합산 생산량 전체에 보너스가 붙는다.
    //
    // 최종 생산량 = (동물 생산량(동물 레벨 보정) [+ 도구 생산량(도구 레벨 보정)] x 특화 보너스)
    //             x 난이도 보정 x 마을 업그레이드 보정
    //
    // 난이도 보정과 마을 업그레이드 보정은 동물/도구 각각이 아니라 합산된 전체 생산량에 적용한다.
    // 마을 업그레이드 보정은 아직 마을 업그레이드 시스템이 없어 값(townUpgradeMultiplier)을 파라미터로 받는다.
    // 마을 업그레이드 시스템이 만들어지면 그 쪽에서 계산한 배율을 여기에 넘겨주면 된다.
    public static class FinalProductionCalculator
    {
        public static float CalculateCoinPerSecond(
            AnimalData animal,
            ToolData tool,
            int animalLevel,
            int toolLevel,
            DifficultyType difficulty,
            DifficultyProductionTable difficultyTable,
            float townUpgradeMultiplier)
        {
            if (animal == null) return 0f;

            float animalProduction = animal.BaseCoinPerSecond * animal.CalculateLevelMultiplier(animalLevel);

            float combinedProduction;
            if (tool != null)
            {
                float toolProduction = tool.BaseCoinPerSecond * tool.CalculateLevelMultiplier(toolLevel);
                float specialBonusMultiplier = SpecialBonusCalculator.GetBonusMultiplier(tool, animal);
                combinedProduction = (animalProduction + toolProduction) * specialBonusMultiplier;
            }
            else
            {
                combinedProduction = animalProduction;
            }

            float difficultyMultiplier = difficultyTable != null ? difficultyTable.GetMultiplier(difficulty) : 1f;

            return combinedProduction * difficultyMultiplier * townUpgradeMultiplier;
        }
    }
}
