using TaskTown.Gacha;
using UnityEngine;


namespace Tool.Data
{
    // 생산량/레벨업/해금 레벨은 공통 베이스(GachaEntryData)에서 상속받습니다.
    // ISpecialToolEntry를 구현해서 FinalProductionCalculator/SpecialBonusCalculator(TaskTown.Gacha 어셈블리)가
    // Assembly-CSharp를 직접 참조하지 않고도 특화 보너스 필드를 읽을 수 있게 합니다.
    [CreateAssetMenu(menuName = "Inventory/Tool/Tool Data SO", fileName = "ToolDataSO")]
    public class ToolDataSO : GachaEntryData, ISpecialToolEntry
    {
        [Header("도구 설명")]
        [TextArea(3, 5)]
        [SerializeField] private string toolDescription;

        [Header("특화 동물")]
        [SerializeField] private string specialAnimalId;
        [SerializeField, Range(0f, 5f)] private float specialAnimalBonusRate;

        [Header("배치용 프리팹")]
        [SerializeField] private GameObject toolPrefab;

        public string ToolDescription => toolDescription;
        public string SpecialAnimalId => specialAnimalId;
        public float SpecialAnimalBonusRate => specialAnimalBonusRate;
        public GameObject ToolPrefab => toolPrefab;
    }
}
