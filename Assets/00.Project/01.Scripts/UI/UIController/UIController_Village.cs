using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// 연결된 마을 정보의 출력과 갱신을 담당합니다.
    /// CurrentCoin / Slider / 레벨업 버튼은 CoinManager와 연동합니다.
    /// 메뉴·축소 등 닫기 트리거 버튼 클릭 시 Village 패널을 닫습니다.
    /// </summary>
    public class UIController_Village : MonoBehaviour
    {
        [Header("갱신할 정보")]
        [SerializeField] private TMP_Text townLevelText;
        [SerializeField] private TMP_Text clickCoinText;
        [SerializeField] private TMP_Text typingCoinText;
        [SerializeField] private TMP_Text toolCapacityText;
        [SerializeField] private TMP_Text autoProductBonusText;
        [SerializeField] private TMP_Text currentCoinText;
        [SerializeField] private TMP_Text requireLevelupCoinText;
        [SerializeField] private Slider coinSlider;

        [Header("선택 연결")]
        [SerializeField] private Button upgradeButton;
        [SerializeField] private VillageUI_Manager villageUIManager;

        [Header("패널 닫기 트리거")]
        [Tooltip("메뉴 목록 버튼, Minimize 등. 클릭 시 VillageInfo_Root를 닫습니다.")]
        [SerializeField] private Button[] closePanelButtons;

        private const string CurrentCoinPrefix = "CurrentCoin: ";

        private int requireLevelupCoin;
        private bool isSubscribed;
        private Coroutine subscribeRoutine;

        private void Awake()
        {
            if (villageUIManager == null)
                villageUIManager = GetComponentInParent<VillageUI_Manager>();

            if (upgradeButton != null)
                upgradeButton.onClick.AddListener(OnClickUpgrade);

            BindClosePanelButtons();
        }

        private void OnDestroy()
        {
            if (upgradeButton != null)
                upgradeButton.onClick.RemoveListener(OnClickUpgrade);

            UnbindClosePanelButtons();
        }

        private void OnEnable()
        {
            TrySubscribeCoinEvent();
            RefreshCoinView();

            if (!isSubscribed && subscribeRoutine == null)
                subscribeRoutine = StartCoroutine(SubscribeWhenCoinManagerReady());
        }

        private void OnDisable()
        {
            if (subscribeRoutine != null)
            {
                StopCoroutine(subscribeRoutine);
                subscribeRoutine = null;
            }

            UnsubscribeCoinEvent();
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

        public void Refresh(
            int townLevel,
            int clickCoin,
            int typingCoin,
            int toolCapacity,
            float autoProductBonus,
            long currentCoin,
            int requireLevelupCoin,
            bool canUpgrade)
        {
            this.requireLevelupCoin = Mathf.Max(0, requireLevelupCoin);

            SetText(townLevelText, "TownLevel : " + townLevel);
            SetText(clickCoinText, "Click : " + clickCoin.ToString("N0"));
            SetText(typingCoinText, "Typing : " + typingCoin.ToString("N0"));
            SetText(toolCapacityText, "Tool : " + toolCapacity.ToString("N0"));
            SetText(autoProductBonusText, "Bonus : " + autoProductBonus.ToString("N0")+" %");
            SetText(requireLevelupCoinText, "Require : \n" + this.requireLevelupCoin.ToString("N0"));

            long displayCoin = currentCoin;
            if (CoinManager.Instance != null)
                displayCoin = CoinManager.Instance.totalCoin;

            ApplyCoinProgress(displayCoin);
            TrySubscribeCoinEvent();
        }

        private void OnClickUpgrade()
        {
            Debug.Log("[UIController_Village] 마을 레벨업 코드 실행");

            if (villageUIManager != null)
                villageUIManager.RefreshVillageUI();
            else
                RefreshCoinView();
        }

        private IEnumerator SubscribeWhenCoinManagerReady()
        {
            const float timeoutSeconds = 3f;
            float elapsed = 0f;

            while (CoinManager.Instance == null && elapsed < timeoutSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            subscribeRoutine = null;
            TrySubscribeCoinEvent();
            RefreshCoinView();
        }

        private void TrySubscribeCoinEvent()
        {
            if (isSubscribed)
                return;

            if (CoinManager.Instance == null)
                return;

            CoinManager.Instance.OnCoinChanged += OnCoinChanged;
            isSubscribed = true;
        }

        private void UnsubscribeCoinEvent()
        {
            if (!isSubscribed)
                return;

            if (CoinManager.Instance != null)
                CoinManager.Instance.OnCoinChanged -= OnCoinChanged;

            isSubscribed = false;
        }

        private void OnCoinChanged(long coinAmount)
        {
            ApplyCoinProgress(coinAmount);
        }

        private void RefreshCoinView()
        {
            if (CoinManager.Instance == null)
                return;

            ApplyCoinProgress(CoinManager.Instance.totalCoin);
        }

        private void ApplyCoinProgress(long currentCoin)
        {
            SetText(currentCoinText, CurrentCoinPrefix + "\n" +currentCoin.ToString("N0"));

            if (coinSlider != null)
            {
                coinSlider.minValue = 0f;
                coinSlider.maxValue = 1f;

                if (requireLevelupCoin <= 0)
                    coinSlider.value = 0f;
                else
                    coinSlider.value = Mathf.Clamp01((float)currentCoin / requireLevelupCoin);
            }

            if (upgradeButton != null)
            {
                bool canUpgrade = requireLevelupCoin > 0 && currentCoin >= requireLevelupCoin;
                upgradeButton.gameObject.SetActive(canUpgrade);
            }
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target == null)
                return;

            target.text = value;
        }
    }
}
