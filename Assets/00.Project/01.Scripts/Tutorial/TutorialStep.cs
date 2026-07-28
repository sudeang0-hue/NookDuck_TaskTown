using System;

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
        Completed = 9,

        // 기존 저장 데이터의 enum 숫자를 유지하기 위해 논리적 삽입 단계는 마지막 값으로 추가합니다.
        CollapseAndExpandTown = 10
    }

    /// <summary>
    /// 하나의 튜토리얼 단계 안에서 저장해야 하는 세부 진행과 보상 지급 여부입니다.
    /// </summary>
    [Flags]
    public enum TutorialProgressFlags
    {
        None = 0,
        TownWindowGuideCompleted = 1 << 0,
        TownWindowMinimized = 1 << 1,
        TownWindowExpanded = 1 << 2,
        TownWindowRewardGranted = 1 << 3,
        ToolDrawCoinRewardGranted = 1 << 4
    }
}
