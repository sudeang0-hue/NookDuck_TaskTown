using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// VillageInfo_Root 표시 전담.
    /// 마을 레벨 / 클릭·타이핑 획득량 / 생산 보너스(도구 효율) / 장착 가능 도구 수량을 출력합니다.
    /// 업그레이드·레벨업 상호작용은 VillageUpgrade_root에서 처리합니다.
    /// </summary>
    public class UIController_VillageInfo : MonoBehaviour
    {
        [Header("갱신할 정보")]
        [SerializeField] private TMP_Text townLevelText;
        [SerializeField] private TMP_Text clickCoinText;
        [SerializeField] private TMP_Text typingCoinText;
        [SerializeField] private TMP_Text toolCapacityText;
        [SerializeField] private TMP_Text autoProductBonusText;

        [Header("패널 닫기 트리거")]
        [Tooltip("닫기 버튼, Minimize 등. 클릭 시 VillageInfo 패널을 닫습니다.")]
        [SerializeField] private Button[] closePanelButtons;

        [SerializeField] private VillageInfoUI_Manager villageUIManager;

        private void Awake()
        {
            if (villageUIManager == null)
                villageUIManager = GetComponent<VillageInfoUI_Manager>();

            BindClosePanelButtons();
        }

        private void OnDestroy()
        {
            UnbindClosePanelButtons();
        }

        private void BindClosePanelButtons()
        {
            if (closePanelButtons == null)
                return;

            for (int i = 0; i < closePanelButtons.Length; i++)
            {
                if (closePanelButtons[i] == null)
                    continue;

                closePanelButtons[i].onClick.AddListener(OnClosePanelRequested);
            }
        }

        private void UnbindClosePanelButtons()
        {
            if (closePanelButtons == null)
                return;

            for (int i = 0; i < closePanelButtons.Length; i++)
            {
                if (closePanelButtons[i] == null)
                    continue;

                closePanelButtons[i].onClick.RemoveListener(OnClosePanelRequested);
            }
        }

        private void OnClosePanelRequested()
        {
            if (villageUIManager == null)
                return;

            villageUIManager.ClosePanel();
        }

        /// <summary>
        /// VillageUpgrade / TownUpgrade 현재 상태를 VillageInfo 텍스트에 반영합니다.
        /// toolCapacity는 현재 마을 레벨 기준 도구 상한입니다.
        /// </summary>
        public void Refresh(
            int townLevel,
            int clickCoin,
            int typingCoin,
            int toolCapacity,
            float autoProductBonus)
        {
            SetText(townLevelText, "마을 레벨: " + townLevel + ".Lv");
            SetText(clickCoinText, "클릭 코인: " + clickCoin.ToString("N0"));
            SetText(typingCoinText, "타이핑 코인: " + typingCoin.ToString("N0"));
            // 현재 마을 레벨의 도구 상한 (InventoryManager_Tool.GetToolCapacity)
            SetText(toolCapacityText, "도구 상한: " + toolCapacity.ToString("N0"));
            // VillageUpgrade의 toolProductValue 표기와 동일 (도구 효율 배율)
            SetText(autoProductBonusText, "생산 효율: " + autoProductBonus.ToString("0.#") + " %");
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target == null)
                return;

            target.text = value;
        }
    }
}
