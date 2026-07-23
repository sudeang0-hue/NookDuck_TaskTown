using UnityEngine;

// 클릭 코인 / 타이핑 코인 / 도구 효율, 세 가지 개별 업그레이드를 관리합니다.
// 기존에는 마을 레벨이 오르면 TownUpgradeEffectConfig가 전체 생산량에 자동으로 배율을 곱해줬지만
// (이슈 #72), 그 자동 보너스는 폐지하고 도구 효율 업그레이드를 직접 구매해야만 생산량 배율이
// 오르도록 바꿉니다. 클릭/타이핑 업그레이드는 EarnProcessor의 기존 Multiplier뿐 아니라 시간당
// 획득 상한(maxCoinPerHour)도 함께 올려서, 이미 상한을 채우는 플레이어에게도 실질적인 생산량
// 증가 효과를 갖습니다(상한이 배율과 무관하게 고정이면 상한을 채우는 플레이어는 배율을 올려도
// 얻는 게 없어서 - 사용자 확인 후 상한도 함께 올리는 방향으로 확정).
//
// 아래 비용/효과 기본값은 simulate_game.py의 N=1000 시뮬레이션으로 검증한 값입니다
// (완주 58~60h대, 레벨8~10 비중 48.5%, 레벨별 비중 단조증가 확인. 셋 다 성장률을 1.6으로
// 통일했을 때만 단조증가가 안정적으로 유지됨 - 하나라도 더 낮으면 해당 업그레이드가 중후반에
// 몰아 사는 구간이 생겨 레벨9->10 구간이 비정상적으로 짧아지는 문제가 있었음).
public class TownUpgradeManager : MonoBehaviour
{
    public static TownUpgradeManager Instance { get; private set; }

    [System.Serializable]
    public class UpgradeTrack
    {
        [SerializeField] private long baseCost;
        [SerializeField] private float costGrowthRate;
        [Tooltip("0 이하면 레벨 제한 없음")]
        [SerializeField] private int maxLevel;

        public int Level { get; private set; }
        public bool IsMaxLevel => maxLevel > 0 && Level >= maxLevel;

        public UpgradeTrack(long baseCost, float costGrowthRate, int maxLevel)
        {
            this.baseCost = baseCost;
            this.costGrowthRate = costGrowthRate;
            this.maxLevel = maxLevel;
        }

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
    [SerializeField] private UpgradeTrack clickUpgrade = new UpgradeTrack(300, 1.6f, 20);
    [Tooltip("레벨당 EarnProcessor.ClickMultiplier 증가량(레벨 x 이 값을 매번 새로 계산해서 적용, 누적 아님)")]
    [SerializeField] private int clickMultiplierPerLevel = 1;
    [Tooltip("레벨당 시간당 획득 상한(maxCoinPerHour) 증가분")]
    [SerializeField] private int clickCapBonusPerLevel = 2000;

    [Header("타이핑 코인 업그레이드")]
    [SerializeField] private UpgradeTrack typingUpgrade = new UpgradeTrack(300, 1.6f, 20);
    [SerializeField] private int typingMultiplierPerLevel = 1;
    [SerializeField] private int typingCapBonusPerLevel = 2000;

    [Header("도구 효율 업그레이드 (기존 마을 레벨 자동 생산 보너스를 대체)")]
    [SerializeField] private UpgradeTrack toolEfficiencyUpgrade = new UpgradeTrack(60000, 1.6f, 20);
    [Tooltip("레벨당 전체 생산량에 곱해지는 효율 증가분")]
    [SerializeField, Min(0f)] private float toolEfficiencyBonusPerLevel = 0.1f;

    // EarnProcessor의 원래(업그레이드 반영 전) 시간당 획득 상한. Start에서 한 번만 캐시해서,
    // 매번 이 값 기준으로 클릭/타이핑 레벨의 상한 보너스를 더해 재계산합니다.
    private int baseMaxCoinPerHour = -1;

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
        if (EarnProcessor.Instance != null && baseMaxCoinPerHour < 0)
            baseMaxCoinPerHour = EarnProcessor.Instance.MaxCoinPerHour;

        // SaveManager(실행 순서 -100)가 있으면 이미 LoadLevels로 반영했겠지만, 세이브가 없거나
        // SaveManager 없이 쓰는 씬에서도 EarnProcessor 배율/상한이 항상 현재 레벨과 일치하도록 보정합니다.
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

    // EarnProcessor의 클릭/타이핑 배율과 시간당 획득 상한을 현재 레벨 기준으로 다시 계산해서
    // 덮어씁니다(누적 아님).
    private void ApplyEarnProcessorEffects()
    {
        if (EarnProcessor.Instance == null) return;

        EarnProcessor.Instance.ClickMultiplier = 1 + clickUpgrade.Level * clickMultiplierPerLevel;
        EarnProcessor.Instance.TypingMultiplier = 1 + typingUpgrade.Level * typingMultiplierPerLevel;

        if (baseMaxCoinPerHour >= 0)
        {
            int newCap = baseMaxCoinPerHour
                + clickUpgrade.Level * clickCapBonusPerLevel
                + typingUpgrade.Level * typingCapBonusPerLevel;
            EarnProcessor.Instance.SetMaxCoinPerHour(newCap);
        }
    }

    // SaveManager가 로드 완료 후 저장돼 있던 레벨을 그대로 복원할 때 사용합니다.
    public void LoadLevels(int clickLevel, int typingLevel, int toolEfficiencyLevel)
    {
        if (EarnProcessor.Instance != null && baseMaxCoinPerHour < 0)
            baseMaxCoinPerHour = EarnProcessor.Instance.MaxCoinPerHour;

        clickUpgrade.SetLevel(clickLevel);
        typingUpgrade.SetLevel(typingLevel);
        toolEfficiencyUpgrade.SetLevel(toolEfficiencyLevel);
        ApplyEarnProcessorEffects();
    }
}
