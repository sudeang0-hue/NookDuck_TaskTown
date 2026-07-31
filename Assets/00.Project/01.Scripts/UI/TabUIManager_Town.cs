using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public enum TownTabType
{
    VillageUpgrade,
    AnimalSet
}

namespace UI
{
    public class TabUIManager_Town : MonoBehaviour
    {
        [SerializeField] private Button[] openVillageUpgradeButtons;
        [SerializeField] private Button[] openAnimalSetButtons;


        [Header("Tab Panels")]
        [SerializeField] private UIPanelWindow villageUpgradePanel;
        [SerializeField] private UIPanelWindow animalSetPanel;

        private TownTabType lastOpenedTab = TownTabType.VillageUpgrade;


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
            lastOpenedTab = TownTabType.VillageUpgrade;

            animalSetPanel?.ClosePanel();
            villageUpgradePanel?.OpenPanelDefaultPosition();

            // 오픈 시 세이브 반영된 요소 업그레이드/마을 레벨을 UI에 다시 그림
            VillageUpgradeUI_Manager upgradeManager =
                villageUpgradePanel != null
                    ? villageUpgradePanel.GetComponentInParent<VillageUpgradeUI_Manager>()
                    : null;

            if (upgradeManager == null)
                upgradeManager = FindFirstObjectByType<VillageUpgradeUI_Manager>();

            upgradeManager?.RefreshAllUI();
        }

        /// <summary>
        /// 동물 배치 탭 열기
        /// </summary>
        public void OpenAnimalSetTab()
        {
            lastOpenedTab = TownTabType.AnimalSet;

            villageUpgradePanel?.ClosePanel();
            animalSetPanel?.OpenPanelDefaultPosition();
        }

        /// <summary>
        /// 마지막으로 열었던 탭 열기
        /// </summary>
        private void OpenLastTab()
        {
            switch (lastOpenedTab)
            {
                case TownTabType.AnimalSet:
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

        /// <summary>
        /// 열려있는 탭이 있는지 판단
        /// </summary>
        public bool IsAnyTabOpen
        {
            get
            {
                bool isVillageUpgradeOpen = villageUpgradePanel != null && villageUpgradePanel.gameObject.activeSelf;

                bool isAnimalSetOpen = animalSetPanel != null && animalSetPanel.gameObject.activeSelf;

                return isVillageUpgradeOpen || isAnimalSetOpen;
            }
        }

        /// <summary>
        /// 모든 탭 닫기
        /// </summary>
        public void CloseAllTabs()
        {
            villageUpgradePanel?.ClosePanel();
            animalSetPanel?.ClosePanel();
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
