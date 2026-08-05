using System;
using System.Collections.Generic;
using UnityEngine;

namespace TaskTown.Gacha
{
    // 동물 뽑기/도구 뽑기가 공유하는 실행 로직입니다.
    // pool, costConfig만 다른 에셋/값으로 채워서 새 뽑기 종류를 쉽게 추가할 수 있습니다.
    // (예: 치장 아이템 뽑기가 생기면 이 클래스를 상속하는 컴포넌트만 하나 더 만들면 됩니다.)
    public abstract class GachaManagerBase : MonoBehaviour
    {
        [SerializeField] protected GachaPoolData pool;
        [SerializeField] protected GachaCostConfig costConfig;

        [Tooltip("ITownLevelProvider를 구현한 컴포넌트를 연결합니다. 비워두면 마을 레벨 1로 취급합니다.")]
        [SerializeField] private MonoBehaviour townLevelProviderSource;

        [Tooltip("IDifficultyProvider를 구현한 컴포넌트를 연결합니다. 비워두면 Normal 난이도로 취급합니다.")]
        [SerializeField] private MonoBehaviour difficultyProviderSource;

        private ITownLevelProvider townLevelProvider;
        private IDifficultyProvider difficultyProvider;
        private GachaSystem gachaSystem;

        public event Action<GachaResult> OnGachaResolved;

        public long CurrentCost => GetCost(1);

        protected virtual void Awake()
        {
            gachaSystem = new GachaSystem();
            townLevelProvider = ResolveProvider<ITownLevelProvider>(townLevelProviderSource);
            difficultyProvider = ResolveProvider<IDifficultyProvider>(difficultyProviderSource);
        }

        // 인스펙터에 연결된 값이 데모 전용(IDemoOnlyProvider) 구현체면, 씬에 다른 실제
        // 구현체가 있는지 먼저 찾아서 있으면 그걸 우선 사용합니다. 이렇게 하면 이 프리팹을
        // 새 씬(예: 03.MainScene)에 그대로 갖다놔도 별도 수동 배선 없이 그 씬의 진짜
        // 마을/난이도 시스템에 자동으로 연결됩니다. 데모 전용 씬처럼 다른 구현체가 없으면
        // 데모값을 그대로 사용합니다.
        private static T ResolveProvider<T>(MonoBehaviour explicitSource) where T : class
        {
            T explicitProvider = explicitSource as T;
            if (explicitProvider != null && !(explicitProvider is IDemoOnlyProvider))
            {
                return explicitProvider;
            }

            foreach (MonoBehaviour candidate in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (candidate is T found && !(found is IDemoOnlyProvider))
                {
                    return found;
                }
            }

            return explicitProvider;
        }

        // rollCount번 뽑을 때 필요한 총 비용입니다. 10연뽑기 버튼 등에서 사용합니다.
        // 마을 레벨 구간별 비용은 costConfig에서 직접 관리하며(GachaCostConfig), rollCount는 단순 배수로 적용됩니다.
        public long GetCost(int rollCount)
        {
            return costConfig.GetCostForTownLevel(GetTownLevel()) * rollCount;
        }

        // UI/저장 시스템은 이 메서드를 호출하고 OnGachaResolved 이벤트로 결과를 받습니다.
        // 재화 차감 여부 판단은 UI/저장 담당 쪽에서 CurrentCost를 확인해 처리합니다.
        public virtual GachaResult Roll()
        {
            GachaResult result = gachaSystem.Roll(pool, GetTownLevel(), GetDifficulty());
            OnGachaResolved?.Invoke(result);
            return result;
        }

        // count번 연속으로 뽑습니다. 10연뽑기처럼 여러 번을 한 번에 처리할 때 사용합니다.
        public virtual List<GachaResult> RollMulti(int count)
        {
            List<GachaResult> results = new List<GachaResult>(count);
            for (int i = 0; i < count; i++)
            {
                results.Add(Roll());
            }

            return results;
        }

        private int GetTownLevel()
        {
            return townLevelProvider != null ? townLevelProvider.CurrentTownLevel : 1;
        }

        private DifficultyType GetDifficulty()
        {
            return difficultyProvider != null ? difficultyProvider.CurrentDifficulty : DifficultyType.Normal;
        }
    }
}
