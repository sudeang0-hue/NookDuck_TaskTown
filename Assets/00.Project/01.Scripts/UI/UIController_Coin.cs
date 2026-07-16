using System;
using TMPro;
using UnityEngine;

public class UIController_Coin : MonoBehaviour
{

    [Header("획득 코인 텍스트")]
    [SerializeField] private TMP_Text allCoinText;   // 현재 보유 코인
    [SerializeField] private TMP_Text autoCoinText;  // 시간당 획득량

    [Header("Text Format")]
    [SerializeField] private string prefix = " /h";

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

        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.OnCoinChanged -= UpdateAllCoinText;
        }

        isSubscribed = false;
    }

    private void SubscribeCoinEvent()
    {
        if (isSubscribed)
            return;

        if (CoinManager.Instance == null)
        {
            Debug.LogWarning("[UIController_Coin] CoinManager.Instance가 없습니다.");
            return;
        }

        CoinManager.Instance.OnCoinChanged += UpdateAllCoinText;
        isSubscribed = true;

        // 현재 값 즉시 반영
        UpdateAllCoinText(CoinManager.Instance.totalCoin);

        // 자동 생산은 추후 구현 예정
        UpdateAutoCoinText(0);
    }


    /// <summary>
    /// 현재 보유 코인 UI
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
    /// 시간당 생산량 UI
    /// </summary>
    private void UpdateAutoCoinText(long coinPerHour)
    {
        if (autoCoinText == null)
        {
            Debug.LogWarning("[UIController_Coin] autoCoinText가 연결되지 않았습니다.");
            return;
        }

        autoCoinText.text = $"{coinPerHour:N0}{prefix}";
    }


}
