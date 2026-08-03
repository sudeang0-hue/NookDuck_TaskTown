
// 사용하지 않음 — 하위 호환/참고용으로만 남겨 둡니다.
// public enum GameMenuState
// {
//     Compact,
//     ExpandedTown,
//     Gacha,
//     AnimalDex,
//     ToolPlacement,
//     VillageUpgrade,
//     MiniGame
// }

/// <summary>
/// 메인 메뉴 패널 구분. UIPanelWindow.MenuType / 배타 오픈에 사용합니다.
/// Inventory·Dex·Village는 내부 탭(GameTabType)으로 세분화합니다.
/// </summary>
public enum GameMenuType
{
    None,
    Inventory,
    Gacha,
    Dex,
    Village,
    Option,
    LevelUpPopup
}

/// <summary>
/// 인벤/도감 상세 페이지 구분.
/// </summary>
public enum GameMenuPageType
{
    None,
    AnimalInvPage,
    ToolInvPage,
    AnimalDexPage,
    ToolDexPage
}

/// <summary>
/// 메뉴 내부 탭 구분.
/// </summary>
public enum GameTabType
{
    None,
    AnimalInvTab,
    ToolInvTab,
    AnimalDexTab,
    ToolDexTab,
    VillageUpgradeTab,
    AnimalSetTab,
    SoundTab,
    GraphicTab,
    GameTab
}
