using System;
using System.Collections.Generic;
using UnityEngine;

namespace TaskTown.Gacha
{
    // 뽑기 1회를 처리하는 핵심 로직입니다. MonoBehaviour가 아닌 순수 C# 클래스로 두어
    // 어떤 GachaPoolData(동물/도구/추후 치장 아이템 등)에도 그대로 재사용할 수 있습니다.
    public class GachaSystem
    {
        private readonly IRandomProvider randomProvider;

        public GachaSystem(IRandomProvider randomProvider = null)
        {
            this.randomProvider = randomProvider ?? new UnityRandomProvider();
        }

        public GachaResult Roll(GachaPoolData pool, int townLevel)
        {
            if (pool == null)
            {
                throw new ArgumentNullException(nameof(pool));
            }

            ItemGrade grade = RollGrade(pool.RateTable, townLevel);
            GachaEntryData entry = RollEntry(pool.GetEntries(grade, townLevel));

            // 해당 등급에 아직 해금된(또는 등록된) 엔트리가 없으면 낮은 등급으로 대체합니다.
            if (entry == null)
            {
                entry = RollFallbackEntry(pool, grade, townLevel);
            }

            return new GachaResult(entry, grade);
        }

        private ItemGrade RollGrade(GachaRateTableData rateTable, int townLevel)
        {
            IReadOnlyList<GradeWeight> weights = rateTable != null
                ? rateTable.GetWeightsForTownLevel(townLevel)
                : Array.Empty<GradeWeight>();

            float totalWeight = 0f;
            for (int i = 0; i < weights.Count; i++)
            {
                totalWeight += Mathf.Max(0f, weights[i].weight);
            }

            if (totalWeight <= 0f)
            {
                return ItemGrade.Normal;
            }

            float roll = randomProvider.NextFloat01() * totalWeight;
            float accumulated = 0f;
            for (int i = 0; i < weights.Count; i++)
            {
                accumulated += Mathf.Max(0f, weights[i].weight);
                if (roll <= accumulated)
                {
                    return weights[i].grade;
                }
            }

            return weights[weights.Count - 1].grade;
        }

        private GachaEntryData RollEntry(IReadOnlyList<GachaEntryData> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return null;
            }

            int index = Mathf.Clamp(
                Mathf.FloorToInt(randomProvider.NextFloat01() * entries.Count),
                0, entries.Count - 1);

            return entries[index];
        }

        private GachaEntryData RollFallbackEntry(GachaPoolData pool, ItemGrade originalGrade, int townLevel)
        {
            for (int g = (int)originalGrade; g >= 0; g--)
            {
                IReadOnlyList<GachaEntryData> entries = pool.GetEntries((ItemGrade)g, townLevel);
                GachaEntryData entry = RollEntry(entries);
                if (entry != null)
                {
                    return entry;
                }
            }

            return null;
        }
    }
}
