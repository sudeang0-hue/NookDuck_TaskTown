//NB

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Animal.Data;
using TaskTown.KDH;
using UI;

// UI Confirm 버튼을 후킹하여 Director의 버스 연출을 깔끔하게 실행해 주는 브릿지 클래스
public class VillagePlacementBridge : MonoBehaviour
{
    [Header("UI 및 연출 컴포넌트 참조")]
    [SerializeField] private VillageAnimalSetUI_Manager uiManager;
    [SerializeField] private UIController_VillageAnimalSet uiController;
    [SerializeField] private VillagerPlacementDirector placementDirector;

    [Header("데이터 참조 (미연결 시 Instance 사용)")]
    [SerializeField] private InventoryManager_Animal animalInventory;

    private Button _targetedConfirmButton;

    private void Start()
    {
        ResolveInventory();
        HookConfirmButton();
    }

    private void OnDestroy()
    {
        UnhookConfirmButton();
    }

    private void HookConfirmButton()
    {
        if (uiController != null)
        {
            _targetedConfirmButton = uiController.ConfirmSetAnimalButton;
        }
        else if (uiManager != null)
        {
            uiController = FindFirstObjectByType<UIController_VillageAnimalSet>();
            if (uiController != null)
                _targetedConfirmButton = uiController.ConfirmSetAnimalButton;
        }

        if (_targetedConfirmButton != null)
        {
            _targetedConfirmButton.onClick.RemoveListener(OnConfirmButtonClicked);
            _targetedConfirmButton.onClick.AddListener(OnConfirmButtonClicked);
            Debug.Log("[VillagePlacementBridge] 팀원 UI Confirm 버튼 후킹 성공!", this);
        }
        else
        {
            Debug.LogError("[VillagePlacementBridge] Confirm 버튼을 찾지 못했습니다. Inspector 연결을 확인하세요.", this);
        }
    }

    private void UnhookConfirmButton()
    {
        if (_targetedConfirmButton != null)
        {
            _targetedConfirmButton.onClick.RemoveListener(OnConfirmButtonClicked);
        }
    }

    private void OnConfirmButtonClicked()
    {
        if (placementDirector == null) return;

        if (placementDirector.IsBusSummoning)
        {
            Debug.LogWarning("[VillagePlacementBridge] 이미 버스 연출이 진행 중입니다.", this);
            return;
        }

        // Bridge에서 불완전하게 차분을 구하지 않고, 3D 월드 상태를 종합 비교하는 Director의 Sync 메서드를 호출
        Debug.Log("[VillagePlacementBridge] 주민 교체/스폰 버스 연출을 요청합니다!", this);
        placementDirector.RequestVillagerSync();
    }

    private void ResolveInventory()
    {
        if (animalInventory == null)
            animalInventory = InventoryManager_Animal.Instance;
    }
}