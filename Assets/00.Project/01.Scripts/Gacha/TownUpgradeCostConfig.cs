using System.Collections.Generic;
using UnityEngine;

namespace TaskTown.Gacha
{
    // 마을 레벨 구간별 마을 업그레이드 비용입니다. GachaCostConfig와 동일한 방식(레벨마다 비용 직접 지정)이며,
    // 마을 업그레이드는 돈으로 하는 핵심 소비처라 뽑기/레벨업 비용보다 훨씬 비싸게 기본값을 잡는다.
    [System.Serializable]
    public class TownUpgradeCostConfig
    {
        [SerializeField]
        private List<LevelCostEntry> levelCosts = new List<LevelCostEntry>
        {
            new LevelCostEntry { townLevelThreshold = 1, cost = 1000 }
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
