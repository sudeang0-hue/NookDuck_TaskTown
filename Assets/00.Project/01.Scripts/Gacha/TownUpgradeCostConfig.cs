using System.Collections.Generic;
using UnityEngine;

namespace TaskTown.Gacha
{
    // 마을 레벨 구간별 마을 업그레이드 비용입니다. GachaCostConfig와 동일한 방식(레벨마다 비용 직접 지정)이며,
    // 마을 업그레이드는 돈으로 하는 핵심 소비처라 뽑기/레벨업 비용보다 훨씬 비싸게 기본값을 잡는다.
    // 기본값은 마을 최대 레벨(10)까지 미리 채워둬서, 값을 안 채운 구간이 그대로 고정되는 일이 없게 한다.
    [System.Serializable]
    public class TownUpgradeCostConfig
    {
        [SerializeField]
        private List<LevelCostEntry> levelCosts = new List<LevelCostEntry>
        {
            new LevelCostEntry { townLevelThreshold = 1, cost = 1000 },
            new LevelCostEntry { townLevelThreshold = 2, cost = 2000 },
            new LevelCostEntry { townLevelThreshold = 3, cost = 4000 },
            new LevelCostEntry { townLevelThreshold = 4, cost = 8000 },
            new LevelCostEntry { townLevelThreshold = 5, cost = 16000 },
            new LevelCostEntry { townLevelThreshold = 6, cost = 32000 },
            new LevelCostEntry { townLevelThreshold = 7, cost = 64000 },
            new LevelCostEntry { townLevelThreshold = 8, cost = 128000 },
            new LevelCostEntry { townLevelThreshold = 9, cost = 256000 },
            new LevelCostEntry { townLevelThreshold = 10, cost = 512000 },
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
