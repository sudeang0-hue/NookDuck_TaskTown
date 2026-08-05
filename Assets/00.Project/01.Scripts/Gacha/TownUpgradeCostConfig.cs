using System;
using System.Collections.Generic;
using UnityEngine;

namespace TaskTown.Gacha
{
    // 마을 레벨 구간별 마을 업그레이드 비용입니다. GachaCostConfig와 동일한 방식(레벨마다 비용 직접 지정)입니다.
    //
    // 레벨40 확장(#20) 실험을 철회하고 레벨1~9 설계로 되돌렸습니다(사용자 확인). 아래 표는
    // #19 리밸런스 시점(클릭/타이핑/도구효율 업그레이드 상한 Min(마을레벨,10) 도입 후, 완주
    // 59.95h, N=1000 검증 완료)의 값입니다.
    [System.Serializable]
    public class TownUpgradeCostConfig
    {
        [SerializeField]
        private List<LevelCostEntry> levelCosts = new List<LevelCostEntry>
        {
            new LevelCostEntry { townLevelThreshold = 1, cost = 77000 },
            new LevelCostEntry { townLevelThreshold = 2, cost = 425000 },
            new LevelCostEntry { townLevelThreshold = 3, cost = 1220000 },
            new LevelCostEntry { townLevelThreshold = 4, cost = 2610000 },
            new LevelCostEntry { townLevelThreshold = 5, cost = 7820000 },
            new LevelCostEntry { townLevelThreshold = 6, cost = 15050000 },
            new LevelCostEntry { townLevelThreshold = 7, cost = 21450000 },
            new LevelCostEntry { townLevelThreshold = 8, cost = 35700000 },
            new LevelCostEntry { townLevelThreshold = 9, cost = 45200000 },
        };

        // #19(엔드리스 사전 작업): 레벨9(최대 정의 구간) 이후에도 마을 레벨이 계속 오를 때(엔드리스 모드)
        // 적용할 비용 성장률입니다. 기본값은 레벨9->10 구간의 실제 성장률을 그대로 반복합니다
        // (45,200,000 / 35,700,000 ≈ 1.2661, 사용자 확인 - "레벨9->10 배율을 그대로 반복" 방식).
        [Tooltip("레벨9(최대 정의 구간) 이후 마을 레벨이 계속 오를 때(엔드리스) 적용할 비용 성장률")]
        [SerializeField] private float postMaxLevelGrowthRate = 1.2661f;

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
