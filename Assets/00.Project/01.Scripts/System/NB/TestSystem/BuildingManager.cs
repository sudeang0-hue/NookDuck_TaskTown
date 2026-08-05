using System;
using System.Collections.Generic;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    [Serializable]
    public struct BuildingData
    {
        public string buildingName;       // 건물 이름 (인스펙터 가독성용)
        public int requiredLevel;         // 해금 필요 레벨
        public GameObject buildingObject; // 실제 월드에 배치된 건물 오브젝트
    }

    [Header("건물 해금 리스트")]
    [SerializeField] private List<BuildingData> buildingList;

    // 게임이 완전히 시작되었는지 체크하는 플래그 (시작 시 불필요한 낙하 연출 방지)
    private bool isGameStarted = false;

    private void Start()
    {
        if (TownManager.Instance != null)
        {
            // 1. 이벤트 구독
            TownManager.Instance.OnTownLevelChanged += HandleLevelChanged;

            // 2. [핵심 해결] Start 타이밍 문제로 이벤트를 놓쳤을 경우를 대비해, 
            // 게임 시작 시점의 레벨을 기준으로 최초 건물의 켜짐/꺼짐 상태를 강제 동기화합니다.
            UpdateBuildings(TownManager.Instance.CurrentLevel, isInitialSetup: true);
        }
        else
        {
            Debug.LogError("<color=red>[BuildingManager] 씬에 TownManager가 존재하지 않습니다!</color>");
        }

        isGameStarted = true;
    }

    private void OnDestroy()
    {
        if (TownManager.Instance != null)
        {
            TownManager.Instance.OnTownLevelChanged -= HandleLevelChanged;
        }
    }

    private void HandleLevelChanged(int level)
    {
        // 레벨업 이벤트로 호출될 때는 초기 세팅이 아님 (isInitialSetup = false)
        UpdateBuildings(level, isInitialSetup: false);
    }

    private void UpdateBuildings(int currentLevel, bool isInitialSetup)
    {
        for (int i = 0; i < buildingList.Count; i++)
        {
            var building = buildingList[i];

            // 엣지 케이스 방어: 인스펙터에 건물이 비어있는지 확인
            if (building.buildingObject == null) continue;

            // 현재 레벨이 요구 레벨 이상인지 판단
            bool shouldBeActive = currentLevel >= building.requiredLevel;
            bool isCurrentlyActive = building.buildingObject.activeSelf;

            // 상태가 바뀔 때만 조작
            if (isCurrentlyActive != shouldBeActive)
            {
                building.buildingObject.SetActive(shouldBeActive);

                // 초기 시작이 아니고, 레벨업을 통해 '새롭게 해금되어 켜지는 순간'에만 낙하 연출 실행!
                if (shouldBeActive && !isInitialSetup && isGameStarted)
                {
                    BuildingDropEffect dropEffect = building.buildingObject.GetComponent<BuildingDropEffect>();
                    if (dropEffect != null)
                    {
                        dropEffect.PlayDropAnimation();
                    }
                }

                Debug.Log($"<color=cyan>[BuildingManager] [{building.buildingName}] 상태 갱신 -> 활성화: {shouldBeActive}</color>");
            }
        }
    }
}