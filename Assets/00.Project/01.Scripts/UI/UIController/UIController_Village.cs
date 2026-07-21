using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 연결된 마을 정보의 출력과 갱신을 담당합니다.
/// CurrentCoin / Slider / 레벨업 버튼은 CoinManager와 연동합니다.
/// Click / Typing 수치는 VillageUI_Manager가 EarnProcessor(Base * Multiplier) 값을 전달합니다.
/// </summary>
public class UIController_Village : MonoBehaviour
{
    [Header("갱신할 정보")]
    [SerializeField] private TMP_Text townLevelText;
    [SerializeField] private TMP_Text clickCoinText;
    [SerializeField] private TMP_Text typingCoinText;
    [SerializeField] private TMP_Text toolCapacityText;
    [SerializeField] private TMP_Text currentCoinText;
    [SerializeField] private TMP_Text requireLevelupCoinText;
    [SerializeField] private Slider coinSlider;

    [Header("선택 연결")]
    [SerializeField] private Button upgradeButton;
    [SerializeField] private VillageUI_Manager villageUIManager;

    private const string CurrentCoinPrefix = "CurrentCoin: ";

    private int requireLevelupCoin;
    private bool isSubscribed;

    private void Awake()
    {
        if (villageUIManager == null)
            villageUIManager = GetComponentInParent<VillageUI_Manager>();

        if (upgradeButton != null)
            upgradeButton.onClick.AddListener(OnClickUpgrade);
    }

    private void OnDestroy()
    {
        if (upgradeButton != null)
            upgradeButton.onClick.RemoveListener(OnClickUpgrade);
    }

    private void OnEnable()
    {
        SubscribeCoinEvent();
        RefreshCoinView();
    }

    private void OnDisable()
    {
        UnsubscribeCoinEvent();
    }

    /// <summary>
    /// VillageUI_Manager에서 전달받은 값으로 텍스트/슬라이더/버튼을 갱신합니다.
    /// currentCoin은 CoinManager 값을 우선 사용합니다.
    /// </summary>
    public void Refresh(
        int townLevel,
        int clickCoin,
        int typingCoin,
        int toolCapacity,
        long currentCoin,
        int requireLevelupCoin,
        bool canUpgrade)
    {
        this.requireLevelupCoin = Mathf.Max(0, requireLevelupCoin);

        SetText(townLevelText, "TownLevel : " + townLevel);
        SetText(clickCoinText, "Click : " + clickCoin.ToString("N0"));
        SetText(typingCoinText, "Typing : " + typingCoin.ToString("N0"));
        SetText(toolCapacityText, "Tool : " + toolCapacity.ToString("N0"));
        SetText(requireLevelupCoinText, "Require : " + this.requireLevelupCoin.ToString("N0"));

        long displayCoin = currentCoin;
        if (CoinManager.Instance != null)
            displayCoin = CoinManager.Instance.totalCoin;

        ApplyCoinProgress(displayCoin);
        SubscribeCoinEvent();
    }

    /// <summary>
    /// 레벨업 버튼 stub. 실제 VillageSystem 연동 전 단계.
    /// </summary>
    private void OnClickUpgrade()
    {
        Debug.Log("[UIController_Village] 마을 레벨업 코드 실행");

        // TODO: 마을 레벨업 시스템 연동 후 실제 처리로 교체
        if (villageUIManager != null)
            villageUIManager.RefreshVillageUI();
        else
            RefreshCoinView();
    }

    private void SubscribeCoinEvent()
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

    /// <summary>
    /// CurrentCoinText / Coin_Slider / 레벨업 버튼 상태를 갱신합니다.
    /// Slider: 0 = 코인 0, 1 = RequireLevelupCoin 도달.
    /// Require 미달 시 레벨업 버튼은 숨김 처리합니다.
    /// </summary>
    private void ApplyCoinProgress(long currentCoin)
    {
        SetText(currentCoinText, CurrentCoinPrefix + currentCoin.ToString("N0"));

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
