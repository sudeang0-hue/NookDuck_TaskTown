namespace TaskTown.Tutorial
{
    /// <summary>
    /// 최초 플레이 튜토리얼의 선형 진행 단계를 나타냅니다.
    /// 저장 데이터와 연결되므로 기존 값의 순서는 변경하지 않습니다.
    /// </summary>
    public enum TutorialStep
    {
        IntroDialogue = 0,
        EarnManualCoin = 1,
        DrawAnimal = 2,
        DrawTool = 3,
        AssignAnimal = 4,
        ConfirmAutoProduction = 5,
        OpenVillageInfo = 6,
        UpgradeVillage = 7,
        CompletionDialogue = 8,
        Completed = 9
    }
}
