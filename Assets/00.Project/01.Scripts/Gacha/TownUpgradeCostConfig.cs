using System.Collections.Generic;
using UnityEngine;

namespace TaskTown.Gacha
{
    // 마을 레벨 구간별 마을 업그레이드 비용입니다. GachaCostConfig와 동일한 방식(레벨마다 비용 직접 지정)이며,
    // 마을 업그레이드는 돈으로 하는 핵심 소비처라 뽑기/레벨업 비용보다 훨씬 비싸게 기본값을 잡는다.
    // 기본값은 인플레이션 밸런스 시뮬레이션(simulate_game.py) 역산 결과입니다. 성장률 공식 하나로는
    // "레벨1에 노말+레어를 미리 다 풀어주면서도 낮은 레벨이 높은 레벨보다 비중이 크면 안 된다"는
    // 조건을 동시에 만족시킬 수 없어서, 레벨 1~9 목표 소요시간에 맞춰 레벨별 비용을 직접 역산했습니다.
    //
    // #18 리밸런스(도구 상한 기능 추가, 이슈 #72 후속): 도구 상한(동시에 생산 가능한 도구 슬롯 개수
    // 제한, InventoryManager_Tool 참고)이 추가되면서 이 비용 곡선도 다시 역산했습니다. 상한 도입 후
    // 경제구조에서는 "완전한 단조증가"와 "정확히 목표 총시간"을 동시에 만족하는 지점이 없어서
    // (구간 하나를 늘리면 다른 구간이 깨지는 현상을 여러 방법으로 반복 확인), 완전한 단조증가가
    // 자연스럽게 성립하는 지점을 찾아 총 78.6h로 확정했습니다(N=1000, 레벨8~10 비중 46.8%,
    // 사용자 확인 완료. simulate_game.py의 TOWN_UPGRADE_COSTS와 동일한 값).
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
            new LevelCostEntry { townLevelThreshold = 9, cost = 44300000 },
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
