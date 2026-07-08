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

        public string Id => id;
        public string DisplayName => displayName;
        public ItemGrade Grade => grade;
        public Sprite Icon => icon;
    }
}
