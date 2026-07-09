using System.Collections.Generic;
using UnityEngine;

namespace TaskTown.Gacha
{
    [System.Serializable]
    public class LevelCostEntry
    {
        [Tooltip("이 마을 레벨부터 아래 비용이 적용됩니다. (다음 threshold 전까지 유지)")]
        [Min(1)] public int townLevelThreshold = 1;
        [Min(0)] public long cost = 100;
    }

    // 마을 레벨 구간별 뽑기 비용입니다. 공식으로 자동 계산하지 않고,
    // 레벨마다 비용을 직접 지정합니다. (GachaRateTableData의 levelRates와 같은 방식)
    [System.Serializable]
    public class GachaCostConfig
    {
        [SerializeField]
        private List<LevelCostEntry> levelCosts = new List<LevelCostEntry>
        {
            new LevelCostEntry { townLevelThreshold = 1, cost = 100 }
        };

        // townLevel 이하 threshold 중 가장 큰 값을 가진 구간의 비용을 사용합니다.
        // 중간 레벨을 전부 정의하지 않아도, 마지막으로 정의된 비용이 그대로 이어서 적용됩니다.
        public long GetCostForTownLevel(int townLevel)
        {
            LevelCostEntry best = null;
            for (int i = 0; i < levelCosts.Count; i++)
            {
                LevelCostEntry entry = levelCosts[i];
                if (entry.townLevelThreshold <= townLevel &&
                    (best == null || entry.townLevelThreshold > best.townLevelThreshold))
                {
                    best = entry;
                }
            }

            return best != null ? best.cost : 0;
        }
    }
}
