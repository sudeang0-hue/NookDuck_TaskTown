namespace TaskTown.Gacha
{
    // GachaSystem이 UnityEngine.Random에 직접 의존하지 않도록 분리한 인터페이스입니다.
    // 확률 검증용 테스트에서는 고정된 값을 반환하는 구현체로 교체해서 사용할 수 있습니다.
    public interface IRandomProvider
    {
        // 0.0 이상 1.0 이하의 난수를 반환합니다.
        float NextFloat01();
    }
}
