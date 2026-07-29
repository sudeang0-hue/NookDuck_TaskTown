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
    public class TownTabUI_Manager : MonoBehaviour
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
        /// 기본으로 오픈할 패널
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
        /// 마지막으로 열었던 탭 다시 열기
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
        /// 메뉴 버튼에서 호출할 Toggle 메서드
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
        /// 패널 활성화 상태를 확인하는 메서드
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
        /// 탭 닫기
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
