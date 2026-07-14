namespace TaskTown.SceneFlow
{
    /// <summary>
    /// 향후 저장 시스템을 연결할 때 지켜야 하는 고정 초기화 단계입니다.
    /// 같은 Phase 안에서는 StartupLoadStepSO.Order 순서로 실행됩니다.
    /// </summary>
    public enum StartupLoadPhase
    {
        CoreValidation = 0,
        LocalSettings = 100,
        SaveRead = 200,
        SaveConvert = 300,
        SaveValidation = 400,
        SessionBuild = 500,
        RuntimeApply = 600,
        ScenePreload = 700,
        Finalize = 800
    }
}
