using UnityEngine;

namespace TaskTown.Gacha
{
    [CreateAssetMenu(menuName = "TaskTown/Gacha/Tool Data", fileName = "New ToolData")]
    public class ToolData : GachaEntryData
    {
        [Header("생산량")]
        [SerializeField] private float baseCoinPerSecond;

        [Header("특화 동물")]
        [SerializeField] private string specialAnimalId;
        [SerializeField, Range(0f, 5f)] private float specialAnimalBonusRate;

        [Header("데스크 타운 표시용")]
        [SerializeField] private GameObject toolPrefab;

        public float BaseCoinPerSecond => baseCoinPerSecond;
        public string SpecialAnimalId => specialAnimalId;
        public float SpecialAnimalBonusRate => specialAnimalBonusRate;
        public GameObject ToolPrefab => toolPrefab;
    }
}
