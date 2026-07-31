namespace TaskTown.Tutorial
{
    /// <summary>
    /// 튜토리얼이 진행 조건으로 받아들이는 일회성 입력입니다.
    /// 실제 게임 시스템과의 연결은 TutorialEventBridge가 담당합니다.
    /// </summary>
    public enum TutorialSignalType
    {
        None = 0,
        DialogueCompleted = 1,
        ManualCoinEarned = 2,
        AnimalDrawn = 3,
        ToolDrawn = 4,
        AnimalAssigned = 5,
        AutoProductionConfirmed = 6,
        VillageInfoOpened = 7,
        AnyUpgradePurchased = 8,
        TownWindowMinimized = 9,
        TownWindowExpanded = 10,
        VillageAnimalPlaced = 11
    }
}
