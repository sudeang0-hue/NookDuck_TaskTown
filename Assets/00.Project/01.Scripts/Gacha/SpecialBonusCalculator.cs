namespace TaskTown.Gacha
{
    // 도구가 특화 동물 정보를 가지고 있음을 나타내는 인터페이스입니다.
    // ToolData/ToolDataSO 등 구체 타입이 어느 어셈블리(Assembly-CSharp 포함)에 있든,
    // TaskTown.Gacha 어셈블리 안의 계산 로직이 순환 참조 없이 재사용할 수 있게 해줍니다.
    public interface ISpecialToolEntry
    {
        string SpecialAnimalId { get; }
        float SpecialAnimalBonusRate { get; }
    }

    // 동물과 도구의 ID를 기준으로 특화 보너스 적용 여부를 계산합니다.
    // 도구의 SpecialAnimalId와 배치된 동물의 id가 일치하면 해당 도구의 생산량에 보너스가 붙습니다.
    public static class SpecialBonusCalculator
    {
        public static bool IsSpecialMatch(GachaEntryData tool, GachaEntryData assignedAnimal)
        {
            if (tool == null || assignedAnimal == null) return false;
            if (tool is not ISpecialToolEntry specialTool) return false;
            if (string.IsNullOrEmpty(specialTool.SpecialAnimalId)) return false;

            return specialTool.SpecialAnimalId == assignedAnimal.Id;
        }

        // 특화 매칭 여부에 따른 배율입니다. 매칭이면 1 + SpecialAnimalBonusRate, 아니면 1(보정 없음).
        public static float GetBonusMultiplier(GachaEntryData tool, GachaEntryData assignedAnimal)
        {
            if (tool == null) return 1f;
            if (tool is not ISpecialToolEntry specialTool) return 1f;

            return IsSpecialMatch(tool, assignedAnimal) ? 1f + specialTool.SpecialAnimalBonusRate : 1f;
        }

        // 도구에 동물을 배치했을 때의 초당 생산량입니다. 특화 동물이 배치되면 SpecialAnimalBonusRate만큼 증가합니다.
        // 예: baseCoinPerSecond 3, specialAnimalBonusRate 0.5 → 특화 동물 배치 시 3 * 1.5 = 4.5
        public static float CalculateCoinPerSecond(GachaEntryData tool, GachaEntryData assignedAnimal)
        {
            if (tool == null) return 0f;

            return tool.BaseCoinPerSecond * GetBonusMultiplier(tool, assignedAnimal);
        }
    }
}
