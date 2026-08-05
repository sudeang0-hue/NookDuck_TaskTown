using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

[System.Serializable]
public struct IslandStageData
{
    public int minLevel;                  // 이 단계가 시작되는 최소 레벨 (예: 1, 5, 10)
    public Mesh islandMesh;               // 해당 단계의 섬 3D 메시
    public PhysicsMaterial physicsMaterial; // [수정 완료] 3D 물리 재질 (PhysicMaterial -> PhysicsMaterial)
    public NavMeshData navMeshData;       // 에디터에서 미리 구워둔(Pre-baked) NavMeshData 에셋
}

public class IslandManager : MonoBehaviour
{
    [Header("단계별 섬 설정 데이터 (인스펙터에서 등록)")]
    [SerializeField] private IslandStageData[] islandStages;

    [Header("컴포넌트 참조")]
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private MeshCollider meshCollider;
    [SerializeField] private NavMeshSurface navMeshSurface;

    private NavMeshDataInstance currentNavMeshDataInstance;

    private void Start()
    {
        if (TownManager.Instance != null)
        {
            TownManager.Instance.OnTownLevelChanged += HandleLevelChanged;
            // 게임 시작 시 초기 레벨(1레벨) 데이터 적용
            ApplyIslandStage(TownManager.Instance.CurrentLevel);
        }
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
        ApplyIslandStage(level);
    }

    private void ApplyIslandStage(int currentLevel)
    {
        // 1. 현재 레벨에 맞는 가장 높은 단계의 데이터를 역순으로 탐색하여 찾습니다.
        IslandStageData targetStage = islandStages[0];

        for (int i = islandStages.Length - 1; i >= 0; i--)
        {
            if (currentLevel >= islandStages[i].minLevel)
            {
                targetStage = islandStages[i];
                break;
            }
        }

        // 2. 메시(Mesh) 교체
        if (meshFilter != null && targetStage.islandMesh != null)
        {
            meshFilter.sharedMesh = targetStage.islandMesh;
        }

        // 3. 콜라이더(Collider) 갱신 (메시 변경에 따른 물리 형태 동기화)
        if (meshCollider != null)
        {
            meshCollider.sharedMesh = null; // 초기화 후 재할당해야 정확히 갱신됩니다 ($Mesh\ Refresh$)
            meshCollider.sharedMesh = targetStage.islandMesh;
            meshCollider.sharedMaterial = targetStage.physicsMaterial;
        }

        // 4. 네브메시 데이터(NavMeshData) 즉시 스위칭 (렉 0초!)
        SwitchNavMeshData(targetStage.navMeshData);

        Debug.Log($"<color=cyan>[IslandManager] 섬 스테이지 갱신 완료! (적용된 minLevel: {targetStage.minLevel})</color>");
    }

    private void SwitchNavMeshData(NavMeshData newNavMeshData)
    {
        // 기존에 등록된 네브메시 데이터가 있다면 안전하게 제거
        if (currentNavMeshDataInstance.valid)
        {
            NavMesh.RemoveNavMeshData(currentNavMeshDataInstance);
        }

        // 새로운 NavMeshData가 존재한다면 즉시 등록
        if (newNavMeshData != null)
        {
            currentNavMeshDataInstance = NavMesh.AddNavMeshData(newNavMeshData);
            Debug.Log("<color=magenta>[IslandManager]  NavMeshData 에셋 즉시 교체 완료!</color>");
        }
    }

    /// <summary>
    /// 섬 드래그 이동이 끝났을 때 외부(ObjectDragger)에서 호출할 수 있는 함수
    /// </summary>
    public void RebakeOnMoveComplete()
    {
        if (currentNavMeshDataInstance.valid && navMeshSurface != null)
        {
            navMeshSurface.BuildNavMesh(); // 단일 오브젝트 구조에서의 동적 베이킹 갱신
        }
    }
}