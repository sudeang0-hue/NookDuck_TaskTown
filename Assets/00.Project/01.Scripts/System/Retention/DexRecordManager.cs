using UnityEngine;

namespace TaskTown.KDH
{
    // 도감(수집 기록)을 인벤토리와 별개로 영구 저장합니다. 난이도 리셋으로 보유 동물/도구
    // 인벤토리가 초기화돼도 "이미 본 적 있음" 기록은 그대로 유지됩니다(사용자 확인 - 게임
    // 수치/생산량에는 영향 없는 순수 기록용 요소).
    public class DexRecordManager : MonoBehaviour
    {
        public static DexRecordManager Instance { get; private set; }

        private const string DiscoveredKeyPrefix = "Dex_Discovered_";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Awake() 실행 순서는 GameObject마다 달라질 수 있어서, 모든 Awake가 끝난 뒤 호출되는
            // Start()에서 구독합니다(InventoryManager_Animal/Tool.Instance가 확실히 준비된 시점).
            if (InventoryManager_Animal.Instance != null)
                InventoryManager_Animal.Instance.OnAnimalSlotChanged += HandleAnimalSlotChanged;

            if (InventoryManager_Tool.Instance != null)
                InventoryManager_Tool.Instance.OnToolSlotChanged += HandleToolSlotChanged;
        }

        private void OnDisable()
        {
            if (InventoryManager_Animal.Instance != null)
                InventoryManager_Animal.Instance.OnAnimalSlotChanged -= HandleAnimalSlotChanged;

            if (InventoryManager_Tool.Instance != null)
                InventoryManager_Tool.Instance.OnToolSlotChanged -= HandleToolSlotChanged;
        }

        private void HandleAnimalSlotChanged(SlotData_Animal slotData)
        {
            if (slotData != null)
                MarkDiscovered(slotData.AnimalId);
        }

        private void HandleToolSlotChanged(SlotData_Tool slotData)
        {
            if (slotData != null)
                MarkDiscovered(slotData.ToolId);
        }

        public void MarkDiscovered(string id)
        {
            if (string.IsNullOrEmpty(id))
                return;

            PlayerPrefs.SetInt(DiscoveredKeyPrefix + id, 1);
        }

        public bool IsDiscovered(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;

            return PlayerPrefs.GetInt(DiscoveredKeyPrefix + id, 0) == 1;
        }
    }
}
