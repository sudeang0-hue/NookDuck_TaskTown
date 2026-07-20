using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
/// <summary>
/// 코인/획득량/시간당 한도/제한 주기 리셋용 디버그 패널.
/// OnGUI를 쓰므로 프리팹·씬 수정 없이 동작합니다.
/// 릴리즈 빌드에서는 컴파일에서 제외됩니다.
/// </summary>
public class DebugTool : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private KeyCode toggleKey = KeyCode.BackQuote; // ` 키
    [SerializeField] private bool showOnStart = true;
    // 입력 버퍼 (OnGUI TextField용). Update에서 문자열 만들지 않고 필드만 갱신합니다.
    private string coinBalanceInput = "0";
    private string clickCoinInput = "5";
    private string typingCoinInput = "3";
    private string maxPerHourInput = "10000";
    private bool isVisible;
    private Rect windowRect = new Rect(20f, 20f, 360f, 320f);
    // Awake에서 싱글톤만 캐시해 Update 안에서의 Find 비용을 없앱니다.
    private CoinManager coinManager;
    private EarnProcessor earnProcessor;
    private void Awake()
    {
        coinManager = CoinManager.Instance;
        earnProcessor = EarnProcessor.Instance;
        isVisible = showOnStart;
    }
    private void Start()
    {
        // Start 시점에 Instance가 준비됐을 수 있어 한 번 더 캐시 + 현재값으로 입력칸 채움
        CacheManagersIfNeeded();
        SyncInputsFromManagers();
    }
    private void Update()
    {
        // 토글만 처리. 무거운 연산/할당 없음.
        if (Input.GetKeyDown(toggleKey))
            isVisible = !isVisible;
    }
    private void OnGUI()
    {
        if (!isVisible) return;
        CacheManagersIfNeeded();
        windowRect = GUI.Window(GetInstanceID(), windowRect, DrawWindow, "Debug Tool");
    }
    private void DrawWindow(int id)
    {
        GUILayout.Space(8f);
        // --- 1) 보유 코인 ---
        GUILayout.Label($"보유 코인: {(coinManager != null ? coinManager.Balance.ToString() : "-")}");
        GUILayout.BeginHorizontal();
        coinBalanceInput = GUILayout.TextField(coinBalanceInput, GUILayout.Width(160f));
        if (GUILayout.Button("코인 설정"))
            ApplyCoinBalance();
        GUILayout.EndHorizontal();
        GUILayout.Space(8f);
        // --- 2) 인풋 획득량 (클릭/타이핑) ---
        GUILayout.Label("인풋 획득 코인");
        GUILayout.BeginHorizontal();
        GUILayout.Label("클릭", GUILayout.Width(48f));
        clickCoinInput = GUILayout.TextField(clickCoinInput, GUILayout.Width(80f));
        if (GUILayout.Button("적용", GUILayout.Width(60f)))
            ApplyClickCoin();
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("타이핑", GUILayout.Width(48f));
        typingCoinInput = GUILayout.TextField(typingCoinInput, GUILayout.Width(80f));
        if (GUILayout.Button("적용", GUILayout.Width(60f)))
            ApplyTypingCoin();
        GUILayout.EndHorizontal();
        GUILayout.Space(8f);
        // --- 3) 시간당(주기당) 최대 획득량 ---
        if (earnProcessor != null)
        {
            GUILayout.Label(
                $"주기 한도: {earnProcessor.CurrentPeriodEarnedCoin} / {earnProcessor.MaxCoinPerHour}" +
                $"  (타이머 {earnProcessor.LimitTimer:0} / {earnProcessor.LimitPeriodSeconds:0}s)");
        }
        else
        {
            GUILayout.Label("EarnProcessor 없음");
        }
        GUILayout.BeginHorizontal();
        maxPerHourInput = GUILayout.TextField(maxPerHourInput, GUILayout.Width(160f));
        if (GUILayout.Button("시간당 한도 설정"))
            ApplyMaxPerHour();
        GUILayout.EndHorizontal();
        GUILayout.Space(8f);
        // --- 4) limitPeriodSeconds 주기 리셋 ---
        if (GUILayout.Button("획득 제한 시간 리셋 (limitPeriod)"))
            ResetEarnLimitPeriod();
        GUILayout.Space(8f);
        if (GUILayout.Button("입력칸 ← 현재값 동기화"))
            SyncInputsFromManagers();
        GUI.DragWindow();
    }
    private void CacheManagersIfNeeded()
    {
        // Instance가 늦게 준비되는 씬 순서 대비. null일 때만 대입해서 불필요한 참조 교체를 피합니다.
        if (coinManager == null)
            coinManager = CoinManager.Instance;
        if (earnProcessor == null)
            earnProcessor = EarnProcessor.Instance;
    }
    private void SyncInputsFromManagers()
    {
        if (coinManager != null)
            coinBalanceInput = coinManager.Balance.ToString();
        if (earnProcessor != null)
        {
            clickCoinInput = earnProcessor.BaseCoinPerClick.ToString();
            typingCoinInput = earnProcessor.BaseCoinPerTyping.ToString();
            maxPerHourInput = earnProcessor.MaxCoinPerHour.ToString();
        }
    }
    private void ApplyCoinBalance()
    {
        if (coinManager == null)
        {
            Debug.LogWarning("[DebugTool] CoinManager가 없습니다.");
            return;
        }
        if (!long.TryParse(coinBalanceInput, out long value))
        {
            Debug.LogWarning("[DebugTool] 코인 값이 숫자가 아닙니다.");
            return;
        }
        coinManager.SetCoin(value);
    }
    private void ApplyClickCoin()
    {
        if (earnProcessor == null) return;
        if (!int.TryParse(clickCoinInput, out int value)) return;
        earnProcessor.SetBaseCoinPerClick(value);
    }
    private void ApplyTypingCoin()
    {
        if (earnProcessor == null) return;
        if (!int.TryParse(typingCoinInput, out int value)) return;
        earnProcessor.SetBaseCoinPerTyping(value);
    }
    private void ApplyMaxPerHour()
    {
        if (earnProcessor == null) return;
        if (!int.TryParse(maxPerHourInput, out int value)) return;
        earnProcessor.SetMaxCoinPerHour(value);
    }
    // EarnProcessor.limitTimer / currentPeriodEarnedCoin 을 0으로 돌립니다.
    private void ResetEarnLimitPeriod()
    {
        if (earnProcessor == null)
        {
            Debug.LogWarning("[DebugTool] EarnProcessor가 없습니다.");
            return;
        }
        earnProcessor.ResetLimitPeriod();
    }
}
#else
// 릴리즈 빌드에서는 컴포넌트만 비워 두어 실수로 남아 있어도 동작하지 않게 합니다.
public class DebugTool : MonoBehaviour { }
#endif
