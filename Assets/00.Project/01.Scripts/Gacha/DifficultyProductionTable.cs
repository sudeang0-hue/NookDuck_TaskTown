using System.Collections.Generic;
using UnityEngine;

namespace TaskTown.Gacha
{
    [System.Serializable]
    public class DifficultyMultiplierEntry
    {
        public DifficultyType difficulty;
        [Min(0f)] public float productionMultiplier = 1f;
    }

    // 난이도별 동물 기본 생산량 보정 배율을 관리합니다.
    // 기획자는 코드 수정 없이 이 에셋의 Inspector 값만 바꿔서 난이도 밸런스를 조정할 수 있습니다.
    [CreateAssetMenu(menuName = "TaskTown/Gacha/Difficulty Production Table", fileName = "New DifficultyProductionTable")]
    public class DifficultyProductionTable : ScriptableObject
    {
        [SerializeField] private List<DifficultyMultiplierEntry> entries = new List<DifficultyMultiplierEntry>();

        // 등록되지 않은 난이도는 배율 1(보정 없음)로 취급합니다.
        public float GetMultiplier(DifficultyType difficulty)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].difficulty == difficulty)
                {
                    return entries[i].productionMultiplier;
                }
            }

            return 1f;
        }
    }
}
