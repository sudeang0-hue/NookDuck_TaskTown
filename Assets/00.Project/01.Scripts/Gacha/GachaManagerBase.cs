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

        private ITownLevelProvider townLevelProvider;
        private GachaSystem gachaSystem;

        public event Action<GachaResult> OnGachaResolved;

        public long CurrentCost => GetCost(1);

        protected virtual void Awake()
        {
            gachaSystem = new GachaSystem();
            townLevelProvider = townLevelProviderSource as ITownLevelProvider;
        }

        // rollCount번 뽑을 때 필요한 총 비용입니다. 10연뽑기 버튼 등에서 사용합니다.
        public long GetCost(int rollCount)
        {
            return costConfig.cost * rollCount;
        }

        // UI/저장 시스템은 이 메서드를 호출하고 OnGachaResolved 이벤트로 결과를 받습니다.
        // 재화 차감 여부 판단은 UI/저장 담당 쪽에서 CurrentCost를 확인해 처리합니다.
        public virtual GachaResult Roll()
        {
            GachaResult result = gachaSystem.Roll(pool, GetTownLevel());
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
    }
}
