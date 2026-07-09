namespace TaskTown.Gacha
{
    // CoinManager(담당: 별도 팀원, Assembly-CSharp 소속) 구현체와 뽑기 시스템을 직접 결합시키지 않기 위한 인터페이스입니다.
    // Assembly-CSharp은 이 어셈블리보다 나중에 컴파일되므로, 뽑기 쪽에서 CoinManager 타입을 직접 참조할 수 없습니다.
    // ITownLevelProvider와 동일한 이유로 인터페이스를 통해 연결합니다.
    public interface ICoinWallet
    {
        long Balance { get; }
        bool TrySpend(long amount);
        void Add(long amount);
    }
}
