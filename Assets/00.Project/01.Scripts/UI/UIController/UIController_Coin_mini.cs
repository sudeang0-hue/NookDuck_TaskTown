using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;

public class UIController_Coin_mini : MonoBehaviour
{
    private const long Million = 1_000_000;
    private const long Billion = 1_000_000_000;


    private CoinManager coinManager;

    [Header("획득 코인 텍스트")]
    [SerializeField] private TMP_Text allCoinText;

    private bool isSubscribed;
    private Coroutine subscribeRoutine;

    private void Awake()
    {
        if(coinManager == null)
        {
            coinManager = CoinManager.Instance;
        }
    }
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

        //-----------------26.08.05 KDH-------------------------
        // Prefab에 텍스트 외 다른 오브젝트가 연결될 수 있어, 표시 대상이 없으면 구독하지 않습니다.
        if (allCoinText == null)
            return;
        //----------------------------------------

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

        //allCoinText.text = coinAmount.ToString("N0");
        allCoinText.text = FormatCoinAmount(coinAmount);
    }

    private string FormatCoinAmount(long coinAmount)
    {

        if (coinAmount >= Billion)
        {
            double formattedAmount = coinAmount / (double)Billion;

            return formattedAmount.ToString(
                "0.##",
                CultureInfo.InvariantCulture) + "B";
        }
        /*
        if (coinAmount >= Million)
        {
            double formattedAmount = coinAmount / (double)Million;

            return formattedAmount.ToString(
                "0.##",
                CultureInfo.InvariantCulture) + "M";
        }
        */
        return coinAmount.ToString("N0");
    }
}
