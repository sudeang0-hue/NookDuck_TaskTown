using System;
using System.Collections.Generic;
using UnityEngine;

namespace TaskTown.Gacha
{
    // 마을 레벨 구간별 마을 업그레이드 비용입니다. GachaCostConfig와 동일한 방식(레벨마다 비용 직접 지정)입니다.
    //
    // #20 리밸런스(레벨40 확장, 사용자 확인): 기존 레벨1~9 설계(총 60h, 최종비용 45,200,000코인)를
    // 40단계로 세분화했습니다. 마을레벨1→2는 신규 유저 온보딩을 위해 공식과 무관하게 27,000코인으로
    // 별도 할인했고, 레벨2부터는 뽑기와 동일한 성장률(1.1703배/레벨)로 재설계했습니다. 뽑기 레벨1을
    // 2,000코인으로 고정 + 전체 단일 성장률 적용하면서 뽑기 총량이 줄어든 만큼, 목표 완주시간을
    // 60h에서 50h로 낮추고 이 표 전체를 인상해 보전했습니다(N=1,000 검증 완료. Town_Coin_인플레이션_밸런스표.xlsx
    // 시뮬레이션_파라미터 시트와 동일한 값).
    [System.Serializable]
    public class TownUpgradeCostConfig
    {
        [SerializeField]
        private List<LevelCostEntry> levelCosts = new List<LevelCostEntry>
        {
            new LevelCostEntry { townLevelThreshold = 1, cost = 27000 },
            new LevelCostEntry { townLevelThreshold = 2, cost = 88800 },
            new LevelCostEntry { townLevelThreshold = 3, cost = 104000 },
            new LevelCostEntry { townLevelThreshold = 4, cost = 122000 },
            new LevelCostEntry { townLevelThreshold = 5, cost = 142000 },
            new LevelCostEntry { townLevelThreshold = 6, cost = 167000 },
            new LevelCostEntry { townLevelThreshold = 7, cost = 195000 },
            new LevelCostEntry { townLevelThreshold = 8, cost = 228000 },
            new LevelCostEntry { townLevelThreshold = 9, cost = 266000 },
            new LevelCostEntry { townLevelThreshold = 10, cost = 312000 },
            new LevelCostEntry { townLevelThreshold = 11, cost = 366000 },
            new LevelCostEntry { townLevelThreshold = 12, cost = 428000 },
            new LevelCostEntry { townLevelThreshold = 13, cost = 500000 },
            new LevelCostEntry { townLevelThreshold = 14, cost = 586000 },
            new LevelCostEntry { townLevelThreshold = 15, cost = 686000 },
            new LevelCostEntry { townLevelThreshold = 16, cost = 802000 },
            new LevelCostEntry { townLevelThreshold = 17, cost = 938000 },
            new LevelCostEntry { townLevelThreshold = 18, cost = 1100000 },
            new LevelCostEntry { townLevelThreshold = 19, cost = 1290000 },
            new LevelCostEntry { townLevelThreshold = 20, cost = 1500000 },
            new LevelCostEntry { townLevelThreshold = 21, cost = 1760000 },
            new LevelCostEntry { townLevelThreshold = 22, cost = 2060000 },
            new LevelCostEntry { townLevelThreshold = 23, cost = 2420000 },
            new LevelCostEntry { townLevelThreshold = 24, cost = 2820000 },
            new LevelCostEntry { townLevelThreshold = 25, cost = 3300000 },
            new LevelCostEntry { townLevelThreshold = 26, cost = 3860000 },
            new LevelCostEntry { townLevelThreshold = 27, cost = 4520000 },
            new LevelCostEntry { townLevelThreshold = 28, cost = 5300000 },
            new LevelCostEntry { townLevelThreshold = 29, cost = 6200000 },
            new LevelCostEntry { townLevelThreshold = 30, cost = 7240000 },
            new LevelCostEntry { townLevelThreshold = 31, cost = 8480000 },
            new LevelCostEntry { townLevelThreshold = 32, cost = 9920000 },
            new LevelCostEntry { townLevelThreshold = 33, cost = 11600000 },
            new LevelCostEntry { townLevelThreshold = 34, cost = 13600000 },
            new LevelCostEntry { townLevelThreshold = 35, cost = 15900000 },
            new LevelCostEntry { townLevelThreshold = 36, cost = 18600000 },
            new LevelCostEntry { townLevelThreshold = 37, cost = 21800000 },
            new LevelCostEntry { townLevelThreshold = 38, cost = 25400000 },
            new LevelCostEntry { townLevelThreshold = 39, cost = 29800000 },
        };

        // #20(엔드리스 사전 작업): 레벨39(최대 정의 구간) 이후에도 마을 레벨이 계속 오를 때(엔드리스 모드)
        // 적용할 비용 성장률입니다. 뽑기/마을업 공통 성장률(1.1703)을 그대로 반복합니다.
        [Tooltip("레벨39(최대 정의 구간) 이후 마을 레벨이 계속 오를 때(엔드리스) 적용할 비용 성장률")]
        [SerializeField] private float postMaxLevelGrowthRate = 1.1703f;

        // townLevel 이하 threshold 중 가장 큰 값을 가진 구간의 비용을 사용합니다.
        // 정의된 구간을 넘어서면(엔드리스 모드로 마을 레벨이 9를 초과하면) postMaxLevelGrowthRate만큼
        // 매 레벨 곱해서 계속 늘어납니다(#19 이전에는 마지막 값에서 멈춰서 늘지 않는 문제가 있었음).
        public long GetCostForTownLevel(int townLevel)
        {
            LevelCostEntry best = null;
            int maxDefinedThreshold = 0;
            for (int i = 0; i < levelCosts.Count; i++)
            {
                LevelCostEntry entry = levelCosts[i];
                if (entry.townLevelThreshold <= townLevel &&
                    (best == null || entry.townLevelThreshold > best.townLevelThreshold))
                {
                    best = entry;
                }
                if (entry.townLevelThreshold > maxDefinedThreshold)
                    maxDefinedThreshold = entry.townLevelThreshold;
            }

            if (best == null) return 0;
            if (townLevel <= maxDefinedThreshold) return best.cost;

            int extraLevels = townLevel - maxDefinedThreshold;
            return (long)(best.cost * Math.Pow(postMaxLevelGrowthRate, extraLevels));
        }
    }
}
