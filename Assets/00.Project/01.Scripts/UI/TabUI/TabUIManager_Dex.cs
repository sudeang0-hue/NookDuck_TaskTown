using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// 동물 도감 / 도구 도감 탭 전환 Manager.
    /// Inventory·Town 탭 Manager와 동일한 토글·배타 오픈 패턴을 따릅니다.
    /// </summary>
    public class TabUIManager_Dex : MonoBehaviour
    {
        [Header("Tab Buttons")]
        [SerializeField] private Button[] openAnimalDexButtons;
        [SerializeField] private Button[] openToolDexButtons;

        [Header("Tab Panels (비어 있으면 MenuType+TabType으로 자동 보완)")]
        [SerializeField] private UIPanelWindow AnimalDexPanel;
        [SerializeField] private UIPanelWindow ToolDexPanel;

        // 같은 오브젝트(또는 씬)에 있는 컨트롤러 — SerializeField 없이 자동 연결
        private UIController_AnimalDex animalDexUI;
        private UIController_ToolDex toolDexUI;

        private GameTabType lastOpenedTab = GameTabType.AnimalDexTab;
        private UIController_Menu menuUI;

        private void Awake()
        {
            animalDexUI = GetComponent<UIController_AnimalDex>();
            if (animalDexUI == null)
                animalDexUI = FindFirstObjectByType<UIController_AnimalDex>();

            toolDexUI = GetComponent<UIController_ToolDex>();
            if (toolDexUI == null)
                toolDexUI = FindFirstObjectByType<UIController_ToolDex>();

            if (menuUI == null)
                menuUI = FindFirstObjectByType<UIController_Menu>();

            EnsurePanelsResolved();
        }

        private void RequestExclusiveMenu()
        {
            menuUI?.CloseOtherExclusiveMenus(GameMenuType.Dex);
        }

        private void EnsurePanelsResolved()
        {
            UIPanelTabUtility.ResolveTabPanel(ref AnimalDexPanel, GameMenuType.Dex, GameTabType.AnimalDexTab);
            UIPanelTabUtility.ResolveTabPanel(ref ToolDexPanel, GameMenuType.Dex, GameTabType.ToolDexTab);
        }

        private void OnEnable()
        {
            RegisterButtonListeners(openAnimalDexButtons, OpenAnimalDexTab);
            RegisterButtonListeners(openToolDexButtons, OpenToolDexTab);
        }

        private void OnDisable()
        {
            UnregisterButtonListeners(openAnimalDexButtons, OpenAnimalDexTab);
            UnregisterButtonListeners(openToolDexButtons, OpenToolDexTab);
        }

        /// <summary>
        /// 기본으로 오픈할 패널 (동물 도감)
        /// </summary>
        public void OpenDefaultTab()
        {
            OpenAnimalDexTab();
        }

        /// <summary>
        /// 동물 도감 탭 열기
        /// </summary>
        public void OpenAnimalDexTab()
        {
            lastOpenedTab = GameTabType.AnimalDexTab;
            RequestExclusiveMenu();

            // Dex에는 NotifyPanelClosed가 없어, 상세 페이지 정리는 NotifyPanelOpened로 처리
            toolDexUI?.NotifyPanelOpened();
            UIPanelTabUtility.OpenTab(GameMenuType.Dex, GameTabType.AnimalDexTab);
            animalDexUI?.NotifyPanelOpened();
            EnsurePanelsResolved();
        }

        /// <summary>
        /// 도구 도감 탭 열기
        /// </summary>
        public void OpenToolDexTab()
        {
            lastOpenedTab = GameTabType.ToolDexTab;
            RequestExclusiveMenu();

            animalDexUI?.NotifyPanelOpened();
            UIPanelTabUtility.OpenTab(GameMenuType.Dex, GameTabType.ToolDexTab);
            toolDexUI?.NotifyPanelOpened();
            EnsurePanelsResolved();
        }

        private void OpenLastTab()
        {
            switch (lastOpenedTab)
            {
                case GameTabType.ToolDexTab:
                    OpenToolDexTab();
                    break;

                default:
                    OpenAnimalDexTab();
                    break;
            }
        }

        /// <summary>
        /// 메뉴 버튼에서 호출할 Toggle 메서드
        /// </summary>
        public void ToggleDexTabs()
        {
            if (IsAnyTabOpen)
            {
                CloseAllTabs();
                return;
            }

            OpenLastTab();
        }

        public bool IsAnyTabOpen => UIPanelTabUtility.IsAnyTabOpen(GameMenuType.Dex);

        /// <summary>
        /// 탭 닫기
        /// </summary>
        public void CloseAllTabs()
        {
            UIPanelTabUtility.CloseAllTabs(GameMenuType.Dex);
            animalDexUI?.NotifyPanelOpened();
            toolDexUI?.NotifyPanelOpened();
        }

        private void RegisterButtonListeners(Button[] buttons, UnityAction action)
        {
            if (buttons == null)
                return;

            foreach (Button button in buttons)
            {
                if (button != null)
                    button.onClick.AddListener(action);
            }
        }

        private void UnregisterButtonListeners(Button[] buttons, UnityAction action)
        {
            if (buttons == null)
                return;

            foreach (Button button in buttons)
            {
                if (button != null)
                    button.onClick.RemoveListener(action);
            }
        }
    }
}
