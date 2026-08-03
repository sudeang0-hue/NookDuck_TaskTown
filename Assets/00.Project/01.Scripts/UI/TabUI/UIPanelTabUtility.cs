using UnityEngine;

namespace UI
{
    /// <summary>
    /// UIPanelWindow의 MenuType + TabType으로 탭 패널을 찾고 전환합니다.
    /// TabType == None 인 창(상세 페이지·단일 메뉴 등)은 탭 전환 대상에서 제외합니다.
    /// </summary>
    public static class UIPanelTabUtility
    {
        /// <summary>
        /// MenuType + TabType이 일치하는 탭 패널을 찾습니다.
        /// </summary>
        public static UIPanelWindow FindTabPanel(GameMenuType menuType, GameTabType tabType)
        {
            if (menuType == GameMenuType.None || tabType == GameTabType.None)
                return null;

            UIPanelWindow[] windows = Object.FindObjectsByType<UIPanelWindow>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < windows.Length; i++)
            {
                UIPanelWindow window = windows[i];
                if (window == null)
                    continue;

                if (window.MenuType == menuType && window.TabType == tabType)
                    return window;
            }

            return null;
        }

        /// <summary>
        /// Inspector 참조가 비어 있을 때 MenuType + TabType으로 패널을 보완합니다.
        /// </summary>
        public static void ResolveTabPanel(ref UIPanelWindow panel, GameMenuType menuType, GameTabType tabType)
        {
            if (panel != null)
                return;

            panel = FindTabPanel(menuType, tabType);
        }

        /// <summary>
        /// 같은 MenuType의 탭 패널 중 targetTab만 열고 나머지 탭은 닫습니다.
        /// </summary>
        /// <returns>열린 대상 탭 패널. 없으면 null.</returns>
        public static UIPanelWindow OpenTab(GameMenuType menuType, GameTabType targetTab)
        {
            if (menuType == GameMenuType.None || targetTab == GameTabType.None)
                return null;

            UIPanelWindow[] windows = Object.FindObjectsByType<UIPanelWindow>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            UIPanelWindow target = null;

            for (int i = 0; i < windows.Length; i++)
            {
                UIPanelWindow window = windows[i];
                if (window == null || window.MenuType != menuType)
                    continue;

                // 상세 페이지 등 TabType=None 창은 탭 전환에서 건드리지 않음
                if (window.TabType == GameTabType.None)
                    continue;

                if (window.TabType == targetTab)
                    target = window;
                else
                    window.ClosePanel();
            }

            target?.OpenPanelDefaultPosition();
            return target;
        }

        /// <summary>
        /// 해당 MenuType의 모든 탭 패널(TabType != None)을 닫습니다.
        /// </summary>
        public static void CloseAllTabs(GameMenuType menuType)
        {
            if (menuType == GameMenuType.None)
                return;

            UIPanelWindow[] windows = Object.FindObjectsByType<UIPanelWindow>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < windows.Length; i++)
            {
                UIPanelWindow window = windows[i];
                if (window == null || window.MenuType != menuType)
                    continue;

                if (window.TabType == GameTabType.None)
                    continue;

                window.ClosePanel();
            }
        }

        /// <summary>
        /// 해당 MenuType의 탭 패널 중 하나라도 열려 있는지 확인합니다.
        /// </summary>
        public static bool IsAnyTabOpen(GameMenuType menuType)
        {
            if (menuType == GameMenuType.None)
                return false;

            UIPanelWindow[] windows = Object.FindObjectsByType<UIPanelWindow>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < windows.Length; i++)
            {
                UIPanelWindow window = windows[i];
                if (window == null || window.MenuType != menuType)
                    continue;

                if (window.TabType == GameTabType.None)
                    continue;

                if (window.gameObject.activeSelf)
                    return true;
            }

            return false;
        }
    }
}
