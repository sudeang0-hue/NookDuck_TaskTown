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
    // 레벨마다 비용을 직접 지정합니다.
    //
    // #20 리밸런스(레벨40 확장, 사용자 확인): 레벨1을 2,000코인으로 고정하고,
    // 레벨1~39 전체에 단일 성장률(1.1703배/레벨, 마을업그레이드와 동일)를 일관 적용했습니다
    // (레벨별 할인 taper 없음, N=1,000 검증 완료. Town_Coin_인플레이션_밸런스표.xlsx
    // 시뮬레이션_파라미터 시트와 동일한 값).
    [System.Serializable]
    public class GachaCostConfig
    {
        [SerializeField]
        private List<LevelCostEntry> levelCosts = new List<LevelCostEntry>
        {
            new LevelCostEntry { townLevelThreshold = 1, cost = 2000 },
            new LevelCostEntry { townLevelThreshold = 2, cost = 2340 },
            new LevelCostEntry { townLevelThreshold = 3, cost = 2740 },
            new LevelCostEntry { townLevelThreshold = 4, cost = 3210 },
            new LevelCostEntry { townLevelThreshold = 5, cost = 3750 },
            new LevelCostEntry { townLevelThreshold = 6, cost = 4390 },
            new LevelCostEntry { townLevelThreshold = 7, cost = 5140 },
            new LevelCostEntry { townLevelThreshold = 8, cost = 6010 },
            new LevelCostEntry { townLevelThreshold = 9, cost = 7040 },
            new LevelCostEntry { townLevelThreshold = 10, cost = 8230 },
            new LevelCostEntry { townLevelThreshold = 11, cost = 9630 },
            new LevelCostEntry { townLevelThreshold = 12, cost = 11300 },
            new LevelCostEntry { townLevelThreshold = 13, cost = 13200 },
            new LevelCostEntry { townLevelThreshold = 14, cost = 15400 },
            new LevelCostEntry { townLevelThreshold = 15, cost = 18100 },
            new LevelCostEntry { townLevelThreshold = 16, cost = 21100 },
            new LevelCostEntry { townLevelThreshold = 17, cost = 24700 },
            new LevelCostEntry { townLevelThreshold = 18, cost = 29000 },
            new LevelCostEntry { townLevelThreshold = 19, cost = 33900 },
            new LevelCostEntry { townLevelThreshold = 20, cost = 39700 },
            new LevelCostEntry { townLevelThreshold = 21, cost = 46400 },
            new LevelCostEntry { townLevelThreshold = 22, cost = 54300 },
            new LevelCostEntry { townLevelThreshold = 23, cost = 63600 },
            new LevelCostEntry { townLevelThreshold = 24, cost = 74400 },
            new LevelCostEntry { townLevelThreshold = 25, cost = 87000 },
            new LevelCostEntry { townLevelThreshold = 26, cost = 102000 },
            new LevelCostEntry { townLevelThreshold = 27, cost = 119000 },
            new LevelCostEntry { townLevelThreshold = 28, cost = 140000 },
            new LevelCostEntry { townLevelThreshold = 29, cost = 163000 },
            new LevelCostEntry { townLevelThreshold = 30, cost = 191000 },
            new LevelCostEntry { townLevelThreshold = 31, cost = 224000 },
            new LevelCostEntry { townLevelThreshold = 32, cost = 262000 },
            new LevelCostEntry { townLevelThreshold = 33, cost = 306000 },
            new LevelCostEntry { townLevelThreshold = 34, cost = 358000 },
            new LevelCostEntry { townLevelThreshold = 35, cost = 419000 },
            new LevelCostEntry { townLevelThreshold = 36, cost = 491000 },
            new LevelCostEntry { townLevelThreshold = 37, cost = 574000 },
            new LevelCostEntry { townLevelThreshold = 38, cost = 672000 },
            new LevelCostEntry { townLevelThreshold = 39, cost = 786000 },
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
