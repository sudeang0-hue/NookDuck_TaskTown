namespace TaskTown.Gacha
{
    // 기획서 "사용 데이터 변수 정리"의 DifficultyType과 동일한 정의입니다.
    // 난이도 전용 뽑기 대상(예: 고난이도 전용 동물) 조건에 사용됩니다.
    // 2026.08.06 - 사용자 요청으로 Easy 난이도 제거(Normal/Hard/VeryHard 3단계로 축소).
    public enum DifficultyType
    {
        Normal,
        Hard,
        VeryHard
    }
}
