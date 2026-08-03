using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;


public enum InventoryTabType
{
    AnimalInv,
    ToolInv
}

namespace UI
{
    public class TabUIManager_Inventory : MonoBehaviour
    {

        [SerializeField] private Button[] openAnimalInvButtons;
        [SerializeField] private Button[] openToolInvButtons;


        [Header("Tab Panels")]
        [SerializeField] private UIPanelWindow AnimalInvPanel;
        [SerializeField] private UIPanelWindow ToolInvPanel;

        [Header("UI ?????")]
        [Tooltip("???? Inv ?г? ????/??? ?? ?? ??? + SyncAllSlots ??? ???")]
        private UIController_AnimalInv animalInvUI;
        [Tooltip("???? Inv ?г? ????/??? ?? ?? ??? + SyncAllSlots ??? ???")]
        private UIController_ToolInv toolInvUI;

        private InventoryTabType lastOpenedTab = InventoryTabType.AnimalInv;
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

        /// <summary>
        /// Inspector ?????? ??? ???? ?? GameMenuType.Inventory ?г??? ??????? ????????.
        /// </summary>
        private void EnsurePanelsResolved()
        {
            if (AnimalInvPanel != null && ToolInvPanel != null)
                return;

            UIPanelWindow[] windows = FindObjectsByType<UIPanelWindow>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < windows.Length; i++)
            {
                UIPanelWindow window = windows[i];
                if (window == null || window.MenuType != GameMenuType.Inventory)
                    continue;

                string key = window.name.ToLowerInvariant();
                // Aniaml_Inv_root 오타 이름도 동물 인벤으로 인식
                bool isAnimalName = key.Contains("animal") || key.Contains("aniaml");
                if (ToolInvPanel == null && key.Contains("tool") && key.Contains("inv"))
                    ToolInvPanel = window;
                else if (AnimalInvPanel == null && isAnimalName && key.Contains("inv"))
                    AnimalInvPanel = window;
            }
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
        /// ?????? ?????? ?г?
        /// </summary>
        public void OpenDefaultTab()
        {
            OpenAnimalInvTab();
        }

        /// <summary>
        /// ???? ?κ??? ?? ????
        /// </summary>
        public void OpenAnimalInvTab()
        {
            lastOpenedTab = InventoryTabType.AnimalInv;
            EnsurePanelsResolved();
            RequestExclusiveMenu();

            ToolInvPanel?.ClosePanel();
            toolInvUI?.NotifyPanelClosed();

            AnimalInvPanel?.OpenPanelDefaultPosition();
            animalInvUI?.NotifyPanelOpened();
        }

        /// <summary>
        /// ???? ?κ??? ?? ????
        /// </summary>
        public void OpenToolInvTab()
        {
            lastOpenedTab = InventoryTabType.ToolInv;
            EnsurePanelsResolved();
            RequestExclusiveMenu();

            AnimalInvPanel?.ClosePanel();
            animalInvUI?.NotifyPanelClosed();

            ToolInvPanel?.OpenPanelDefaultPosition();
            toolInvUI?.NotifyPanelOpened();
        }

        private void OpenLastTab()
        {
            switch (lastOpenedTab)
            {
                case InventoryTabType.ToolInv:
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

        public bool IsAnyTabOpen
        {
            get
            {
                bool isAnimalInvOpen = AnimalInvPanel != null && AnimalInvPanel.gameObject.activeSelf;
                bool isToolInvOpen = ToolInvPanel != null && ToolInvPanel.gameObject.activeSelf;

                return isAnimalInvOpen || isToolInvOpen;
            }
        }

        public void CloseAllTabs()
        {
            EnsurePanelsResolved();

            AnimalInvPanel?.ClosePanel();
            animalInvUI?.NotifyPanelClosed();

            ToolInvPanel?.ClosePanel();
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
