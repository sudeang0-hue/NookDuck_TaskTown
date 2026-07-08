using System;
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
        private int gachaCount;

        public event Action<GachaResult> OnGachaResolved;

        public int GachaCount => gachaCount;
        public long CurrentCost => GachaCostCalculator.CalculateCost(costConfig, gachaCount, GetTownLevel());

        protected virtual void Awake()
        {
            gachaSystem = new GachaSystem();
            townLevelProvider = townLevelProviderSource as ITownLevelProvider;
        }

        // UI/저장 시스템은 이 메서드를 호출하고 OnGachaResolved 이벤트로 결과를 받습니다.
        // 재화 차감 여부 판단은 UI/저장 담당 쪽에서 CurrentCost를 확인해 처리합니다.
        public virtual GachaResult Roll()
        {
            GachaResult result = gachaSystem.Roll(pool, GetTownLevel());
            gachaCount++;
            OnGachaResolved?.Invoke(result);
            return result;
        }

        private int GetTownLevel()
        {
            return townLevelProvider != null ? townLevelProvider.CurrentTownLevel : 1;
        }
    }
}
