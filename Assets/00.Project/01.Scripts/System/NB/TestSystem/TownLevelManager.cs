using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 1~4레벨 기본 섬 유지, 5레벨 마일스톤 섬 교체 및 건물 스폰을 총괄하는 레벨 관리자
/// </summary>
public class TownLevelManager : MonoBehaviour
{
    [Header("컨트롤러 및 데이터 참조")]
    [SerializeField] private IslandController islandController;
    [SerializeField] private SkyDropInstaller skyDropInstaller;
    [SerializeField] private List<LevelProgressionData> levelDataList;

    [Header("현재 진행 상태")]
    [SerializeField] private int currentLevel = 1;

    // 연출 중복 실행을 막는 락(Lock) 플래그
    private bool isTransitioning = false;

    public int CurrentLevel => currentLevel;
    public bool IsTransitioning => isTransitioning;

    private void Start()
    {
        // 게임 시작 시 1레벨 기준으로 초기화
        InitializeTownGame(currentLevel);
    }

    /// <summary>
    /// 게임 최초 구동 시 1레벨 세팅 (기본 섬 + 1레벨 건물 배치)
    /// </summary>
    public void InitializeTownGame(int startLevel)
    {
        currentLevel = startLevel;
        StartCoroutine(InitSequenceCoroutine(startLevel));
    }

    /// <summary>
    /// 레벨업 실행 함수 (U키 또는 UI 버튼 연동)
    /// </summary>
    public void LevelUp()
    {
        if (isTransitioning)
        {
            Debug.LogWarning("[TownLevelManager] 현재 레벨업 연출이 진행 중입니다.");
            return;
        }

        currentLevel++;
        Debug.Log($"[TownLevelManager] ★ 레벨업! 현재 레벨: {currentLevel}");
        StartCoroutine(ProcessLevelUpSequenceCoroutine(currentLevel));
    }

    /// <summary>
    /// 초기 1레벨 마을 구축 코루틴
    /// </summary>
    private IEnumerator InitSequenceCoroutine(int targetLevel)
    {
        isTransitioning = true;

        LevelProgressionData levelData = GetLevelData(targetLevel);
        if (levelData != null)
        {
            // 만약 씬에 이미 섬이 배치되어 있지 않다면, 1레벨 SO에 있는 기본 섬 프리팹을 스폰합니다.
            if (islandController.CurrentIslandInstance == null && levelData.targetIslandPrefab != null)
            {
                islandController.SpawnInitialIsland(levelData.targetIslandPrefab);
                yield return null;
            }

            // 1레벨 해금 건물 드롭
            yield return StartCoroutine(SpawnAndDropBuildingsCoroutine(levelData, targetLevel));
        }

        isTransitioning = false;
    }

    /// <summary>
    /// 레벨업 발생 시 섬 교체(5레벨 등) 및 신규 건물 스폰을 처리하는 핵심 코루틴
    /// </summary>
    private IEnumerator ProcessLevelUpSequenceCoroutine(int targetLevel)
    {
        isTransitioning = true;

        LevelProgressionData levelData = GetLevelData(targetLevel);
        if (levelData == null)
        {
            isTransitioning = false;
            yield break;
        }

        // [핵심] 해당 레벨 SO에 targetIslandPrefab이 할당되어 있다면 (예: 5레벨 2rd_island) 섬을 교체합니다!
        // 2, 3, 4레벨 SO에는 이 칸이 비어있(null)으므로 섬 교체를 건너뛰고 기존 섬이 유지됩니다.
        if (levelData.targetIslandPrefab != null && islandController != null)
        {
            Debug.Log($"[TownLevelManager] 마일스톤 도달! 섬 교체 실행 -> {levelData.targetIslandPrefab.name}");
            islandController.ChangeIslandPrefab(levelData.targetIslandPrefab);

            // 섬 교체 연출 대기 시간
            yield return new WaitForSeconds(1.2f);
        }

        // 신규 건물 스폰 및 낙하 연출
        yield return StartCoroutine(SpawnAndDropBuildingsCoroutine(levelData, targetLevel));

        isTransitioning = false;
    }

    /// <summary>
    /// 소켓 좌표를 찾아 건물을 생성하고 SkyDrop 연출을 태우는 서브 루틴
    /// </summary>
    private IEnumerator SpawnAndDropBuildingsCoroutine(LevelProgressionData levelData, int targetLevel)
    {
        if (islandController == null || islandController.CurrentIslandInstance == null)
        {
            Debug.LogError("[TownLevelManager] 씬에 활성화된 섬 인스턴스가 없습니다!");
            yield break;
        }

        IslandSocketProvider socketProvider = islandController.CurrentIslandInstance.GetComponentInChildren<IslandSocketProvider>();
        if (socketProvider == null)
        {
            Debug.LogError($"[TownLevelManager] '{islandController.CurrentIslandInstance.name}' 섬에 IslandSocketProvider 컴포넌트가 없습니다!", islandController.CurrentIslandInstance);
            yield break;
        }

        if (levelData.unlockBuildings == null || levelData.unlockBuildings.Count == 0)
        {
            yield break;
        }

        List<GameObject> dropSpawnList = new List<GameObject>();
        List<Transform> targetSocketList = new List<Transform>();

        foreach (var unlockInfo in levelData.unlockBuildings)
        {
            if (unlockInfo.buildingPrefab == null) continue;

            Transform targetSocket = socketProvider.GetBuildingSocket(unlockInfo.buildingType, targetLevel);
            if (targetSocket == null) continue;

            GameObject bldgObj = Instantiate(
                unlockInfo.buildingPrefab,
                targetSocket.position,
                targetSocket.rotation,
                transform
            );

            dropSpawnList.Add(bldgObj);
            targetSocketList.Add(targetSocket);
        }

        if (dropSpawnList.Count > 0 && skyDropInstaller != null)
        {
            yield return StartCoroutine(skyDropInstaller.PlayDropSequenceCoroutine(
                dropSpawnList,
                targetSocketList,
                () => Debug.Log($"[TownLevelManager] Lv.{targetLevel} 건물 배치 완료!")
            ));
        }
    }

    private LevelProgressionData GetLevelData(int targetLevel)
    {
        LevelProgressionData levelData = levelDataList.Find(data => data.level == targetLevel);
        if (levelData == null)
        {
            Debug.LogError($"[TownLevelManager] Lv.{targetLevel}에 해당하는 LevelProgressionData SO를 찾을 수 없습니다.");
        }
        return levelData;
    }
}