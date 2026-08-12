using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// 사운드 옵션 / 게임 옵션 탭 전환 Manager.
    /// Inventory·Dex·Town 탭 Manager와 동일한 토글·배타 오픈 패턴을 따릅니다.
    /// </summary>
    public class TabUIManager_Option : MonoBehaviour
    {
        [Header("Tab Buttons")]
        [SerializeField] private Button[] openSoundOptionButtons;
        [SerializeField] private Button[] openGameOptionButtons;

        [Header("Tab Panels (비어 있으면 MenuType+TabType으로 자동 보완)")]
        [SerializeField] private UIPanelWindow SoundOptionPanel;
        [SerializeField] private UIPanelWindow GameOptionPanel;

        // 같은 오브젝트(또는 씬)에 있는 컨트롤러 — SerializeField 없이 자동 연결
        private UIController_SoundOption soundOptionUI;
        private UIController_GameOption gameOptionUI;

        private GameTabType lastOpenedTab = GameTabType.SoundTab;
        private UIController_Menu menuUI;

        private void Awake()
        {
            soundOptionUI = GetComponent<UIController_SoundOption>();
            if (soundOptionUI == null)
                soundOptionUI = FindFirstObjectByType<UIController_SoundOption>();

            gameOptionUI = GetComponent<UIController_GameOption>();
            if (gameOptionUI == null)
                gameOptionUI = FindFirstObjectByType<UIController_GameOption>();

            if (menuUI == null)
                menuUI = GetComponent<UIController_Menu>();

            if (menuUI == null)
                menuUI = FindFirstObjectByType<UIController_Menu>();

            EnsurePanelsResolved();
        }

        private void RequestExclusiveMenu()
        {
            menuUI?.CloseOtherExclusiveMenus(GameMenuType.Option);
        }

        private void EnsurePanelsResolved()
        {
            UIPanelTabUtility.ResolveTabPanel(ref SoundOptionPanel, GameMenuType.Option, GameTabType.SoundTab);
            UIPanelTabUtility.ResolveTabPanel(ref GameOptionPanel, GameMenuType.Option, GameTabType.GameTab);
        }

        private void OnEnable()
        {
            RegisterButtonListeners(openSoundOptionButtons, OpenSoundOptionTab);
            RegisterButtonListeners(openGameOptionButtons, OpenGameOptionTab);
        }

        private void OnDisable()
        {
            UnregisterButtonListeners(openSoundOptionButtons, OpenSoundOptionTab);
            UnregisterButtonListeners(openGameOptionButtons, OpenGameOptionTab);
        }

        /// <summary>
        /// 기본으로 오픈할 패널 (사운드 옵션)
        /// </summary>
        public void OpenDefaultTab()
        {
            OpenSoundOptionTab();
        }

        /// <summary>
        /// 사운드 옵션 탭 열기
        /// </summary>
        public void OpenSoundOptionTab()
        {
            lastOpenedTab = GameTabType.SoundTab;
            RequestExclusiveMenu();
            UIPanelTabUtility.OpenTab(GameMenuType.Option, GameTabType.SoundTab);
            EnsurePanelsResolved();
        }

        /// <summary>
        /// 게임 옵션 탭 열기
        /// </summary>
        public void OpenGameOptionTab()
        {
            lastOpenedTab = GameTabType.GameTab;
            RequestExclusiveMenu();
            UIPanelTabUtility.OpenTab(GameMenuType.Option, GameTabType.GameTab);
            EnsurePanelsResolved();
        }

        private void OpenLastTab()
        {
            switch (lastOpenedTab)
            {
                case GameTabType.GameTab:
                    OpenGameOptionTab();
                    break;

                default:
                    OpenSoundOptionTab();
                    break;
            }
        }

        /// <summary>
        /// 메뉴 버튼에서 호출할 Toggle 메서드
        /// </summary>
        public void ToggleOptionTabs()
        {
            if (IsAnyTabOpen)
            {
                CloseAllTabs();
                return;
            }

            OpenLastTab();
        }

        public bool IsAnyTabOpen => UIPanelTabUtility.IsAnyTabOpen(GameMenuType.Option);

        /// <summary>
        /// 모든 옵션 탭 닫기
        /// </summary>
        public void CloseAllTabs()
        {
            UIPanelTabUtility.CloseAllTabs(GameMenuType.Option);
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
