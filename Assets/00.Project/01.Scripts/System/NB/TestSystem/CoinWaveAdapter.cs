//NB

using System.Collections;
using TMPro;
using UnityEngine;

// 코인 변화량에 따라 즉시 반영과 롤링 카운트업을 스마트하게 전환하며, 백만 단위 마일스톤 돌파 시 TextWaveJuicer 파도타기를 트리거하는 어댑터입니다.

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

    private void Awake()
    {
        coinText = GetComponent<TMP_Text>();
        waveJuicer = GetComponent<TextWaveJuicer>();
    }

    private void Start()
    {
        if (CoinManager.Instance != null)
        {
            displayedCoin = CoinManager.Instance.Balance;
            UpdateTextUI(displayedCoin);
        }
    }

    private void OnEnable()
    {
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.OnCoinChanged += OnCoinAmountChanged;
        }
    }

    private void OnDisable()
    {
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.OnCoinChanged -= OnCoinAmountChanged;
        }
    }

    // CoinManager 이벤트 발생 시 수치 변화량(Delta)을 분석하여 최적의 연출 분기 실행
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

        // 2. 소량 획득 (+3 등) 시: 즉시 반영하여 타격감 확보 (코루틴 실행 안 함)
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

        // 3. 대량 획득 시: 부드러운 롤링 카운트업 실행
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

    // TextMeshPro 텍스트 갱신 공통 함수
    private void UpdateTextUI(long value)
    {
        if (coinText != null)
        {
            coinText.text = value.ToString("N0");
        }
    }
}