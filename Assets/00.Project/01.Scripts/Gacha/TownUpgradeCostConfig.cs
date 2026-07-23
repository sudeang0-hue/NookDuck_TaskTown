using System.Collections.Generic;
using UnityEngine;

namespace TaskTown.Gacha
{
    // 마을 레벨 구간별 마을 업그레이드 비용입니다. GachaCostConfig와 동일한 방식(레벨마다 비용 직접 지정)이며,
    // 마을 업그레이드는 돈으로 하는 핵심 소비처라 뽑기/레벨업 비용보다 훨씬 비싸게 기본값을 잡는다.
    // 기본값은 인플레이션 밸런스 시뮬레이션(simulate_game.py) 역산 결과입니다. 성장률 공식 하나로는
    // "레벨1에 노말+레어를 미리 다 풀어주면서도 낮은 레벨이 높은 레벨보다 비중이 크면 안 된다"는
    // 조건을 동시에 만족시킬 수 없어서, 레벨 1~9 목표 소요시간(총 50시간, 레벨 8~10 비중 약 50%)에
    // 맞춰 레벨별 비용을 직접 역산했습니다. (중복 성장배수 2 + Legendary 생산량 34 + 뽑기 기본비용
    // 1500 반영 최종값에서 1만 단위로 반올림)
    [System.Serializable]
    public class TownUpgradeCostConfig
    {
        [SerializeField]
        private List<LevelCostEntry> levelCosts = new List<LevelCostEntry>
        {
            new LevelCostEntry { townLevelThreshold = 1, cost = 400000 },
            new LevelCostEntry { townLevelThreshold = 2, cost = 1470000 },
            new LevelCostEntry { townLevelThreshold = 3, cost = 2410000 },
            new LevelCostEntry { townLevelThreshold = 4, cost = 3370000 },
            new LevelCostEntry { townLevelThreshold = 5, cost = 4770000 },
            new LevelCostEntry { townLevelThreshold = 6, cost = 7050000 },
            new LevelCostEntry { townLevelThreshold = 7, cost = 11480000 },
            new LevelCostEntry { townLevelThreshold = 8, cost = 21440000 },
            new LevelCostEntry { townLevelThreshold = 9, cost = 33000000 },
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
