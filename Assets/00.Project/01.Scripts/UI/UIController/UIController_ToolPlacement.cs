using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TaskTown.KDH
{
    /// <summary>
    /// 도구 배치 / 동물 할당 UI 호출 예시.
    /// 비즈니스 로직은 ToolPlacementService에만 맡긴다.
    /// </summary>
    public class UIController_ToolPlacement : MonoBehaviour
    {
        [Header("서비스 (비워두면 Instance)")]
        [SerializeField] private ToolPlacementService placementService;

        [Header("패널")]
        [SerializeField] private GameObject placementPanel;

        [Header("배치 현황 표시")]
        [SerializeField] private TMP_Text placedCountText; // 예: "2 / 3"

        [Header("도구 배치")]
        [SerializeField] private Button placeToolButton;
        [SerializeField] private Button unplaceToolButton;

        [Header("동물 할당")]
        [SerializeField] private Button assignAnimalButton;
        [SerializeField] private Button unassignAnimalButton;

        // 현재 UI에서 고른 대상 (다른 UI에서 SetSelected로 넘겨도 됨)
        private string selectedToolId;
        private string selectedAnimalId;

        private void Awake()
        {
            if (placementService == null)
                placementService = ToolPlacementService.Instance;
            if (placeToolButton != null)
                placeToolButton.onClick.AddListener(OnClickPlaceTool);
            if (unplaceToolButton != null)
                unplaceToolButton.onClick.AddListener(OnClickUnplaceTool);
            if (assignAnimalButton != null)
                assignAnimalButton.onClick.AddListener(OnClickAssignAnimal);
            if (unassignAnimalButton != null)
                unassignAnimalButton.onClick.AddListener(OnClickUnassignAnimal);
            if (placementPanel != null)
                placementPanel.SetActive(false);
        }

        private void OnEnable()
        {
            if (placementService == null)
                placementService = ToolPlacementService.Instance;

            // 도구 슬롯이 바뀌면 배치 현황 텍스트만 갱신
            if (InventoryManager_Tool.Instance != null)
                InventoryManager_Tool.Instance.OnToolSlotChanged += OnToolSlotChanged;

            RefreshPlacedCountText();
        }

        private void OnDisable()
        {
            if (InventoryManager_Tool.Instance != null)
                InventoryManager_Tool.Instance.OnToolSlotChanged -= OnToolSlotChanged;
        }

        // ---------- 외부(다른 UI)에서 선택값 주입 ----------
        public void SetSelectedTool(string toolId)
        {
            selectedToolId = toolId;
        }

        public void SetSelectedAnimal(string animalId)
        {
            selectedAnimalId = animalId;
        }

        public void Open(string toolId = null, string animalId = null)
        {
            if (!string.IsNullOrEmpty(toolId))
                selectedToolId = toolId;
            if (!string.IsNullOrEmpty(animalId))
                selectedAnimalId = animalId;
            if (placementPanel != null)
                placementPanel.SetActive(true);
            RefreshPlacedCountText();
        }

        public void Close()
        {
            if (placementPanel != null)
                placementPanel.SetActive(false);
        }

        // ---------- 버튼 콜백 (여기가 실제 연결) ----------
        private void OnClickPlaceTool()
        {
            if (!TryGetService(out ToolPlacementService service))
                return;
            if (string.IsNullOrEmpty(selectedToolId))
            {
                Debug.LogWarning("[UIController_ToolPlacement] 선택된 도구가 없습니다.");
                return;
            }
            bool ok = service.PlaceTool(selectedToolId);
            Debug.Log(ok
                ? $"[배치 성공] {selectedToolId}"
                : $"[배치 실패] {selectedToolId} (한도 초과 또는 없음)");
            RefreshPlacedCountText();
        }

        private void OnClickUnplaceTool()
        {
            if (!TryGetService(out ToolPlacementService service))
                return;
            if (string.IsNullOrEmpty(selectedToolId))
                return;
            bool ok = service.UnplaceTool(selectedToolId);
            Debug.Log(ok ? $"[배치 해제] {selectedToolId}" : $"[해제 실패] {selectedToolId}");
            RefreshPlacedCountText();
        }

        private void OnClickAssignAnimal()
        {
            if (!TryGetService(out ToolPlacementService service))
                return;
            if (string.IsNullOrEmpty(selectedToolId) || string.IsNullOrEmpty(selectedAnimalId))
            {
                Debug.LogWarning("[UIController_ToolPlacement] 도구/동물이 선택되지 않았습니다.");
                return;
            }
            // 배치된 도구에만 할당 (Service 내부에서 CurrentSet 검사)
            bool ok = service.AssignAnimal(selectedToolId, selectedAnimalId);
            Debug.Log(ok
                ? $"[할당 성공] tool={selectedToolId}, animal={selectedAnimalId}"
                : $"[할당 실패] 도구 미배치 / 동물 없음 / 기타");
        }

        private void OnClickUnassignAnimal()
        {
            if (!TryGetService(out ToolPlacementService service))
                return;
            if (string.IsNullOrEmpty(selectedToolId))
                return;
            bool ok = service.UnassignAnimal(selectedToolId);
            Debug.Log(ok ? $"[할당 해제] {selectedToolId}" : $"[해제 실패] {selectedToolId}");
        }

        // ---------- 현황 표시 ----------
        private void OnToolSlotChanged(SlotData_Tool _)
        {
            RefreshPlacedCountText();
        }

        private void RefreshPlacedCountText()
        {
            if (placedCountText == null)
                return;
            if (!TryGetService(out ToolPlacementService service))
            {
                placedCountText.text = "- / -";
                return;
            }
            placedCountText.text = $"{service.PlacedToolCount} / {service.MaxPlacedToolCount}";
        }

        private bool TryGetService(out ToolPlacementService service)
        {
            if (placementService == null)
                placementService = ToolPlacementService.Instance;
            service = placementService;
            if (service == null)
            {
                Debug.LogWarning("[UIController_ToolPlacement] ToolPlacementService 가 없습니다.");
                return false;
            }
            return true;
        }
    }
}
