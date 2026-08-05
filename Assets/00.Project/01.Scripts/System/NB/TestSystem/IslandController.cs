using System.Collections;
using UnityEngine;

/// <summary>
/// VillageOrigin 내부의 오염을 방지하고, 섬 인스턴스만 안전하게 교체하는 컨트롤러
/// </summary>
public class IslandController : MonoBehaviour
{
    [Header("섬 생성 기준 위치 (예: IslandSlot 또는 씬 내부 트랜스폼)")]
    [SerializeField] private Transform islandSpawnParent;

    [Header("현재 활성화된 섬 인스턴스 (런타임 전용)")]
    [SerializeField] private GameObject currentIslandInstance;

    public GameObject CurrentIslandInstance => currentIslandInstance;

    private void Awake()
    {
        // 만약 부모 트랜스폼이 따로 지정되지 않았다면 자기자신(또는 상위 컨테이너)을 기준으로 삼음
        if (islandSpawnParent == null)
        {
            islandSpawnParent = transform;
        }
    }

    /// <summary>
    /// 게임 시작 시 최초 1레벨 섬을 지정된 위치에 생성합니다.
    /// </summary>
    public void SpawnInitialIsland(GameObject islandPrefab)
    {
        if (islandPrefab == null) return;

        // 이미 섬이 존재한다면 중복 생성 방지
        if (currentIslandInstance != null) return;

        currentIslandInstance = Instantiate(islandPrefab, islandSpawnParent.position, islandSpawnParent.rotation, islandSpawnParent);
        Debug.Log($"[IslandController] 초기 섬 생성 완료: {islandPrefab.name}");
    }

    /// <summary>
    /// 5레벨, 10레벨 마일스톤 도달 시 기존 섬만 깔끔하게 파괴하고 새 섬으로 교체합니다.
    /// (VillageOrigin 및 다른 자식 시스템들은 절대 건드리지 않습니다!)
    /// </summary>
    public void ChangeIslandPrefab(GameObject newIslandPrefab)
    {
        if (newIslandPrefab == null) return;

        StartCoroutine(ChangeIslandSequenceCoroutine(newIslandPrefab));
    }

    private IEnumerator ChangeIslandSequenceCoroutine(GameObject newIslandPrefab)
    {
        // 1. 기존에 있던 구형 섬 인스턴스만 안전하게 파괴
        if (currentIslandInstance != null)
        {
            Debug.Log($"[IslandController] 기존 섬 파괴 중: {currentIslandInstance.name}");
            Destroy(currentIslandInstance);
            currentIslandInstance = null;
        }

        // 2. 파괴 처리가 유니티 엔진에 반영되도록 1프레임 대기 (메모리 및 렌더링 동기화)
        yield return null;

        // 3. 새로운 마일스톤 섬(예: 2rd_island)을 동일한 위치에 생성
        currentIslandInstance = Instantiate(newIslandPrefab, islandSpawnParent.position, islandSpawnParent.rotation, islandSpawnParent);
        Debug.Log($"[IslandController] ★ 마일스톤 섬 교체 완료: {newIslandPrefab.name}");
    }
}