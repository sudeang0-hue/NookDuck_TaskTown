using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI
{
    public class TabUIManager_Inventory : MonoBehaviour
    {
        [SerializeField] private Button[] openAnimalInvButtons;
        [SerializeField] private Button[] openToolInvButtons;

        [Header("Tab Panels (��� ������ MenuType+TabType���� �ڵ� ����)")]
        [SerializeField] private UIPanelWindow AnimalInvPanel;
        [SerializeField] private UIPanelWindow ToolInvPanel;

        [Header("UI ����ȭ")]
        [Tooltip("���� Inv �г� ����/Ŭ���� �� ���� Sync ȣ�� ���")]
        private UIController_AnimalInv animalInvUI;
        [Tooltip("���� Inv �г� ����/Ŭ���� �� ���� Sync ȣ�� ���")]
        private UIController_ToolInv toolInvUI;

        private GameTabType lastOpenedTab = GameTabType.AnimalInvTab;
        private UIController_Menu menuUI;

        private void Awake()
        {
            if (animalInvUI == null)
                animalInvUI = FindFirstObjectByType<UIController_AnimalInv>();

            if (toolInvUI == null)
                toolInvUI = FindFirstObjectByType<UIController_ToolInv>();

            if (menuUI == null)
                menuUI = GetComponent<UIController_Menu>();

            if (menuUI == null)
                menuUI = FindFirstObjectByType<UIController_Menu>();

            EnsurePanelsResolved();
        }

        private void RequestExclusiveMenu()
        {
            menuUI?.CloseOtherExclusiveMenus(GameMenuType.Inventory);
        }

        private void EnsurePanelsResolved()
        {
            UIPanelTabUtility.ResolveTabPanel(ref AnimalInvPanel, GameMenuType.Inventory, GameTabType.AnimalInvTab);
            UIPanelTabUtility.ResolveTabPanel(ref ToolInvPanel, GameMenuType.Inventory, GameTabType.ToolInvTab);
        }

        private void OnEnable()
        {
            RegisterButtonListeners(openAnimalInvButtons, OpenAnimalInvTab);
            RegisterButtonListeners(openToolInvButtons, OpenToolInvTab);
        }

        private void OnDisable()
        {
            UnregisterButtonListeners(openAnimalInvButtons, OpenAnimalInvTab);
            UnregisterButtonListeners(openToolInvButtons, OpenToolInvTab);
        }

        /// <summary>
        /// �⺻���� ������ �г� (���� �κ�)
        /// </summary>
        public void OpenDefaultTab()
        {
            OpenAnimalInvTab();
        }

        /// <summary>
        /// ���� �κ��丮 �� ����
        /// </summary>
        public void OpenAnimalInvTab()
        {
            lastOpenedTab = GameTabType.AnimalInvTab;
            RequestExclusiveMenu();

            toolInvUI?.NotifyPanelClosed();
            UIPanelTabUtility.OpenTab(GameMenuType.Inventory, GameTabType.AnimalInvTab);
            animalInvUI?.NotifyPanelOpened();
            EnsurePanelsResolved();
        }

        /// <summary>
        /// ���� �κ��丮 �� ����
        /// </summary>
        public void OpenToolInvTab()
        {
            lastOpenedTab = GameTabType.ToolInvTab;
            RequestExclusiveMenu();

            animalInvUI?.NotifyPanelClosed();
            UIPanelTabUtility.OpenTab(GameMenuType.Inventory, GameTabType.ToolInvTab);
            toolInvUI?.NotifyPanelOpened();
            EnsurePanelsResolved();
        }

        private void OpenLastTab()
        {
            switch (lastOpenedTab)
            {
                case GameTabType.ToolInvTab:
                    OpenToolInvTab();
                    break;

                default:
                    OpenAnimalInvTab();
                    break;
            }
        }

        public void ToggleInventoryTabs()
        {
            if (IsAnyTabOpen)
            {
                CloseAllTabs();
                return;
            }

            OpenLastTab();
        }

        public bool IsAnyTabOpen => UIPanelTabUtility.IsAnyTabOpen(GameMenuType.Inventory);

        public void CloseAllTabs()
        {
            UIPanelTabUtility.CloseAllTabs(GameMenuType.Inventory);
            animalInvUI?.NotifyPanelClosed();
            toolInvUI?.NotifyPanelClosed();
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
