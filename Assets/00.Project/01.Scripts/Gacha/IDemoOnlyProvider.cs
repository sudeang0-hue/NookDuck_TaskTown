namespace TaskTown.Gacha
{
    // 프리팹에 기본 내장된 데모/테스트 전용 Provider(예: DemoTownLevelProvider)를 표시하는
    // 마커 인터페이스입니다. GachaManagerBase가 씬에서 실제(비-데모) Provider를 자동으로
    // 찾을 때, 이 마커를 가진 구현체는 후보에서 제외합니다.
    public interface IDemoOnlyProvider
    {
    }
}
