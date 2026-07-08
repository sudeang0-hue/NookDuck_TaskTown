using UnityEngine;

namespace TaskTown.Gacha
{
    // 기획서 공식: currentCost = baseCost * pow(costIncreaseRate, gachaCount) * townLevelMultiplier
    [System.Serializable]
    public class GachaCostConfig
    {
        [Min(0)] public long baseCost = 100;

        [Tooltip("뽑기 1회당 비용 증가율. 1.05 = 매 뽑기마다 5%씩 증가")]
        [Min(1f)] public float costIncreaseRate = 1.05f;

        [Tooltip("마을 레벨 1당 비용 배율 증가치. 0.1 = 레벨 1당 10%씩 증가")]
        [Min(0f)] public float townLevelCostMultiplierPerLevel = 0.1f;
    }
}
