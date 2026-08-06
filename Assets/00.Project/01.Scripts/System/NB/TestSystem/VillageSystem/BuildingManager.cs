//NB

using System;
using System.Collections.Generic;
using UnityEngine;
using Manager; 

public class BuildingManager : MonoBehaviour
{
    [Serializable]
    public struct BuildingData
    {
        public string buildingName;       // 건물 이름
        public int requiredLevel;         // 해금 필요 레벨
        public GameObject buildingObject; // 실제 월드에 배치된 건물 오브젝트
    }

    [Header("건물 해금 리스트")]
    [SerializeField] private List<BuildingData> buildingList;

    private bool isGameStarted = false;

    private void Start()
    {
        if (VillageSystemManager.Instance != null)
        {
            // 1. 이벤트 구독 (팀원분의 OnVillageStateChanged)
            VillageSystemManager.Instance.OnVillageStateChanged += HandleVillageStateChanged;

            // 2. Start 시점의 현재 레벨로 최초 동기화
            UpdateBuildings(VillageSystemManager.Instance.TownLevel, isInitialSetup: true);
        }
        else
        {
            Debug.LogError("<color=red>[BuildingManager] 씬에 VillageSystemManager가 존재하지 않습니다!</color>");
        }

        isGameStarted = true;
    }

    private void OnDestroy()
    {
        if (VillageSystemManager.Instance != null)
        {
            VillageSystemManager.Instance.OnVillageStateChanged -= HandleVillageStateChanged;
        }
    }

    private void HandleVillageStateChanged()
    {
        if (VillageSystemManager.Instance == null) return;

        // 레벨업/상태 변경 시 현재 TownLevel을 가져와 비주얼 갱신
        UpdateBuildings(VillageSystemManager.Instance.TownLevel, isInitialSetup: false);
    }

    private void UpdateBuildings(int currentLevel, bool isInitialSetup)
    {
        for (int i = 0; i < buildingList.Count; i++)
        {
            var building = buildingList[i];

            if (building.buildingObject == null) continue;

            bool shouldBeActive = currentLevel >= building.requiredLevel;
            bool isCurrentlyActive = building.buildingObject.activeSelf;

            if (isCurrentlyActive != shouldBeActive)
            {
                building.buildingObject.SetActive(shouldBeActive);

                // 새롭게 해금되는 순간에만 쿵! 떨어지는 낙하 연출 실행
                if (shouldBeActive && !isInitialSetup && isGameStarted)
                {
                    BuildingDropEffect dropEffect = building.buildingObject.GetComponent<BuildingDropEffect>();
                    if (dropEffect != null)
                    {
                        dropEffect.PlayDropAnimation();
                    }
                }

                Debug.Log($"<color=cyan>[BuildingManager] [{building.buildingName}] 활성화 상태: {shouldBeActive}</color>");
            }
        }
    }
}