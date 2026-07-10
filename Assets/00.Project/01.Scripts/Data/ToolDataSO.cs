using TaskTown.Gacha;
using UnityEngine;


namespace Tool.Data
{
    [CreateAssetMenu(menuName = "Inventory/Tool/Tool Data SO", fileName = "ToolDataSO")]
    public class ToolDataSO : GachaEntryData
    {
        [Header("도구 해금 레벨")]
        [SerializeField] private int unlockLevel;  // 도구 해금 레벨

        [Header("도구 설명글")]
        [TextArea(3, 5)]
        [SerializeField] private string toolDescription;  // 도구 설명글

        [Header("생산량")]
        [SerializeField] private float baseCoinPerSecond;

        [Header("특화 동물")]
        [SerializeField] private string specialAnimalId;
        [SerializeField, Range(0f, 5f)] private float specialAnimalBonusRate;

        [Header("데스크 타운 표시용")]
        [SerializeField] private GameObject toolPrefab;

        public int UnlockLevel => unlockLevel;
        public string ToolDescription => toolDescription;
        public float BaseCoinPerSecond => baseCoinPerSecond;
        public string SpecialAnimalId => specialAnimalId;
        public float SpecialAnimalBonusRate => specialAnimalBonusRate;
        public GameObject ToolPrefab => toolPrefab;
    }
}