using System.Collections;
using UnityEngine;

/// <summary>
/// 섬의 생성, 파괴 및 5레벨/10레벨 마일스톤 교체를 담당하는 안전 제어 컨트롤러
/// </summary>
public class IslandController : MonoBehaviour
{
    [Header("섬 생성 기준 앵커 (비워두면 이 스크립트가 붙은 오브젝트 위치 사용)")]
    [SerializeField] private Transform islandSpawnParent;

    [Header("현재 활성화된 섬 인스턴스 (런타임 전용)")]
    [SerializeField] private GameObject currentIslandInstance;

    public GameObject CurrentIslandInstance => currentIslandInstance;

    private void Awake()
    {
        // 부모 앵커가 지정되지 않았다면 자기 자신의 트랜스폼을 기본값으로 설정
        if (islandSpawnParent == null)
        {
            islandSpawnParent = transform;
        }
    }

    /// <summary>
    /// 최초 게임 시작 시 1레벨 기본 섬을 생성합니다.
    /// </summary>
    public void SpawnInitialIsland(GameObject islandPrefab)
    {
        if (islandPrefab == null) return;
        if (currentIslandInstance != null) return;

        Transform safeParent = GetSafeSpawnParent();
        currentIslandInstance = Instantiate(islandPrefab, safeParent.position, safeParent.rotation, safeParent);
        Debug.Log($"[IslandController] 초기 섬 생성 완료: {islandPrefab.name}");
    }

    /// <summary>
    /// 5레벨, 10레벨 마일스톤 도달 시 기존 섬을 안전하게 파괴하고 새 섬으로 교체합니다.
    /// </summary>
    public void ChangeIslandPrefab(GameObject newIslandPrefab)
    {
        if (newIslandPrefab == null)
        {
            Debug.LogError("[IslandController] 교체할 새로운 섬 프리팹(Target Island Prefab)이 Null입니다! SO를 확인하세요.");
            return;
        }

        StartCoroutine(ChangeIslandSequenceCoroutine(newIslandPrefab));
    }

    private IEnumerator ChangeIslandSequenceCoroutine(GameObject newIslandPrefab)
    {
        // 1. 기존에 존재하던 섬 인스턴스만 안전하게 파괴
        if (currentIslandInstance != null)
        {
            Debug.Log($"[IslandController] 기존 섬 파괴 중: {currentIslandInstance.name}");
            Destroy(currentIslandInstance);
            currentIslandInstance = null;
        }

        // 2. 유니티 엔진이 파괴 명령을 처리할 수 있도록 1프레임 대기 ($t \ge 1\text{ frame}$)
        yield return null;

        // 3. [핵심 방어] 스폰 앵커가 도중에 파괴되었거나 유효하지 않은 경우 안전하게 복구
        Transform safeParent = GetSafeSpawnParent();

        // 4. 새로운 마일스톤 섬(예: 2nd_island)을 안전한 위치에 생성
        currentIslandInstance = Instantiate(newIslandPrefab, safeParent.position, safeParent.rotation, safeParent);

        if (currentIslandInstance != null)
        {
            Debug.Log($"[IslandController] ★ 마일스톤 섬 교체 성공: {newIslandPrefab.name}");
        }
        else
        {
            Debug.LogError($"[IslandController] 앗! '{newIslandPrefab.name}' 인스턴스화에 실패했습니다.");
        }
    }

    /// <summary>
    /// 스폰 부모 앵커가 파괴되었는지 검사하고, 문제가 있다면 현재 컨트롤러의 트랜스폼으로 대체 반환하는 방어 메서드
    /// </summary>
    private Transform GetSafeSpawnParent()
    {
        if (islandSpawnParent == null)
        {
            // C# 레벨에서 null이 된 경우
            islandSpawnParent = transform;
        }
        return islandSpawnParent;
    }
}