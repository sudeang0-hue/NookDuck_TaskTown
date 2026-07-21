using TMPro;
using UnityEngine;

public class UIController_Coin_mini : MonoBehaviour
{
    [SerializeField] private CoinManager coinManager;

    [Header("획득 코인 텍스트")]
    [SerializeField] private TMP_Text allCoinText;   // 현재 보유 코인

    private bool isSubscribed;

    private void Start()
    {
        SubscribeCoinEvent();
    }


    private void OnEnable()
    {
        SubscribeCoinEvent();
    }


    private void OnDisable()
    {
        if (!isSubscribed)
            return;

        if (coinManager != null)
        {
            coinManager.OnCoinChanged -= UpdateAllCoinText;
        }

        isSubscribed = false;
    }

    private void SubscribeCoinEvent()
    {
        if (isSubscribed)
            return;

        if (coinManager == null)
        {
            Debug.LogWarning("[UIController_Coin_mini] CoinManager.Instance가 없습니다.");
            return;
        }

        coinManager.OnCoinChanged += UpdateAllCoinText;
        isSubscribed = true;

        // 현재 값 즉시 반영
        UpdateAllCoinText(coinManager.totalCoin);

        // 자동 생산은 추후 구현 예정
        //UpdateAutoCoinText(0);
    }


    /// <summary>
    /// 현재 보유 코인
    /// </summary>
    private void UpdateAllCoinText(long coinAmount)
    {
        if (allCoinText == null)
        {
            Debug.LogWarning("[UIController_Coin] allCoinText가 연결되지 않았습니다.");
            return;
        }

        allCoinText.text = coinAmount.ToString("N0");
    }


    /// <summary>
    /// 시간당 생산량
    /// </summary>
    private void UpdateAutoCoinText(long coinPerHour)
    {
        
    }
}
