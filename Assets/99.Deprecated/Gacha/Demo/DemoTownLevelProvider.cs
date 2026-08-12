using UnityEngine;

namespace TaskTown.Gacha.Demo
{
    // 마을 시스템이 아직 없는 상태에서 "마을 레벨에 따른 뽑기 확률 변동"을 눈으로 확인하기 위한 임시 컴포넌트입니다.
    // 실제 마을 시스템이 만들어지면 그쪽에서 ITownLevelProvider를 구현하고, GachaManagerBase가 참조하는 대상만 교체하면 됩니다.
    public class DemoTownLevelProvider : MonoBehaviour, ITownLevelProvider, IDemoOnlyProvider
    {
        [SerializeField] private int townLevel = 1;

        public int CurrentTownLevel => townLevel;

        public void IncreaseLevel()
        {
            townLevel++;
        }

        // 세이브 로드 시 저장된 마을 레벨로 복원하기 위한 세터입니다.
        public void SetLevel(int level)
        {
            townLevel = Mathf.Max(1, level);
        }
    }
}
