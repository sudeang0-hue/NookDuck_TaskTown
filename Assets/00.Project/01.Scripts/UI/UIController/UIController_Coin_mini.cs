using System.Collections;
using TMPro;
using UnityEngine;

public class UIController_Coin_mini : MonoBehaviour
{
    [SerializeField] private CoinManager coinManager;

    [Header("획득 코인 텍스트")]
    [SerializeField] private TMP_Text allCoinText;

    private bool isSubscribed;
    private Coroutine subscribeRoutine;

    private void Start()
    {
        TrySubscribeCoinEvent();
    }

    private void OnEnable()
    {
        TrySubscribeCoinEvent();

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

        if (!isSubscribed)
            return;

        CoinManager source = GetCoinManager();
        if (source != null)
            source.OnCoinChanged -= UpdateAllCoinText;

        isSubscribed = false;
    }

    private IEnumerator SubscribeWhenCoinManagerReady()
    {
        const float timeoutSeconds = 3f;
        float elapsed = 0f;

        while (GetCoinManager() == null && elapsed < timeoutSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        subscribeRoutine = null;
        TrySubscribeCoinEvent();

        if (!isSubscribed && GetCoinManager() == null)
        {
            Debug.LogWarning(
                "[UIController_Coin_mini] CoinManager가 없습니다. " +
                "Inspector 참조 또는 씬의 활성 CoinManager를 확인하세요.");
        }
    }

    private CoinManager GetCoinManager()
    {
        if (coinManager != null)
            return coinManager;

        return CoinManager.Instance;
    }

    private void TrySubscribeCoinEvent()
    {
        if (isSubscribed)
            return;

        CoinManager source = GetCoinManager();
        if (source == null)
            return;

        // Inspector 미연결 시 Instance로 보정
        if (coinManager == null)
            coinManager = source;

        source.OnCoinChanged += UpdateAllCoinText;
        isSubscribed = true;
        UpdateAllCoinText(source.totalCoin);
    }

    private void UpdateAllCoinText(long coinAmount)
    {
        if (allCoinText == null)
        {
            Debug.LogWarning("[UIController_Coin_mini] allCoinText가 연결되지 않았습니다.");
            return;
        }

        allCoinText.text = coinAmount.ToString("N0");
    }
}
