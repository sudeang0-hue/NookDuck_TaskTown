using Animal.Data;
using TaskTown.Gacha;
using Tool.Data;
using UnityEngine;

namespace TaskTown.KDH
{
    // AnimalGachaManager/ToolGachaManager의 뽑기 결과를 실제 인벤토리(InventoryManager_Animal/Tool)에
    // 반영하는 연결 스크립트입니다. TaskTown.Gacha(뽑기)와 TaskTown.KDH(인벤토리)는 서로 존재를
    // 모르는 별개 시스템이라, 그 사이를 잇는 역할만 합니다.
    public class GachaInventoryBridge : MonoBehaviour
    {
        [SerializeField] private AnimalGachaManager animalGachaManager;
        [SerializeField] private ToolGachaManager toolGachaManager;

        private void OnEnable()
        {
            if (animalGachaManager != null) animalGachaManager.OnGachaResolved += HandleAnimalGachaResolved;
            if (toolGachaManager != null) toolGachaManager.OnGachaResolved += HandleToolGachaResolved;
        }

        private void OnDisable()
        {
            if (animalGachaManager != null) animalGachaManager.OnGachaResolved -= HandleAnimalGachaResolved;
            if (toolGachaManager != null) toolGachaManager.OnGachaResolved -= HandleToolGachaResolved;
        }

        private void HandleAnimalGachaResolved(GachaResult result)
        {
            if (InventoryManager_Animal.Instance == null)
            {
                Debug.LogWarning("[GachaInventoryBridge] InventoryManager_Animal.Instance가 없습니다.");
                return;
            }

            if (result.Entry is not AnimalDataSO animalData)
            {
                Debug.LogWarning($"[GachaInventoryBridge] AnimalDataSO가 아닌 결과입니다: {result.Entry?.GetType().Name}");
                return;
            }

            InventoryManager_Animal.Instance.AddAnimalSlot(animalData);
        }

        private void HandleToolGachaResolved(GachaResult result)
        {
            if (InventoryManager_Tool.Instance == null)
            {
                Debug.LogWarning("[GachaInventoryBridge] InventoryManager_Tool.Instance가 없습니다.");
                return;
            }

            if (result.Entry is not ToolDataSO toolData)
            {
                Debug.LogWarning($"[GachaInventoryBridge] ToolDataSO가 아닌 결과입니다: {result.Entry?.GetType().Name}");
                return;
            }

            InventoryManager_Tool.Instance.AddToolSlot(toolData);
        }
    }
}
