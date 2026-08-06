//NB

using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using Manager;

// 인스펙터에서 레벨별 섬 비주얼 및 NavMesh 데이터를 세팅하기 위한 데이터 구조체
[System.Serializable]
public struct IslandStageData
{
    public int minLevel;                    // 이 단계가 시작되는 최소 레벨 (예: 1, 5, 10)
    public Mesh islandMesh;                 // 해당 단계의 섬 3D 메시
    public PhysicsMaterial physicsMaterial;   // 3D 물리 재질
    public NavMeshData navMeshData;         // 에디터에서 미리 구워둔(Pre-baked) NavMeshData 에셋
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
        if (VillageSystemManager.Instance != null)
        {
            // 이벤트 구독 및 초기 레벨 데이터 적용
            VillageSystemManager.Instance.OnVillageStateChanged += HandleVillageStateChanged;
            ApplyIslandStage(VillageSystemManager.Instance.TownLevel);
        }
        else
        {
            Debug.LogError("<color=red>[IslandManager] 씬에 VillageSystemManager가 존재하지 않습니다!</color>");
        }
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
        if (VillageSystemManager.Instance != null)
        {
            ApplyIslandStage(VillageSystemManager.Instance.TownLevel);
        }
    }

    private void ApplyIslandStage(int currentLevel)
    {
        if (islandStages == null || islandStages.Length == 0) return; 

        // 현재 레벨 조건에 맞는 가장 높은 단계의 데이터 탐색
        IslandStageData targetStage = islandStages[0]; 

        for (int i = islandStages.Length - 1; i >= 0; i--) 
        {
            if (currentLevel >= islandStages[i].minLevel) 
            {
                targetStage = islandStages[i]; 
                break;
            }
        }

        // 1. 메시 교체
        if (meshFilter != null && targetStage.islandMesh != null) 
        {
            meshFilter.sharedMesh = targetStage.islandMesh; 
        }

        // 2. 콜라이더 및 물리 재질 갱신
        if (meshCollider != null) 
        {
            meshCollider.sharedMesh = null; 
            meshCollider.sharedMesh = targetStage.islandMesh; 
            meshCollider.sharedMaterial = targetStage.physicsMaterial; 
        }

        // 3. 네브메시 데이터 즉시 스위칭
        SwitchNavMeshData(targetStage.navMeshData); 

        Debug.Log($"<color=cyan>[IslandManager] 섬 메쉬 스테이지 갱신 완료! (적용 minLevel: {targetStage.minLevel})</color>"); 
    }

    private void SwitchNavMeshData(NavMeshData newNavMeshData) 
    {
        if (currentNavMeshDataInstance.valid) 
        {
            NavMesh.RemoveNavMeshData(currentNavMeshDataInstance); 
        }

        if (newNavMeshData != null) 
        {
            currentNavMeshDataInstance = NavMesh.AddNavMeshData(newNavMeshData); 
            Debug.Log("<color=magenta>[IslandManager] NavMeshData 교체 완료!</color>"); 
        }
    }

    public void RebakeOnMoveComplete() 
    {
        if (currentNavMeshDataInstance.valid && navMeshSurface != null) 
        {
            navMeshSurface.BuildNavMesh(); 
        }
    }
}