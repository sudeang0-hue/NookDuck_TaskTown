using System.Collections;
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
    private Coroutine subscribeRoutine;

    private void Start()
    {
        TrySubscribeCoinEvent();
    }

    private void OnEnable()
    {
        TrySubscribeCoinEvent();

    private void Update()
    {
        long coinPerHour = TaskTown.KDH.RealProductionTicker.Instance != null
            ? (long)(TaskTown.KDH.RealProductionTicker.Instance.CurrentCoinPerSecond * 3600f)
            : 0;

        UpdateAutoCoinText(coinPerHour);
    }


    private void OnDisable()
    {
        if (subscribeRoutine != null)
        {
            StopCoroutine(subscribeRoutine);
            subscribeRoutine = null;
        }

        if (!isSubscribed)
            return;

        if (CoinManager.Instance != null)
            CoinManager.Instance.OnCoinChanged -= UpdateAllCoinText;

        isSubscribed = false;
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

        if (!isSubscribed && CoinManager.Instance == null)
        {
            Debug.LogWarning(
                "[UIController_Coin] CoinManager.Instance가 없습니다. " +
                "씬에 활성 CoinManager가 있는지 확인하세요.");
        }
    }

    private void TrySubscribeCoinEvent()
    {
        if (isSubscribed)
            return;

        if (CoinManager.Instance == null)
        {
            Debug.LogWarning("[UIController_Coin] CoinManager.Instance�� �����ϴ�.");
            return;

        CoinManager.Instance.OnCoinChanged += UpdateAllCoinText;
        isSubscribed = true;

        
        UpdateAllCoinText(CoinManager.Instance.totalCoin);

        
        UpdateAutoCoinText(0);
    }


    /// <summary>
    /// 전체 코인 갱신
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
    /// 시간당 획득량 갱신
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
