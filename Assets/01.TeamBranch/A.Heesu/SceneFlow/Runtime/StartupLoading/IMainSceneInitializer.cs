namespace TaskTown.SceneFlow
{
    /// <summary>
    /// Main Scene의 팀별 시스템이 저장 데이터를 순서대로 적용하기 위한 확장 지점입니다.
    /// </summary>
    public interface IMainSceneInitializer
    {
        int InitializationOrder { get; }
        void Initialize(GameSession session);
    }
}
