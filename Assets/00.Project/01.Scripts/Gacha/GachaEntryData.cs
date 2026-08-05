using UnityEngine;

namespace TaskTown.Gacha
{
    // 뽑기로 나올 수 있는 모든 대상(동물, 도구, 추후 치장 아이템 등)의 공통 베이스입니다.
    // 새 뽑기 대상을 추가하려면 이 클래스만 상속받으면 GachaSystem/GachaPoolData를 그대로 재사용할 수 있습니다.
    // 생산량/레벨업 관련 필드와 계산 로직도 여기 공통으로 둬서, AnimalDataSO/ToolDataSO 등
    // 어떤 구체 타입을 쓰든 FinalProductionCalculator가 동일하게 재사용할 수 있게 합니다.
    public abstract class GachaEntryData : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private ItemGrade grade;
        [SerializeField] private Sprite icon;

        [Tooltip("이 종류가 뽑기에 등장하기 시작하는 마을 레벨입니다. 그 전까지는 잠겨 있어서 뽑히지 않습니다.")]
        [SerializeField, Min(1)] private int unlockTownLevel = 1;

        [SerializeField] private float baseCoinPerSecond;
        [SerializeField, Min(0f)] private float levelBonusRatePerLevel = 1.0f;   // 레벨당 +100%(=2배) 기본값(등급 공통, 2026.08.05 상향)
        // Lv1->2 코인 비용. 등급별 baseCoinPerSecond(1/2/4/8/34)에 정비례하도록 에셋별로 다르게
        // 설정합니다(Normal 20000 / Rare 40000 / Epic 80000 / Unique 160000 / Legendary 680000,
        // Normal 기준값 2만 코인 - 사용자 확인) - 회수시간이 등급과 무관하게 항상 동일
        // (20000 x levelUpCostIncreaseRate^(레벨-1)초, levelBonusRatePerLevel=1.0 기준)해지도록
        // 유도한 값입니다. 클래스 기본값 20000은 Normal 기준값입니다.
        [SerializeField, Min(0)] private long levelUpBaseCost = 20000;
        [SerializeField, Min(1f)] private float levelUpCostIncreaseRate = 1.5f;  // 레벨당 x1.5(등급 공통)

        public string Id => id;
        public string DisplayName => displayName;
        public ItemGrade Grade => grade;
        public Sprite Icon => icon;
        public int UnlockTownLevel => unlockTownLevel;
        public float BaseCoinPerSecond => baseCoinPerSecond;

        public float CalculateLevelMultiplier(int level)
        {
            return 1f + Mathf.Max(0, level - 1) * levelBonusRatePerLevel;
        }

        public long CalculateLevelUpCoinCost(int currentLevel)
        {
            double cost = levelUpBaseCost * System.Math.Pow(levelUpCostIncreaseRate, Mathf.Max(0, currentLevel - 1));
            return (long)System.Math.Ceiling(cost);
        }
    }
}
