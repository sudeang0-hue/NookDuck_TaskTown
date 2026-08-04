namespace TaskTown.Gacha
{
    // 난이도 시스템 구현체와 뽑기 시스템을 직접 결합시키지 않기 위한 인터페이스입니다.
    // 현재 난이도를 들고 있는 컴포넌트가 이 인터페이스만 구현하면 GachaManager에 연결할 수 있습니다.
    public interface IDifficultyProvider
    {
        DifficultyType CurrentDifficulty { get; }
    }
}
