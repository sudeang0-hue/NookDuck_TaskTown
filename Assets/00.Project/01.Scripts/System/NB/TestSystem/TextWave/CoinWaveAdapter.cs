using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
[RequireComponent(typeof(TextWaveJuicer))]
public class CoinWaveAdapter : MonoBehaviour
{
    private TMP_Text coinText;
    private TextWaveJuicer waveJuicer;

    [Header("카운팅(Rolling) 애니메이션 설정")]
    [Tooltip("숫자가 차오르는 롤링 연출을 적용할 최소 코인 변화량 (이 값 미만은 즉시 반영)")]
    [SerializeField] private long minRollingThreshold = 100L;

    [Tooltip("롤링 연출 시 목적지까지 차오르는 시간 (초)")]
    [SerializeField] private float countUpDuration = 0.4f;

    [Header("마일스톤 연출 트리거 설정")]
    [Tooltip("파도타기를 트리거할 마일스톤 단위 (기본: 1,000,000)")]
    [SerializeField] private long milestoneInterval = 1000000L;

    private long displayedCoin = 0;
    private Coroutine countUpCoroutine;

    // [핵심] 이중 구독을 방지하기 위한 상태 플래그
    private bool isSubscribed = false;

    private void Awake()
    {
        coinText = GetComponent<TMP_Text>();
        waveJuicer = GetComponent<TextWaveJuicer>();
    }

    private void Start()
    {
        // 1. Start 시점에 안전하게 이벤트 구독 시도
        TrySubscribe();

        // 2. 초기 잔액 동기화
        if (CoinManager.Instance != null)
        {
            displayedCoin = CoinManager.Instance.Balance;
            UpdateTextUI(displayedCoin);
        }
    }

    private void OnEnable()
    {
        // OnEnable 시점에도 구독 시도 (Start보다 먼저 호출될 경우 TrySubscribe 내 조건으로 방어)
        TrySubscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    /// <summary>
    /// 싱글톤 인스턴스 존재 여부를 검사하고 이중 구독 없이 안전하게 이벤트를 바인딩합니다.
    /// </summary>
    private void TrySubscribe()
    {
        if (isSubscribed) return;

        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.OnCoinChanged += OnCoinAmountChanged;
            isSubscribed = true;
        }
    }

    /// <summary>
    /// 이벤트 해제 및 구독 플래그 초기화
    /// </summary>
    private void Unsubscribe()
    {
        if (!isSubscribed) return;

        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.OnCoinChanged -= OnCoinAmountChanged;
        }
        isSubscribed = false;
    }

    private void OnCoinAmountChanged(long targetCoin)
    {
        long deltaCoin = targetCoin - displayedCoin;

        // 1. 백만 단위 마일스톤 돌파 여부 검사 ($K = \lfloor S / M \rfloor$)
        long prevMilestone = displayedCoin / milestoneInterval;
        long currMilestone = targetCoin / milestoneInterval;
        bool isMilestoneCrossed = (targetCoin > displayedCoin) && (currMilestone > prevMilestone);

        if (isMilestoneCrossed && waveJuicer != null)
        {
            waveJuicer.PlayPianoWave();
        }

        // 2. 소량 획득 시: 즉시 반영
        if (Mathf.Abs(deltaCoin) < minRollingThreshold)
        {
            if (countUpCoroutine != null)
            {
                StopCoroutine(countUpCoroutine);
                countUpCoroutine = null;
            }

            displayedCoin = targetCoin;
            UpdateTextUI(displayedCoin);
            return;
        }

        // 3. 대량 획득 시: 롤링 카운트업 실행
        if (countUpCoroutine != null)
        {
            StopCoroutine(countUpCoroutine);
        }

        countUpCoroutine = StartCoroutine(CountUpRoutine(displayedCoin, targetCoin));
    }

    private IEnumerator CountUpRoutine(long startValue, long targetValue)
    {
        float elapsedTime = 0f;

        while (elapsedTime < countUpDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsedTime / countUpDuration);
            float smoothAlpha = Mathf.SmoothStep(0f, 1f, alpha);

            displayedCoin = (long)Mathf.Lerp(startValue, targetValue, smoothAlpha);
            UpdateTextUI(displayedCoin);

            yield return null;
        }

        displayedCoin = targetValue;
        UpdateTextUI(displayedCoin);
        countUpCoroutine = null;
    }

    private void UpdateTextUI(long value)
    {
        if (coinText != null)
        {
            coinText.text = value.ToString("N0");
        }
    }
}