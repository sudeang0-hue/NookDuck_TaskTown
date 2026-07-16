using System.Collections.Generic;
using UnityEngine;

namespace TaskTown.Gacha
{
    [System.Serializable]
    public class GradeEntryGroup
    {
        public ItemGrade grade;
        public List<GachaEntryData> entries = new List<GachaEntryData>();
    }

    // 뽑기 한 종류(동물 뽑기, 도구 뽑기, 추후 치장 아이템 뽑기 등)를 정의하는 에셋입니다.
    // GachaEntryData를 상속한 새 데이터 타입이 생겨도 이 클래스는 그대로 재사용합니다.
    [CreateAssetMenu(menuName = "TaskTown/Gacha/Gacha Pool", fileName = "New GachaPool")]
    public class GachaPoolData : ScriptableObject
    {
        [SerializeField] private GachaRateTableData rateTable;
        [SerializeField] private List<GradeEntryGroup> entryGroups = new List<GradeEntryGroup>();

        public GachaRateTableData RateTable => rateTable;

        // townLevel 미만에서 해금되는(unlockTownLevel <= townLevel) 종류만 반환합니다.
        // 아직 해금되지 않은 종류는 등급 확률에 걸리더라도 뽑히지 않습니다.
        public IReadOnlyList<GachaEntryData> GetEntries(ItemGrade grade, int townLevel)
        {
            for (int i = 0; i < entryGroups.Count; i++)
            {
                if (entryGroups[i].grade == grade)
                {
                    List<GachaEntryData> entries = entryGroups[i].entries;
                    List<GachaEntryData> unlocked = new List<GachaEntryData>(entries.Count);
                    for (int e = 0; e < entries.Count; e++)
                    {
                        if (entries[e] != null && entries[e].UnlockTownLevel <= townLevel)
                        {
                            unlocked.Add(entries[e]);
                        }
                    }

                    return unlocked;
                }
            }

            return System.Array.Empty<GachaEntryData>();
        }
    }
}
