namespace TaskTown.Gacha
{
    // 동물/도구 등 뽑기 대상 전체가 공유하는 등급 체계입니다.
    // 향후 치장 아이템 등 새 뽑기 대상이 추가되어도 이 등급을 그대로 사용합니다.
    public enum ItemGrade
    {
        Normal,
        Rare,
        Epic,
        Unique,
        Legendary
    }
}
