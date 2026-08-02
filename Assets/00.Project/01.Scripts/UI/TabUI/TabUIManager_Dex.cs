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

        [Header("Tab Panels")]
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

        /// <summary>
        /// Inspector 참조가 비어 있을 때 GameMenuType.Dex 패널을 이름으로 보완합니다.
        /// </summary>
        private void EnsurePanelsResolved()
        {
            if (AnimalDexPanel != null && ToolDexPanel != null)
                return;

            UIPanelWindow[] windows = FindObjectsByType<UIPanelWindow>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < windows.Length; i++)
            {
                UIPanelWindow window = windows[i];
                if (window == null || window.MenuType != GameMenuType.Dex)
                    continue;

                string key = window.name.ToLowerInvariant();
                if (AnimalDexPanel == null && key.Contains("animal") && key.Contains("dex"))
                    AnimalDexPanel = window;
                else if (ToolDexPanel == null && key.Contains("tool") && key.Contains("dex"))
                    ToolDexPanel = window;
            }
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
            EnsurePanelsResolved();
            RequestExclusiveMenu();

            ToolDexPanel?.ClosePanel();
            // Dex에는 NotifyPanelClosed가 없어, 상세 페이지 정리는 NotifyPanelOpened로 처리
            toolDexUI?.NotifyPanelOpened();

            AnimalDexPanel?.OpenPanelDefaultPosition();
            animalDexUI?.NotifyPanelOpened();
        }

        /// <summary>
        /// 도구 도감 탭 열기
        /// </summary>
        public void OpenToolDexTab()
        {
            lastOpenedTab = GameTabType.ToolDexTab;
            EnsurePanelsResolved();
            RequestExclusiveMenu();

            AnimalDexPanel?.ClosePanel();
            animalDexUI?.NotifyPanelOpened();

            ToolDexPanel?.OpenPanelDefaultPosition();
            toolDexUI?.NotifyPanelOpened();
        }

        /// <summary>
        /// 마지막으로 열었던 탭 다시 열기
        /// </summary>
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

        /// <summary>
        /// 패널 활성화 상태를 확인하는 메서드
        /// </summary>
        public bool IsAnyTabOpen
        {
            get
            {
                bool isAnimalDexOpen = AnimalDexPanel != null && AnimalDexPanel.gameObject.activeSelf;
                bool isToolDexOpen = ToolDexPanel != null && ToolDexPanel.gameObject.activeSelf;

                return isAnimalDexOpen || isToolDexOpen;
            }
        }

        /// <summary>
        /// 탭 닫기
        /// </summary>
        public void CloseAllTabs()
        {
            EnsurePanelsResolved();

            AnimalDexPanel?.ClosePanel();
            animalDexUI?.NotifyPanelOpened();

            ToolDexPanel?.ClosePanel();
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
