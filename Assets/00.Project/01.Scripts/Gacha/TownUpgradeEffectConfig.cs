using UnityEngine;

namespace TaskTown.Gacha
{
    // 마을 업그레이드가 코인 소비처 역할만 하는 게 아니라, 전체 생산량에 곱해지는
    // "생산 효율 버프"도 함께 주도록 하는 설정입니다. FinalProductionCalculator는
    // townUpgradeMultiplier를 파라미터로만 받고 있어서 이를 실제로 계산해 공급하는
    // 곳이 없었는데, 이 클래스가 그 역할을 합니다.
    // 기본값(레벨당 +10%)은 인플레이션 밸런스 시뮬레이션(simulate_game.py) 기준입니다.
    [System.Serializable]
    public class TownUpgradeEffectConfig
    {
        [Tooltip("마을 레벨 1당 전체 생산량에 곱해지는 효율 버프 증가분 (기본 10%)")]
        [SerializeField, Min(0f)] private float efficiencyBuffPerLevel = 0.1f;

        public float GetProductionMultiplier(int townLevel)
        {
            int level = townLevel < 1 ? 1 : townLevel;
            return 1f + (level - 1) * efficiencyBuffPerLevel;
        }
    }
}
