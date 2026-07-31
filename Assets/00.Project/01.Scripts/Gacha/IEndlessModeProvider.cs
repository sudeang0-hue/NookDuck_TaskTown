namespace TaskTown.Gacha
{
    // ITownLevelProvider와 같은 목적(마을 시스템과 직접 결합하지 않기 위한 인터페이스)의 엔드리스 모드 버전입니다.
    // 엔드리스 모드 상태를 들고 있는 컴포넌트가 이 인터페이스만 구현하면, 업그레이드/레벨 상한을 가진
    // 다른 시스템들이 마을 레벨과 동일한 방식으로 연결해서 상한 해제 여부를 물어볼 수 있습니다.
    public interface IEndlessModeProvider
    {
        bool IsEndlessMode { get; }
    }
}
