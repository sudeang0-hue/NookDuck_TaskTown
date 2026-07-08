using System;
using UnityEngine;

namespace TaskTown.Gacha
{
    public static class GachaCostCalculator
    {
        public static long CalculateCost(GachaCostConfig config, int gachaCount, int townLevel)
        {
            float townMultiplier = 1f + Mathf.Max(0, townLevel - 1) * config.townLevelCostMultiplierPerLevel;
            double cost = config.baseCost
                * Math.Pow(config.costIncreaseRate, gachaCount)
                * townMultiplier;

            return (long)Math.Ceiling(cost);
        }
    }
}
