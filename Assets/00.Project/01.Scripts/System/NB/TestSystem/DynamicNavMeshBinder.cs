using UnityEngine;
using Unity.AI.Navigation; // NavMeshSurface 클래스를 위해 필수!
using UnityEngine.AI;       // NavMeshCollectGeometry 및 NavMesh 관련 Enum을 위해 필수!

[RequireComponent(typeof(NavMeshSurface))]
public class DynamicNavMeshBinder : MonoBehaviour
{
    private NavMeshSurface navMeshSurface;

    private void Awake()
    {
        navMeshSurface = GetComponent<NavMeshSurface>();
        ConfigureSurfaceSettings();
    }

    /// <summary>
    /// Tripo3D 동적 에셋을 인식하도록 NavMeshSurface 설정을 런타임에 안전하게 초기화합니다.
    /// </summary>
    private void ConfigureSurfaceSettings()
    {
        if (navMeshSurface == null) return;

        // 1. Static 여부와 상관없이 '자식(Children)' 오브젝트들만 탐색하여 수집
        navMeshSurface.collectObjects = CollectObjects.Children;

        // 2. [에러 해결 지점] UnityEngine.AI 네임스페이스의 NavMeshCollectGeometry 참조
        // Colliders 대신 3D 메쉬 형상(RenderMeshes)을 직접 수집하도록 설정
        navMeshSurface.useGeometry = NavMeshCollectGeometry.RenderMeshes;

        // 3. 수집 레이어 마스크를 Everything(-1)으로 설정하여 누락 방지
        navMeshSurface.layerMask = ~0;
    }

    /// <summary>
    /// 동적으로 생성된 섬을 자식으로 편입시키고 NavMesh를 리베이크합니다.
    /// </summary>
    /// <param name="spawnedIsland">Instantiate로 생성된 섬 오브젝트</param>
    public void BindAndBake(GameObject spawnedIsland)
    {
        if (spawnedIsland == null) return;

        // 생성된 섬을 NavMeshSurface가 붙어있는 오브젝트의 자식으로 등록
        spawnedIsland.transform.SetParent(this.transform, worldPositionStays: true);

        // 런타임 동기 네브메시 생성
        navMeshSurface.BuildNavMesh();

        Debug.Log($"[ECHO_TD] '{spawnedIsland.name}' 지형이 성공적으로 NavMeshSurface에 장착 및 베이크되었습니다.");
    }
}