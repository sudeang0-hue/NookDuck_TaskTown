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

        [Header("UI 동기화")]
        [Tooltip("동물 Inv 패널 오픈/닫기 시 상세 닫기 + SyncAllSlots 호출 대상")]
        private UIController_AnimalInv animalInvUI;
        [Tooltip("도구 Inv 패널 오픈/닫기 시 상세 닫기 + SyncAllSlots 호출 대상")]
        private UIController_ToolInv toolInvUI;

        private InventoryTabType lastOpenedTab = InventoryTabType.AnimalInv;


        private void Awake()
        {
            if (animalInvUI == null)
            {
                animalInvUI = FindFirstObjectByType<UIController_AnimalInv>();
            }

            if (toolInvUI == null)
            {
                toolInvUI = FindFirstObjectByType<UIController_ToolInv>();
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
        /// 기본으로 오픈할 패널
        /// </summary>
        public void OpenDefaultTab()
        {
            OpenAnimalInvTab();
        }

        /// <summary>
        /// 동물 인벤토리 탭 열기
        /// </summary>
        public void OpenAnimalInvTab()
        {
            lastOpenedTab = InventoryTabType.AnimalInv;

            // 도구 탭이 닫힐 때 상세 페이지도 함께 정리
            ToolInvPanel?.ClosePanel();
            toolInvUI?.NotifyPanelClosed();

            AnimalInvPanel?.OpenPanelDefaultPosition();
            // 패널이 켜진 뒤 슬롯 동기화 (CanUpdateView 통과 필요)
            animalInvUI?.NotifyPanelOpened();
        }

        /// <summary>
        /// 도구 인벤토리 탭 열기
        /// </summary>
        public void OpenToolInvTab()
        {
            lastOpenedTab = InventoryTabType.ToolInv;

            // 동물 탭이 닫힐 때 상세 페이지도 함께 정리
            AnimalInvPanel?.ClosePanel();
            animalInvUI?.NotifyPanelClosed();

            ToolInvPanel?.OpenPanelDefaultPosition();
            // 패널이 켜진 뒤 슬롯 동기화 (CanUpdateView 통과 필요)
            toolInvUI?.NotifyPanelOpened();
        }

        /// <summary>
        /// 마지막으로 열었던 탭 다시 열기
        /// </summary>
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

        /// <summary>
        /// 메뉴 버튼에서 호출할 Toggle 메서드
        /// </summary>
        public void ToggleInventoryTabs()
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
                bool isAnimalInvOpen = AnimalInvPanel != null && AnimalInvPanel.gameObject.activeSelf;
                bool isToolInvOpen = ToolInvPanel != null && ToolInvPanel.gameObject.activeSelf;

                return isAnimalInvOpen || isToolInvOpen;
            }
        }

        /// <summary>
        /// 탭 닫기 
        /// </summary>
        public void CloseAllTabs()
        {
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
