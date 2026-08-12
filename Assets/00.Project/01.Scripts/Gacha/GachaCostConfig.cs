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
    // 레벨40 확장(#20) 실험을 철회하고 레벨1~9 설계로 되돌렸습니다(사용자 확인). 아래 표는
    // 인플레이션 밸런스 시뮬레이션(simulate_game.py) 역산 결과: 레벨1 1,500 코인, 레벨당 1.5배
    // 성장에서 100단위로 반올림한 값입니다.
    [System.Serializable]
    public class GachaCostConfig
    {
        [SerializeField]
        private List<LevelCostEntry> levelCosts = new List<LevelCostEntry>
        {
            new LevelCostEntry { townLevelThreshold = 1, cost = 1500 },
            new LevelCostEntry { townLevelThreshold = 2, cost = 2200 },
            new LevelCostEntry { townLevelThreshold = 3, cost = 3400 },
            new LevelCostEntry { townLevelThreshold = 4, cost = 5100 },
            new LevelCostEntry { townLevelThreshold = 5, cost = 7600 },
            new LevelCostEntry { townLevelThreshold = 6, cost = 11400 },
            new LevelCostEntry { townLevelThreshold = 7, cost = 17100 },
            new LevelCostEntry { townLevelThreshold = 8, cost = 25600 },
            new LevelCostEntry { townLevelThreshold = 9, cost = 38400 },
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
