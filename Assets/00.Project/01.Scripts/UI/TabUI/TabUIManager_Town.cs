using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI
{
    public class TabUIManager_Town : MonoBehaviour
    {
        [SerializeField] private Button[] openVillageUpgradeButtons;
        [SerializeField] private Button[] openAnimalSetButtons;

        [Header("Tab Panels (비어 있으면 MenuType+TabType으로 자동 보완)")]
        [SerializeField] private UIPanelWindow villageUpgradePanel;
        [SerializeField] private UIPanelWindow animalSetPanel;

        private GameTabType lastOpenedTab = GameTabType.VillageUpgradeTab;
        private UIController_Menu menuUI;

        private void Awake()
        {
            if (menuUI == null)
                menuUI = GetComponent<UIController_Menu>();

            if (menuUI == null)
                menuUI = FindFirstObjectByType<UIController_Menu>();

            EnsurePanelsResolved();
        }

        private void RequestExclusiveMenu()
        {
            menuUI?.CloseOtherExclusiveMenus(GameMenuType.Village);
        }

        private void EnsurePanelsResolved()
        {
            UIPanelTabUtility.ResolveTabPanel(ref villageUpgradePanel, GameMenuType.Village, GameTabType.VillageUpgradeTab);
            UIPanelTabUtility.ResolveTabPanel(ref animalSetPanel, GameMenuType.Village, GameTabType.AnimalSetTab);
        }

        private void OnEnable()
        {
            RegisterButtonListeners(openVillageUpgradeButtons, OpenVillageUpgradeTab);
            RegisterButtonListeners(openAnimalSetButtons, OpenAnimalSetTab);
        }

        private void OnDisable()
        {
            UnregisterButtonListeners(openVillageUpgradeButtons, OpenVillageUpgradeTab);
            UnregisterButtonListeners(openAnimalSetButtons, OpenAnimalSetTab);
        }

        /// <summary>
        /// 기본 탭 화면 열기(마을 업그레이드 탭)
        /// </summary>
        public void OpenDefaultTab()
        {
            OpenVillageUpgradeTab();
        }

        /// <summary>
        /// 마을 업그레이드 탭 열기
        /// </summary>
        public void OpenVillageUpgradeTab()
        {
            lastOpenedTab = GameTabType.VillageUpgradeTab;
            RequestExclusiveMenu();

            UIPanelWindow opened = UIPanelTabUtility.OpenTab(GameMenuType.Village, GameTabType.VillageUpgradeTab);
            EnsurePanelsResolved();

            // 오픈 시 세이브 반영된 요소 업그레이드/마을 레벨을 UI에 다시 그림
            VillageUpgradeUI_Manager upgradeManager =
                opened != null
                    ? opened.GetComponentInParent<VillageUpgradeUI_Manager>()
                    : null;

            if (upgradeManager == null && villageUpgradePanel != null)
                upgradeManager = villageUpgradePanel.GetComponentInParent<VillageUpgradeUI_Manager>();

            if (upgradeManager == null)
                upgradeManager = FindFirstObjectByType<VillageUpgradeUI_Manager>();

            upgradeManager?.RefreshAllUI();
        }

        /// <summary>
        /// 동물 배치 탭 열기
        /// </summary>
        public void OpenAnimalSetTab()
        {
            lastOpenedTab = GameTabType.AnimalSetTab;
            RequestExclusiveMenu();
            UIPanelTabUtility.OpenTab(GameMenuType.Village, GameTabType.AnimalSetTab);
            EnsurePanelsResolved();
        }

        private void OpenLastTab()
        {
            switch (lastOpenedTab)
            {
                case GameTabType.AnimalSetTab:
                    OpenAnimalSetTab();
                    break;

                default:
                    OpenVillageUpgradeTab();
                    break;
            }
        }

        /// <summary>
        /// 탭 토글기능. 모든 탭 닫고 마지막으로 연 탭 열기
        /// </summary>
        public void ToggleTownTabs()
        {
            if (IsAnyTabOpen)
            {
                CloseAllTabs();
                return;
            }

            OpenLastTab();
        }

        public bool IsAnyTabOpen => UIPanelTabUtility.IsAnyTabOpen(GameMenuType.Village);

        /// <summary>
        /// 모든 탭 닫기
        /// </summary>
        public void CloseAllTabs()
        {
            UIPanelTabUtility.CloseAllTabs(GameMenuType.Village);
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
