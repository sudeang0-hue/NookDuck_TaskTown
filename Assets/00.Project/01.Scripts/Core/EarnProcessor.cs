//나범 키보드 , 마우스 클릭 이벤트 구독 코드 수정 및 추가 했습니다 :-)

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


    // ---- Debug API (DebugTool에서만 사용) ----
    public int BaseCoinPerClick => baseCoinPerClick;
    public int BaseCoinPerTyping => baseCoinPerTyping;
    public int MaxCoinPerHour => maxCoinPerHour;
    public float LimitPeriodSeconds => limitPeriodSeconds;
    public float LimitTimer => limitTimer;
    public int CurrentPeriodEarnedCoin => currentPeriodEarnedCoin;
    //---------------------------------------------------------------


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    // [NB 추가] 키보드 및 마우스 이벤트 통합 구독
    private void OnEnable()
    {
        // 1. 검증된 타이핑 이벤트 구독
        TypingInputFilter.OnTypingValidated += ProcessValidatedTyping;

        // 2. 전역 마우스 클릭 이벤트 구독
        GlobalMouseHook.OnGlobalMouseClicked += ProcessValidatedMouseClick;
    }

    private void OnDisable()
    {
        // 메모리 누수 방지를 위한 이벤트 해제
        TypingInputFilter.OnTypingValidated -= ProcessValidatedTyping;
        GlobalMouseHook.OnGlobalMouseClicked -= ProcessValidatedMouseClick;
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

    private bool CheckLimitAndAddCoin(int amount)
    {
        if (currentPeriodEarnedCoin >= maxCoinPerHour) return false;

        int allowedAmount = amount;
        if (currentPeriodEarnedCoin + amount > maxCoinPerHour)
        {
            allowedAmount = maxCoinPerHour - currentPeriodEarnedCoin;
        }

        currentPeriodEarnedCoin += allowedAmount;

        // [NB 수정}ICoinWallet 인터페이스를 통해 결합도를 낮추는 것이 좋음
        // 현재 CoinManager가 Add(long)으로 구현되어 있으므로 이를 사용
        // CoinManager에 최종 반영
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.Add(allowedAmount);
        }
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

    // [NB 핵심 추가] TypingInputFilter에서 호출되는 핸들러
    public void ProcessValidatedTyping()
    {
        int earnedCoin = baseCoinPerTyping * TypingMultiplier;
        CheckLimitAndAddCoin(earnedCoin);
    }

    // [NB 핵심 추가] 마우스 후킹에서 호출되는 핸들러
    private void ProcessValidatedMouseClick(MouseClickType clickType)
    {
        // 좌클릭/우클릭 차등 보상 처리 예시 (우클릭 시 2배 배율 예시)
        int currentClickMultiplier = (clickType == MouseClickType.Right) ? ClickMultiplier * 2 : ClickMultiplier;

        // $EarnedCoin = BaseCoin \times Multiplier$
        int earnedCoin = baseCoinPerClick * currentClickMultiplier;
        CheckLimitAndAddCoin(earnedCoin);
    }

    // 클릭/타이핑 1회 획득량 수정
    public void SetBaseCoinPerClick(int value)
    {
        baseCoinPerClick = Mathf.Max(0, value);
    }
    public void SetBaseCoinPerTyping(int value)
    {
        baseCoinPerTyping = Mathf.Max(0, value);
    }
    // 주기(시간)당 최대 획득 가능 코인 수정
    public void SetMaxCoinPerHour(int value)
    {
        maxCoinPerHour = Mathf.Max(0, value);
    }
    // limitPeriodSeconds 타이머 + 이번 주기 누적 획득량 리셋
    // Update 폴링 없이 버튼 한 번으로 주기를 처음부터 다시 시작
    public void ResetLimitPeriod()
    {
        limitTimer = 0f;
        currentPeriodEarnedCoin = 0;
        Debug.Log("[EarnProcessor] 획득 제한 주기가 수동 리셋되었습니다.");
    }
}
