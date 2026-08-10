using System.Collections;
using System.Globalization;
using DG.Tweening;
using TaskTown.KDH;
using TMPro;
using UnityEngine;

public class UIController_Coin : MonoBehaviour
{
    private const long Million = 1_000_000;
    private const long Billion = 1_000_000_000;
    private const float SecondsPerHour = 3600f;

    [Header("획득 코인 텍스트")]
    [SerializeField] private TMP_Text allCoinText;   // 현재 보유 코인
    [SerializeField] private TMP_Text autoCoinText;  // 시간당 획득량

    [Header("Text Format")]
    [SerializeField] private string prefix = " /h";

    [Header("Coin Gain Punch")]
    [SerializeField] private Vector3 gainPunchScale = new(0.14f, 0.14f, 0f);
    [SerializeField, Min(0.01f)] private float gainPunchDuration = 0.22f;
    [SerializeField, Min(1)] private int gainPunchVibrato = 4;
    [SerializeField, Range(0f, 1f)] private float gainPunchElasticity = 0.65f;

    private bool isSubscribed;
    private Coroutine subscribeRoutine;
    private EarnProcessor subscribedEarnProcessor;
    private RealProductionTicker subscribedProductionTicker;
    private Tween gainPunchTween;
    private bool isGainPunchRequested;
    private Vector3 allCoinOriginalScale = Vector3.one;

    // [ 2026.08.03 - Choi - 튜토리얼 수동 코인 피드백 연동 ]
    // 튜토리얼 Overlay가 전체 코인 텍스트의 위치와 Scale만 참조할 수 있도록 읽기 전용으로 공개합니다.
    public TMP_Text AllCoinText => allCoinText;
    public TMP_Text AutoCoinText => autoCoinText;

    // 직전에 표시한 초당 생산량. 변경 시에만 autoCoinText를 갱신한다.
    private float lastDisplayedCoinPerSecond = float.NaN;

    private void Start()
    {
        TrySubscribeCoinEvent();
        TrySubscribeGainEvents();
    }

    private void OnEnable()
    {
        // 재활성화 시 이전 캐시를 버리고 다시 그리도록 한다.
        lastDisplayedCoinPerSecond = float.NaN;
        CacheAllCoinOriginalScale();

        TrySubscribeCoinEvent();
        TrySubscribeGainEvents();

        if ((!isSubscribed || !AreGainEventsSubscribed) && subscribeRoutine == null)
            subscribeRoutine = StartCoroutine(SubscribeWhenDependenciesReady());
    }

    private void OnDisable()
    {
        if (subscribeRoutine != null)
        {
            StopCoroutine(subscribeRoutine);
            subscribeRoutine = null;
        }

        if (isSubscribed && CoinManager.Instance != null)
            CoinManager.Instance.OnCoinChanged -= UpdateAllCoinText;

        isSubscribed = false;
        UnsubscribeGainEvents();
        StopGainPunchAndRestore();
    }

    // RealProductionTicker.Update 이후 값을 읽기 위해 LateUpdate에서 갱신한다.
    private void LateUpdate()
    {
        RefreshAutoCoinTextIfNeeded();
        PlayRequestedGainPunch();
    }

