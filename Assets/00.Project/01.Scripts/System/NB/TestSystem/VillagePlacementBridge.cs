using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Animal.Data;
using TaskTown.KDH;
using UI;

namespace TaskTown.Village
{
    /// <summary>
    /// 팀원의 UI 스크립트 수정 없이 확정 버튼 이벤트를 후킹(Hooking)하여
    /// 새로 추가된 동물의 버스 소환 연출을 트리거하는 비침습적 브릿지 클래스입니다.
    /// </summary>
    public class VillagePlacementBridge : MonoBehaviour
    {
        [Header("UI 및 연출 컴포넌트 참조")]
        [SerializeField] private VillageAnimalSetUI_Manager uiManager;
        [SerializeField] private UIController_VillageAnimalSet uiController;
        [SerializeField] private VillagerPlacementDirector placementDirector;

        [Header("데이터 참조 (미연결 시 Instance 사용)")]
        [SerializeField] private InventoryManager_Animal animalInventory;

        // 이전 확정 배치 상태를 저장해둘 캐시 버퍼
        private readonly List<string> _previousPlacedIds = new List<string>();
        private Button _targetedConfirmButton;

        private void Start()
        {
            ResolveInventory();
            InitPreviousCache();
            HookConfirmButton();
        }

        private void OnDestroy()
        {
            UnhookConfirmButton();
        }

        /// <summary>
        /// 팀원 코드 수정 없이 UIController의 Confirm 버튼 onClick 이벤트에 내 로직을 추가합니다.
        /// </summary>
        private void HookConfirmButton()
        {
            if (uiController != null)
            {
                _targetedConfirmButton = uiController.ConfirmSetAnimalButton; // public 프로퍼티 활용
            }
            else if (uiManager != null)
            {
                // UIController가 직접 연결되지 않은 경우 Find
                uiController = FindFirstObjectByType<UIController_VillageAnimalSet>();
                if (uiController != null)
                    _targetedConfirmButton = uiController.ConfirmSetAnimalButton;
            }

            if (_targetedConfirmButton != null)
            {
                // 기존 팀원 리스너는 유지하면서, 내 리스너를 다중 등록(Hooking)
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

        /// <summary>
        /// Confirm 버튼 클릭 시 팀원의 OnClickConfirmAnimalSet() 실행 직후 실행되는 콜백입니다.
        /// </summary>
        private void OnConfirmButtonClicked()
        {
            if (uiManager == null || placementDirector == null) return;

            // 이미 버스가 연출 중이라면 중복 실행 방지
            if (placementDirector.IsBusSummoning)
            {
                Debug.LogWarning("[VillagePlacementBridge] 이미 버스 연출이 진행 중입니다.", this);
                return;
            }

            // 1. 확정된 현재 배치 ID 목록 가져오기
            IReadOnlyList<string> currentPlacedIds = uiManager.PlacedAnimalIds;

            // 2. 이전 배치와 비교하여 새로 추가된 animalId만 추출 ($O(N)$)
            List<string> newlyAddedIds = ExtractNewlyAddedIds(_previousPlacedIds, currentPlacedIds);

            // 3. 차분 상태 업데이트 (다음 확정을 위해 캐시 갱신)
            UpdatePreviousCache(currentPlacedIds);

            if (newlyAddedIds.Count == 0)
            {
                Debug.Log("[VillagePlacementBridge] 새로 추가된 동물이 없으므로 버스 연출을 스킵합니다.", this);
                return;
            }

            // 4. animalId -> AnimalDataSO (GachaEntryData 상속체) 변환
            List<AnimalDataSO> newlyAddedDataList = ConvertIdsToDataList(newlyAddedIds);

            if (newlyAddedDataList.Count > 0)
            {
                Debug.Log($"[VillagePlacementBridge] 신규 주민 {newlyAddedDataList.Count}마리 소환 버스 연출을 시작합니다!", this);

                // 5. 주인님의 버스 연출 실행
                placementDirector.StartBatchBusSummon(newlyAddedDataList);
            }
        }

        /// <summary>
        /// 시간 복잡도 $O(N)$으로 신규 추가된 animalId를 추출합니다.
        /// </summary>
        private List<string> ExtractNewlyAddedIds(List<string> previous, IReadOnlyList<string> current)
        {
            List<string> newlyAdded = new List<string>();
            List<string> prevCopy = new List<string>(previous);

            if (current != null)
            {
                for (int i = 0; i < current.Count; i++)
                {
                    string id = current[i];
                    if (string.IsNullOrEmpty(id)) continue;

                    if (prevCopy.Contains(id))
                    {
                        prevCopy.Remove(id); // 이미 있던 동물이면 제외
                    }
                    else
                    {
                        newlyAdded.Add(id); // 없던 동물이면 신규 추가
                    }
                }
            }

            return newlyAdded;
        }

        private List<AnimalDataSO> ConvertIdsToDataList(List<string> ids)
        {
            List<AnimalDataSO> list = new List<AnimalDataSO>();
            ResolveInventory();

            if (animalInventory == null) return list;

            foreach (string id in ids)
            {
                // InventoryManager_Animal을 통해 GachaEntryData 기반의 AnimalDataSO 조회
                AnimalDataSO data = animalInventory.GetAnimalData(id);
                if (data != null)
                {
                    list.Add(data);
                }
            }

            return list;
        }

        private void InitPreviousCache()
        {
            if (uiManager != null)
            {
                UpdatePreviousCache(uiManager.PlacedAnimalIds);
            }
        }

        private void UpdatePreviousCache(IReadOnlyList<string> source)
        {
            _previousPlacedIds.Clear();
            if (source != null)
            {
                for (int i = 0; i < source.Count; i++)
                {
                    if (!string.IsNullOrEmpty(source[i]))
                        _previousPlacedIds.Add(source[i]);
                }
            }
        }

        private void ResolveInventory()
        {
            if (animalInventory == null)
                animalInventory = InventoryManager_Animal.Instance;
        }
    }
}