namespace TaskTown.Gacha
{
    // 특정 난이도에서만 뽑히는 엔트리(예: 시크릿 동물)가 구현합니다.
    // GachaPoolData/GachaSystem은 이 인터페이스만 보고 필터링하므로, GachaEntryData를
    // 상속하는 어떤 데이터 타입(동물/도구/기타)도 그대로 난이도 전용으로 만들 수 있습니다.
    public interface IDifficultyGated
    {
        bool IsDifficultyExclusive { get; }
        DifficultyType RequiredDifficulty { get; }
    }
}