    private IEnumerator SubscribeWhenDependenciesReady()
    {
        const float timeoutSeconds = 3f;
        float elapsed = 0f;

        while ((!isSubscribed || !AreGainEventsSubscribed) &&
               elapsed < timeoutSeconds)
        {
            TrySubscribeCoinEvent();
            TrySubscribeGainEvents();
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        subscribeRoutine = null;
        TrySubscribeCoinEvent();
        TrySubscribeGainEvents();

        if (!isSubscribed && CoinManager.Instance == null)
        {
            Debug.LogWarning(
                "[UIController_Coin] CoinManager.Instance가 없습니다. " +
                "씬에 활성 CoinManager가 있는지 확인하세요.");
        }
    }

    private bool AreGainEventsSubscribed =>
        subscribedEarnProcessor != null && subscribedProductionTicker != null;

    private void TrySubscribeGainEvents()
    {
        EarnProcessor currentEarnProcessor = EarnProcessor.Instance;
        if (subscribedEarnProcessor != currentEarnProcessor)
        {
            if (subscribedEarnProcessor != null)
                subscribedEarnProcessor.ManualCoinGranted -= HandleCoinGranted;

            subscribedEarnProcessor = currentEarnProcessor;
            if (subscribedEarnProcessor != null)
                subscribedEarnProcessor.ManualCoinGranted += HandleCoinGranted;
        }

        RealProductionTicker currentProductionTicker = RealProductionTicker.Instance;
        if (subscribedProductionTicker == currentProductionTicker)
            return;

        if (subscribedProductionTicker != null)
            subscribedProductionTicker.ProductionCoinGranted -= HandleCoinGranted;

        subscribedProductionTicker = currentProductionTicker;
        if (subscribedProductionTicker != null)
            subscribedProductionTicker.ProductionCoinGranted += HandleCoinGranted;
    }

    private void UnsubscribeGainEvents()
    {
        if (subscribedEarnProcessor != null)
            subscribedEarnProcessor.ManualCoinGranted -= HandleCoinGranted;
        if (subscribedProductionTicker != null)
            subscribedProductionTicker.ProductionCoinGranted -= HandleCoinGranted;

        subscribedEarnProcessor = null;
        subscribedProductionTicker = null;
    }

    private void HandleCoinGranted(int amount)
    {
        if (amount > 0)
            isGainPunchRequested = true;
    }

    private void CacheAllCoinOriginalScale()
    {
        if (allCoinText != null)
            allCoinOriginalScale = allCoinText.rectTransform.localScale;
    }

    private void PlayRequestedGainPunch()
    {
        if (!isGainPunchRequested || allCoinText == null)
            return;

        RectTransform target = allCoinText.rectTransform;
        if (!target.gameObject.activeInHierarchy)
        {
            isGainPunchRequested = false;
            return;
        }

        if (gainPunchTween != null && gainPunchTween.IsActive() &&
            gainPunchTween.IsPlaying())
        {
            return;
        }

        EnsureGainPunchTween(target);
        if (gainPunchTween == null || !gainPunchTween.IsActive())
            return;

        isGainPunchRequested = false;
        target.localScale = allCoinOriginalScale;
        gainPunchTween.Restart();
    }

    private void EnsureGainPunchTween(RectTransform target)
    {
        if (gainPunchTween != null && gainPunchTween.IsActive())
            return;

        target.localScale = allCoinOriginalScale;
        gainPunchTween = target
            .DOPunchScale(
                gainPunchScale,
                gainPunchDuration,
                gainPunchVibrato,
                gainPunchElasticity)
            .SetAutoKill(false)
            .SetUpdate(true)
            .SetLink(target.gameObject, LinkBehaviour.KillOnDestroy)
            .Pause()
            .OnComplete(() =>
            {
                if (target != null)
                    target.localScale = allCoinOriginalScale;
            });
    }

    private void StopGainPunchAndRestore()
    {
        isGainPunchRequested = false;

        if (gainPunchTween != null && gainPunchTween.IsActive())
            gainPunchTween.Kill(false);

        gainPunchTween = null;
        if (allCoinText != null)
            allCoinText.rectTransform.localScale = allCoinOriginalScale;
    }

    private void TrySubscribeCoinEvent()
    {
        if (isSubscribed)
            return;

        if (CoinManager.Instance == null)
            return;

        CoinManager.Instance.OnCoinChanged += UpdateAllCoinText;
        isSubscribed = true;

        UpdateAllCoinText(CoinManager.Instance.totalCoin);
        RefreshAutoCoinTextIfNeeded();
    }

    private void UpdateAllCoinText(long coinAmount)
    {
        if (allCoinText == null)
        {
            Debug.LogWarning("[UIController_Coin] allCoinText가 연결되지 않았습니다.");
            return;
        }

        //allCoinText.text = coinAmount.ToString("N0");
        allCoinText.text = FormatCoinAmount(coinAmount);
    }

    // 장착/레벨/도구효율 등으로 초당 생산량이 바뀌면 시간당(/h) 텍스트를 갱신한다.
    private void RefreshAutoCoinTextIfNeeded()
    {
        float coinPerSecond = RealProductionTicker.Instance != null
            ? RealProductionTicker.Instance.CurrentCoinPerSecond
            : 0f;

        if (!float.IsNaN(lastDisplayedCoinPerSecond)
            && Mathf.Approximately(lastDisplayedCoinPerSecond, coinPerSecond))
            return;

        lastDisplayedCoinPerSecond = coinPerSecond;
        UpdateAutoCoinText((long)(coinPerSecond * SecondsPerHour));
    }

    private void UpdateAutoCoinText(long coinPerHour)
    {
        if (autoCoinText == null)
        {
            Debug.LogWarning("[UIController_Coin] autoCoinText가 연결되지 않았습니다.");
            return;
        }

        autoCoinText.text = $"{coinPerHour:N0}{prefix}";
        // autoCoinText.text = $"{FormatCoinAmount(coinPerHour)}{prefix}";
    }

    private string FormatCoinAmount(long coinAmount)
    {
        
        if (coinAmount < Billion)
        {
            
            return coinAmount.ToString("N0");
        }

        double formattedAmount = coinAmount / (double)Billion;

        return formattedAmount.ToString(
            "0.##",
            CultureInfo.InvariantCulture) + "B";
    }
}
