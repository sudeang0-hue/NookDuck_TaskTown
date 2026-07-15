namespace TaskTown.SceneFlow
{
    /// <summary>
    /// 코드에서 Scene 이름 문자열을 직접 사용하지 않기 위한 식별자입니다.
    /// 실제 Scene 이름은 SceneCatalogSO에서 관리합니다.
    /// </summary>
    public enum SceneId
    {
        TeamLogo = 0,
        Title = 1,
        Main = 2,
        // 기존 직렬화 값을 유지하기 위해 Bootstrap은 마지막 ID로 추가합니다.
        Bootstrap = 3
    }
}
