using System;
using TaskTown.Gacha;
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

    // -----------------------------------------------------------------------------
    // [ 2026.07.27 - Choi - 튜토리얼 기능 업데이트 ]
    // 기능: 세 업그레이드 중 하나의 구매 성공을 튜토리얼 진행 판정에 전달합니다.
    // -----------------------------------------------------------------------------
    /// <summary>
    /// 클릭, 타이핑, 도구 효율 중 하나의 업그레이드 구매가 성공했을 때 발생합니다.
    /// </summary>
    public event Action UpgradePurchased;

    [System.Serializable]
    public class UpgradeTrack
    {
        [SerializeField] private long baseCost;
        [SerializeField] private float costGrowthRate;
        [Tooltip("절대 상한(0 이하면 상한 없음). #19: 실제 유효 상한은 Min(마을 레벨, 이 값)로 계산됨")]
        [SerializeField] private int maxLevel;

        public int Level { get; private set; }

        // #19 리밸런스: 업그레이드 레벨 상한이 고정값이 아니라 마을 레벨과 나란히 올라가다
        // maxLevel(절대 상한)에서 같이 멈추도록 변경 (사용자 확인, simulate_game.py의
        // upgrade_max_level()과 동일한 공식). 엔드리스 모드 우회는 이 상한 값 자체를 건드리지 않고,
        // TownUpgradeManager가 IsEndlessMode()일 때 이 체크를 아예 건너뛰고 ForceLevelUp()을 쓰는
        // 방식으로 처리합니다(아래 ForceLevelUp 참고).
        public bool IsMaxLevelAt(int townLevel)
        {
            if (maxLevel <= 0) return false;
            int effectiveMax = Mathf.Min(Mathf.Max(townLevel, 1), maxLevel);
            return Level >= effectiveMax;
        }

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

        public bool TryLevelUp(int townLevel)
        {
            if (IsMaxLevelAt(townLevel)) return false;
            Level++;
            return true;
        }

        /// <summary>
        /// #19: 엔드리스 모드 전용. 상한 체크 없이 무조건 레벨을 올립니다.
        /// </summary>
        public void ForceLevelUp()
        {
            Level++;
        }

        public void SetLevel(int level)
        {
            Level = Mathf.Max(0, level);
        }
    }

    [Tooltip("ITownLevelProvider(+ IEndlessModeProvider)를 구현한 컴포넌트(VillageUpgradeUI_Manager)를 연결합니다. 비워두면 마을 레벨 1/일반 모드로 취급합니다.")]
    [SerializeField] private MonoBehaviour townLevelProviderSource;
    private ITownLevelProvider townLevelProvider;
    private IEndlessModeProvider endlessModeProvider;

    [Header("클릭 코인 업그레이드")]
    [SerializeField] private UpgradeTrack clickUpgrade = new UpgradeTrack(300, 1.6f, 10);
    [Tooltip("레벨당 EarnProcessor.ClickMultiplier 증가량(레벨 x 이 값을 매번 새로 계산해서 적용, 누적 아님)")]
    [SerializeField] private int clickMultiplierPerLevel = 1;
    [Tooltip("레벨당 시간당 획득 상한(maxCoinPerHour) 증가분")]
    [SerializeField] private int clickCapBonusPerLevel = 2000;

    [Header("타이핑 코인 업그레이드")]
    [SerializeField] private UpgradeTrack typingUpgrade = new UpgradeTrack(300, 1.6f, 10);
    [SerializeField] private int typingMultiplierPerLevel = 1;
    [SerializeField] private int typingCapBonusPerLevel = 2000;

    [Header("도구 효율 업그레이드 (기존 마을 레벨 자동 생산 보너스를 대체)")]
    [SerializeField] private UpgradeTrack toolEfficiencyUpgrade = new UpgradeTrack(60000, 1.6f, 10);
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

    public bool IsClickUpgradeMaxLevel => !IsEndlessMode() && clickUpgrade.IsMaxLevelAt(GetCurrentTownLevel());
    public bool IsTypingUpgradeMaxLevel => !IsEndlessMode() && typingUpgrade.IsMaxLevelAt(GetCurrentTownLevel());
    public bool IsToolEfficiencyUpgradeMaxLevel => !IsEndlessMode() && toolEfficiencyUpgrade.IsMaxLevelAt(GetCurrentTownLevel());

    // RealProductionTicker가 매 틱 참조합니다. 예전에는 마을 레벨이 자동으로 이 배율을 올려줬지만
    // 지금은 이 업그레이드를 구매한 만큼만 오릅니다.
    public float ToolEfficiencyMultiplier => 1f + toolEfficiencyUpgrade.Level * toolEfficiencyBonusPerLevel;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        townLevelProvider = townLevelProviderSource as ITownLevelProvider;
        endlessModeProvider = townLevelProviderSource as IEndlessModeProvider;
    }

    private int GetCurrentTownLevel()
    {
        return townLevelProvider != null ? townLevelProvider.CurrentTownLevel : 1;
    }

    // #19: 엔드리스 모드에서는 3종 업그레이드 상한을 전부 해제합니다(UpgradeTrack 자체는 건드리지 않고,
    // 이 상한 체크를 쓰는 쪽에서 먼저 차단).
    private bool IsEndlessMode()
    {
        return endlessModeProvider != null && endlessModeProvider.IsEndlessMode;
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
        int townLevel = GetCurrentTownLevel();
        bool endless = IsEndlessMode();
        if ((!endless && track.IsMaxLevelAt(townLevel)) || CoinManager.Instance == null)
            return false;

        long cost = track.GetNextCost();
        if (!CoinManager.Instance.TrySpend(cost))
            return false;

        if (endless) track.ForceLevelUp();
        else track.TryLevelUp(townLevel);
        onLeveledUp?.Invoke();

        // -----------------------------------------------------------------------------
        // [ 2026.07.27 - Choi - 튜토리얼 기능 업데이트 ]
        // 기능: 비용 차감과 레벨 상승이 끝난 경우에만 업그레이드 완료 이벤트를 보냅니다.
        // -----------------------------------------------------------------------------
        UpgradePurchased?.Invoke();
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

    //-----------------------26.07.29 KDH-----------------------------
    public bool DebugForceUpgradeClick()
    {
        return DebugForceUpgrade(clickUpgrade, ApplyEarnProcessorEffects);
    }
    public bool DebugForceUpgradeTyping()
    {
        return DebugForceUpgrade(typingUpgrade, ApplyEarnProcessorEffects);
    }
    public bool DebugForceUpgradeToolEfficiency()
    {
        return DebugForceUpgrade(toolEfficiencyUpgrade, null);
    }
    private bool DebugForceUpgrade(UpgradeTrack track, System.Action onLeveledUp)
    {
        // 코인/CoinManager 검사 없음. 최대 레벨만 막음(엔드리스 모드면 그마저도 없음).
        int townLevel = GetCurrentTownLevel();
        bool endless = IsEndlessMode();
        if (!endless && track.IsMaxLevelAt(townLevel)) return false;

        if (endless) track.ForceLevelUp();
        else track.TryLevelUp(townLevel);
        onLeveledUp?.Invoke();
        return true;
    }
    //------------------------------------------------------------------
}
