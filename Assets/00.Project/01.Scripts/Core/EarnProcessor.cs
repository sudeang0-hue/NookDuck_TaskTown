using UnityEngine;

public class EarnProcessor : MonoBehaviour
{
    public static EarnProcessor Instance { get; private set; }

    [Header("Click & Typing Balance")]
    [SerializeField] private int baseCoinPerClick = 5; // 클릭당 기본 획득량
    [SerializeField] private int baseCoinPerTyping = 3;   // 타이핑당 기본 획득량

    [Header("Reward Limit Settings")]
    [SerializeField] private int maxCoinPerHour = 10000; // 시간(또는 지정 주기)당 최대 획득 가능 재화
    private int currentPeriodEarnedCoin = 0;             // 현재 주기 동안 획득한 재화 누적액

    [SerializeField] private float limitPeriodSeconds = 3600f; // 제한 주기 (3600초 = 1시간)
    private float limitTimer = 0f;

    // 나중에 레벨업이나 버프 시스템이 들어올 것을 대비한 배율 변수들
    public int ClickMultiplier { get; set; } = 1;
    public int TypingMultiplier { get; set; } = 1;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        // 제한 주기 타이머 체크 (예: 1시간이 지나면 누적 획득량 초기화)
        limitTimer += Time.deltaTime;
        if (limitTimer >= limitPeriodSeconds)
        {
            currentPeriodEarnedCoin = 0;
            limitTimer = 0f;
            Debug.Log("획득 제한 타이머가 리셋되었습니다.");
        }
    }

    // 재화 지급 전 제한 수치를 확인하는 공통 메서드
    private bool CheckLimitAndAddCoin(int amount)
    {
        // 이미 제한치에 도달한 경우
        if (currentPeriodEarnedCoin >= maxCoinPerHour)
        {
            Debug.LogWarning("시간당 획득 제한(Limit)에 도달하여 재화를 획득할 수 없습니다.");
            return false;
        }

        // 이번 획득으로 제한치를 넘어가게 된다면, 제한치까지만 지급
        if (currentPeriodEarnedCoin + amount > maxCoinPerHour)
        {
            int allowedAmount = maxCoinPerHour - currentPeriodEarnedCoin;
            currentPeriodEarnedCoin = maxCoinPerHour;
            CoinManager.Instance.AddCoin(allowedAmount);
            return true;
        }

        // 정상 지급
        currentPeriodEarnedCoin += amount;
        CoinManager.Instance.AddCoin(amount);
        return true;
    }

    // 1. 어디서든 클릭 시 호출
    public void ProcessGlobalClick()
    {

        int earnedCoin = baseCoinPerClick * ClickMultiplier;

        CheckLimitAndAddCoin(earnedCoin);
    }

    // 2. 어디서든 타이핑 시 호출
    public void ProcessGlobalTyping()
    {
        int earnedCoin = baseCoinPerTyping * TypingMultiplier;

        CheckLimitAndAddCoin(earnedCoin);
    }
}
