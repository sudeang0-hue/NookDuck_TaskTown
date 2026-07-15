namespace TaskTown.Gacha.Tests
{
    // 테스트 전용 최소 구현체입니다. GachaEntryData(동물 역할)를 인스턴스화하기 위한 용도로만 씁니다.
    // 실제 동물 데이터는 Animal.Data.AnimalDataSO(Assembly-CSharp)에 있지만, 이 테스트 어셈블리에서는
    // 참조할 수 없어(순환 참조) 공통 로직만 검증하는 최소 타입을 별도로 둡니다.
    public class TestAnimalEntry : GachaEntryData
    {
    }

    // 테스트 전용 최소 구현체입니다. ISpecialToolEntry(도구 역할, 특화 보너스 포함)를 검증하기 위한 용도입니다.
    public class TestToolEntry : GachaEntryData, ISpecialToolEntry
    {
        [UnityEngine.SerializeField] private string specialAnimalId;
        [UnityEngine.SerializeField] private float specialAnimalBonusRate;

        public string SpecialAnimalId => specialAnimalId;
        public float SpecialAnimalBonusRate => specialAnimalBonusRate;
    }
}
