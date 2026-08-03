using System.Collections.Generic;
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

        // -----------------------------------------------------------------------------
        // [ 2026.08.03 - Choi - 튜토리얼 단계별 강조 연동 ]
        // 기능: 현재 열려 있는 마을 패널에 따라 활성 버튼이 달라지는 동물 배치 탭을
        //       튜토리얼이 후보 목록으로 사용할 수 있도록 읽기 전용으로 공개합니다.
        // -----------------------------------------------------------------------------
        public IReadOnlyList<Button> OpenAnimalSetButtons => openAnimalSetButtons;

        // -----------------------------------------------------------------------------
        // [ 2026.08.03 - Choi - 마을 업그레이드 튜토리얼 강조 연동 ]
        // 기능: 업그레이드 탭 후보 버튼과 실제 패널 활성 상태를 읽기 전용으로 제공해
        //       튜토리얼이 화면 진입 시 강조를 종료할 수 있게 합니다.
        // -----------------------------------------------------------------------------
        public IReadOnlyList<Button> OpenVillageUpgradeButtons =>
            openVillageUpgradeButtons;
        public bool IsVillageUpgradeTabOpen =>
            villageUpgradePanel != null &&
            villageUpgradePanel.gameObject.activeInHierarchy;

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
