using UnityEngine;

// 클릭 코인 / 타이핑 코인 / 도구 효율, 세 가지 개별 업그레이드를 관리합니다.
// 기존에는 마을 레벨이 오르면 TownUpgradeEffectConfig가 전체 생산량에 자동으로 배율을 곱해줬지만
// (이슈 #72), 그 자동 보너스는 폐지하고 도구 효율 업그레이드를 직접 구매해야만 생산량 배율이
// 오르도록 바꿉니다. 클릭/타이핑 업그레이드는 EarnProcessor의 기존 Multiplier 필드에 연결합니다.
public class TownUpgradeManager : MonoBehaviour
{
    public static TownUpgradeManager Instance { get; private set; }

    [System.Serializable]
    public class UpgradeTrack
    {
        [SerializeField] private long baseCost = 5000;
        [SerializeField] private float costGrowthRate = 1.4f;
        [Tooltip("0 이하면 레벨 제한 없음")]
        [SerializeField] private int maxLevel = 20;

        public int Level { get; private set; }
        public bool IsMaxLevel => maxLevel > 0 && Level >= maxLevel;

        public long GetNextCost()
        {
            return (long)(baseCost * Mathf.Pow(costGrowthRate, Level));
        }

        public bool TryLevelUp()
        {
            if (IsMaxLevel) return false;
            Level++;
            return true;
        }

        public void SetLevel(int level)
        {
            Level = Mathf.Max(0, level);
        }
    }

    [Header("클릭 코인 업그레이드")]
    [SerializeField] private UpgradeTrack clickUpgrade = new UpgradeTrack();
    [Tooltip("레벨당 EarnProcessor.ClickMultiplier 증가량(레벨 x 이 값을 매번 새로 계산해서 적용, 누적 아님)")]
    [SerializeField] private int clickMultiplierPerLevel = 1;

    [Header("타이핑 코인 업그레이드")]
    [SerializeField] private UpgradeTrack typingUpgrade = new UpgradeTrack();
    [SerializeField] private int typingMultiplierPerLevel = 1;

    [Header("도구 효율 업그레이드 (기존 마을 레벨 자동 생산 보너스를 대체)")]
    [SerializeField] private UpgradeTrack toolEfficiencyUpgrade = new UpgradeTrack();
    [Tooltip("레벨당 전체 생산량에 곱해지는 효율 증가분")]
    [SerializeField, Min(0f)] private float toolEfficiencyBonusPerLevel = 0.1f;

    public int ClickLevel => clickUpgrade.Level;
    public int TypingLevel => typingUpgrade.Level;
    public int ToolEfficiencyLevel => toolEfficiencyUpgrade.Level;

    public long ClickUpgradeNextCost => clickUpgrade.GetNextCost();
    public long TypingUpgradeNextCost => typingUpgrade.GetNextCost();
    public long ToolEfficiencyUpgradeNextCost => toolEfficiencyUpgrade.GetNextCost();

    public bool IsClickUpgradeMaxLevel => clickUpgrade.IsMaxLevel;
    public bool IsTypingUpgradeMaxLevel => typingUpgrade.IsMaxLevel;
    public bool IsToolEfficiencyUpgradeMaxLevel => toolEfficiencyUpgrade.IsMaxLevel;

    // RealProductionTicker가 매 틱 참조합니다. 예전에는 마을 레벨이 자동으로 이 배율을 올려줬지만
    // 지금은 이 업그레이드를 구매한 만큼만 오릅니다.
    public float ToolEfficiencyMultiplier => 1f + toolEfficiencyUpgrade.Level * toolEfficiencyBonusPerLevel;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // SaveManager(실행 순서 -100)가 있으면 이미 LoadLevels로 반영했겠지만, 세이브가 없거나
        // SaveManager 없이 쓰는 씬에서도 EarnProcessor 배율이 항상 현재 레벨과 일치하도록 보정합니다.
        ApplyEarnProcessorEffects();
    }

    public bool TryUpgradeClick()
    {
        return TryUpgrade(clickUpgrade, ApplyEarnProcessorEffects);
    }

    public bool TryUpgradeTyping()
    {
        return TryUpgrade(typingUpgrade, ApplyEarnProcessorEffects);
    }

    public bool TryUpgradeToolEfficiency()
    {
        return TryUpgrade(toolEfficiencyUpgrade, null);
    }

    private bool TryUpgrade(UpgradeTrack track, System.Action onLeveledUp)
    {
        if (track.IsMaxLevel || CoinManager.Instance == null)
            return false;

        long cost = track.GetNextCost();
        if (!CoinManager.Instance.TrySpend(cost))
            return false;

        track.TryLevelUp();
        onLeveledUp?.Invoke();
        return true;
    }

    // EarnProcessor의 클릭/타이핑 배율을 현재 레벨 기준으로 다시 계산해서 덮어씁니다(누적 아님).
    private void ApplyEarnProcessorEffects()
    {
        if (EarnProcessor.Instance == null) return;

        EarnProcessor.Instance.ClickMultiplier = 1 + clickUpgrade.Level * clickMultiplierPerLevel;
        EarnProcessor.Instance.TypingMultiplier = 1 + typingUpgrade.Level * typingMultiplierPerLevel;
    }

    // SaveManager가 로드 완료 후 저장돼 있던 레벨을 그대로 복원할 때 사용합니다.
    public void LoadLevels(int clickLevel, int typingLevel, int toolEfficiencyLevel)
    {
        clickUpgrade.SetLevel(clickLevel);
        typingUpgrade.SetLevel(typingLevel);
        toolEfficiencyUpgrade.SetLevel(toolEfficiencyLevel);
        ApplyEarnProcessorEffects();
    }
}
