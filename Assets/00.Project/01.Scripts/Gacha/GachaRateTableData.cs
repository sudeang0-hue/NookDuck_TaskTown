using System.Collections.Generic;
using UnityEngine;

namespace TaskTown.Gacha
{
    [System.Serializable]
    public class GradeWeight
    {
        public ItemGrade grade;
        [Min(0f)] public float weight;
    }

    [System.Serializable]
    public class LevelRateEntry
    {
        [Tooltip("이 마을 레벨부터 아래 확률이 적용됩니다. (다음 threshold 전까지 유지)")]
        [Min(1)] public int townLevelThreshold = 1;
        public List<GradeWeight> gradeWeights = new List<GradeWeight>();
    }

    // 마을 레벨 구간별 등급 확률(가중치) 테이블입니다.
    // 기획자는 코드 수정 없이 이 에셋의 Inspector 값만 바꿔서 확률을 조정할 수 있습니다.
    // 동물 뽑기/도구 뽑기가 같은 등급 확률 구조를 쓰고 싶다면 같은 에셋을 공유해서 참조하면 됩니다.
    [CreateAssetMenu(menuName = "TaskTown/Gacha/Rate Table", fileName = "New GachaRateTable")]
    public class GachaRateTableData : ScriptableObject
    {
        [SerializeField] private List<LevelRateEntry> levelRates = new List<LevelRateEntry>();

        // townLevel 이하 threshold 중 가장 큰 값을 가진 구간을 사용합니다.
        // 중간 레벨을 전부 정의하지 않아도, 마지막으로 정의된 구간이 그대로 이어서 적용됩니다.
        public IReadOnlyList<GradeWeight> GetWeightsForTownLevel(int townLevel)
        {
            LevelRateEntry best = null;
            for (int i = 0; i < levelRates.Count; i++)
            {
                LevelRateEntry entry = levelRates[i];
                if (entry.townLevelThreshold <= townLevel &&
                    (best == null || entry.townLevelThreshold > best.townLevelThreshold))
                {
                    best = entry;
                }
            }

            return best != null ? best.gradeWeights : System.Array.Empty<GradeWeight>();
        }
    }
}
