using UnityEngine;

namespace TaskTown.Gacha
{
    // 뽑기로 나올 수 있는 모든 대상(동물, 도구, 추후 치장 아이템 등)의 공통 베이스입니다.
    // 새 뽑기 대상을 추가하려면 이 클래스만 상속받으면 GachaSystem/GachaPoolData를 그대로 재사용할 수 있습니다.
    public abstract class GachaEntryData : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private ItemGrade grade;
        [SerializeField] private Sprite icon;

        [Tooltip("이 종류가 뽑기에 등장하기 시작하는 마을 레벨입니다. 그 전까지는 잠겨 있어서 뽑히지 않습니다.")]
        [SerializeField, Min(1)] private int unlockTownLevel = 1;

        public string Id => id;
        public string DisplayName => displayName;
        public ItemGrade Grade => grade;
        public Sprite Icon => icon;
        public int UnlockTownLevel => unlockTownLevel;
    }
}
