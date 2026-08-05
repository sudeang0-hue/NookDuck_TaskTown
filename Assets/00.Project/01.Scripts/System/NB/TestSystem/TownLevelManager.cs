using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 건물 생명주기 관리, 딕셔너리 기반 리마핑 및 섬 교체를 총괄하는 최종 매니저
/// </summary>
public class TownLevelManager : MonoBehaviour
{
    [Header("컨트롤러 및 데이터 참조")]
    [SerializeField] private IslandController islandController;
    [SerializeField] private SkyDropInstaller skyDropInstaller;
    [SerializeField] private List<LevelProgressionData> levelDataList;

    [Header("계층 구조 컨테이너 설정")]
    [SerializeField] private Transform buildingParentTransform;

    [Header("현재 진행 상태")]
    [SerializeField] private int currentLevel = 1;

    // [핵심] 생성된 건물들을 타입별로 기억하여 섬이 바뀌어도 위치를 재조정할 수 있도록 관리하는 딕셔너리
    private Dictionary<BuildingType, GameObject> spawnedBuildings = new Dictionary<BuildingType, GameObject>();

    private bool isTransitioning = false;

    public int CurrentLevel => currentLevel;
    public bool IsTransitioning => isTransitioning;

    private void Start()
    {
        InitializeTownGame(currentLevel);
    }

    public void InitializeTownGame(int startLevel)
    {
        currentLevel = startLevel;
        StartCoroutine(InitSequenceCoroutine(startLevel));
    }

    public void LevelUp()
    {
        if (isTransitioning)
        {
            Debug.LogWarning("[TownLevelManager] 현재 레벨업 연출이 진행 중입니다.");
            return;
        }

        currentLevel++;
        Debug.Log($"[TownLevelManager] ★ 레벨업 실행! 현재 레벨: {currentLevel}");
        StartCoroutine(ProcessLevelUpSequenceCoroutine(currentLevel));
    }

    private IEnumerator InitSequenceCoroutine(int targetLevel)
    {
        isTransitioning = true;

        LevelProgressionData levelData = GetLevelData(targetLevel);
        if (levelData != null)
        {
            if (islandController.CurrentIslandInstance == null && levelData.targetIslandPrefab != null)
            {
                islandController.SpawnInitialIsland(levelData.targetIslandPrefab);
                yield return null;
            }

            yield return StartCoroutine(SpawnAndDropBuildingsCoroutine(levelData, targetLevel, false));
        }

        isTransitioning = false;
    }

    private IEnumerator ProcessLevelUpSequenceCoroutine(int targetLevel)
    {
        isTransitioning = true;

        LevelProgressionData levelData = GetLevelData(targetLevel);
        if (levelData == null)
        {
            isTransitioning = false;
            yield break;
        }

        // 1. [섬 교체] 마일스톤 레벨(예: 5레벨)인 경우 새 섬으로 교체
        if (levelData.targetIslandPrefab != null && islandController != null)
        {
            Debug.Log($"[TownLevelManager] 마일스톤 도달! 섬 교체 시작 -> {levelData.targetIslandPrefab.name}");
            islandController.ChangeIslandPrefab(levelData.targetIslandPrefab);

            // 새 섬이 인스턴스화되고 트랜스폼이 안착할 때까지 충분히 대기
            yield return new WaitForSeconds(1.2f);

            // 2. [건물 리마핑] 섬이 바뀌었으므로, 기존에 지어둔 모든 건물의 위치를 새 섬의 소켓 위치로 재조정!
            RemapExistingBuildingsToNewIsland(targetLevel);
        }

        // 3. [신규 건물 스폰] 해당 레벨의 신규 해금 건물 낙하 연출
        yield return StartCoroutine(SpawnAndDropBuildingsCoroutine(levelData, targetLevel, true));

        isTransitioning = false;
    }

    /// <summary>
    /// 건물을 생성하고 (필요시) SkyDrop 연출을 태우는 메서드
    /// </summary>
    private IEnumerator SpawnAndDropBuildingsCoroutine(LevelProgressionData levelData, int targetLevel, bool playDropAnim)
    {
        if (islandController == null || islandController.CurrentIslandInstance == null)
        {
            Debug.LogError("[TownLevelManager] 씬에 활성화된 섬 인스턴스가 없습니다!");
            yield break;
        }

        IslandSocketProvider socketProvider = islandController.CurrentIslandInstance.GetComponentInChildren<IslandSocketProvider>();
        if (socketProvider == null)
        {
            Debug.LogError($"[TownLevelManager] '{islandController.CurrentIslandInstance.name}' 섬에 IslandSocketProvider가 없습니다!");
            yield break;
        }

        if (levelData.unlockBuildings == null || levelData.unlockBuildings.Count == 0)
        {
            yield break;
        }

        List<GameObject> dropSpawnList = new List<GameObject>();
        List<Transform> targetSocketList = new List<Transform>();
        Transform parentTarget = buildingParentTransform != null ? buildingParentTransform : transform;

        foreach (var unlockInfo in levelData.unlockBuildings)
        {
            if (unlockInfo.buildingPrefab == null) continue;

            // 이미 지어진 건물이라면 중복 생성 방지
            if (spawnedBuildings.ContainsKey(unlockInfo.buildingType)) continue;

            Transform targetSocket = socketProvider.GetBuildingSocket(unlockInfo.buildingType, targetLevel);
            if (targetSocket == null) continue;

            GameObject bldgObj = Instantiate(
                unlockInfo.buildingPrefab,
                targetSocket.position,
                targetSocket.rotation,
                parentTarget
            );

            // 딕셔너리에 등록하여 영구 관리
            spawnedBuildings[unlockInfo.buildingType] = bldgObj;

            if (playDropAnim)
            {
                dropSpawnList.Add(bldgObj);
                targetSocketList.Add(targetSocket);
            }
        }

        if (playDropAnim && dropSpawnList.Count > 0 && skyDropInstaller != null)
        {
            yield return StartCoroutine(skyDropInstaller.PlayDropSequenceCoroutine(
                dropSpawnList,
                targetSocketList,
                () => Debug.Log($"[TownLevelManager] Lv.{targetLevel} 신규 건물 배치 완료!")
            ));
        }
    }

    /// <summary>
    /// 섬이 교체되었을 때, 기존 건물들을 새 섬의 대응하는 소켓 위치로 이동시키는 리마핑 함수
    /// </summary>
    private void RemapExistingBuildingsToNewIsland(int currentGlobalLevel)
    {
        IslandSocketProvider newSocketProvider = islandController.CurrentIslandInstance.GetComponentInChildren<IslandSocketProvider>();
        if (newSocketProvider == null)
        {
            Debug.LogError("[TownLevelManager] 새로 교체된 섬에 IslandSocketProvider가 없습니다!");
            return;
        }

        foreach (var kvp in spawnedBuildings)
        {
            BuildingType bType = kvp.Key;
            GameObject bObj = kvp.Value;

            if (bObj == null) continue;

            // 새 섬에서 해당 건물의 타입과 일치하는 소켓 위치를 탐색
            Transform newSocket = newSocketProvider.GetBuildingSocket(bType, currentGlobalLevel);
            if (newSocket != null)
            {
                bObj.transform.position = newSocket.position;
                bObj.transform.rotation = newSocket.rotation;
                Debug.Log($"[TownLevelManager] 건물 위치 재배치 완료: {bType}");
            }
        }
    }

    private LevelProgressionData GetLevelData(int targetLevel)
    {
        return levelDataList.Find(data => data.level == targetLevel);
    }
}