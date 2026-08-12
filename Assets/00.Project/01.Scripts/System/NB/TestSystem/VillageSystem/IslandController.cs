//NB

using System;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using Manager; 

// 섬 형태 스위칭, NavMesh 런타임 베이킹, 드래그 이동 대응을 전담하는 통합 컨트롤러입니다.
// 하이어라키의 부모 오브젝트(Island)에 부착하여 사용합니다.
[RequireComponent(typeof(NavMeshSurface))]
public class IslandController : MonoBehaviour
{
    [Header("섬 오브젝트 (단계별 - 자식 루트)")]
    [SerializeField] private GameObject island1stRoot; // 1레벨 ~ 4레벨 (1st_Island_Root)
    [SerializeField] private GameObject island2ndRoot; // 5레벨 ~ 9레벨 (2nd_Island_Root)
    [SerializeField] private GameObject island3rdRoot; // 10레벨 이상 (3rd_Island_Root)

    private NavMeshSurface navMeshSurface;

    //NavMesh 재구성 완료 시 씬 내 AI(동물/주민)들에게 알릴 글로벌 이벤트
    public static event Action OnNavMeshRebaked;

    private void Awake()
    {
        navMeshSurface = GetComponent<NavMeshSurface>();
        ConfigureSurfaceSettings();
    }

    private void Start()
    {
        if (VillageSystemManager.Instance != null)
        {
            // 1. 이벤트 구독
            VillageSystemManager.Instance.OnVillageStateChanged += HandleVillageStateChanged;

            // 2. 최초 게임 시작 시 상태 동기화
            UpdateIslandAndRebake(VillageSystemManager.Instance.TownLevel);
        }
        else
        {
            Debug.LogError("<color=red>[IslandController] 씬에 VillageSystemManager가 존재하지 않습니다!</color>");
        }
    }

    private void OnDestroy()
    {
        if (VillageSystemManager.Instance != null)
        {
            VillageSystemManager.Instance.OnVillageStateChanged -= HandleVillageStateChanged;
        }
    }

    // NavMeshSurface 기본 수집 범위 설정 (RenderMesh 기반 동적 수집)
    private void ConfigureSurfaceSettings()
    {
        if (navMeshSurface == null) return;

        // 자식 오브젝트(Children)의 RenderMesh만 모아서 NavMesh 수집
        navMeshSurface.collectObjects = CollectObjects.Children;
        navMeshSurface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
        navMeshSurface.layerMask = ~0; // Everything Layer
    }

    private void HandleVillageStateChanged()
    {
        if (VillageSystemManager.Instance != null)
        {
            UpdateIslandAndRebake(VillageSystemManager.Instance.TownLevel);
        }
    }

    // 마을 레벨에 따른 섬 활성화 및 NavMesh 리베이킹
 
    private void UpdateIslandAndRebake(int level)
    {
        bool is1stActive = (level >= 1 && level < 5);
        bool is2ndActive = (level >= 5 && level < 10);
        bool is3rdActive = (level >= 10);

        if (island1stRoot != null) island1stRoot.SetActive(is1stActive);
        if (island2ndRoot != null) island2ndRoot.SetActive(is2ndActive);
        if (island3rdRoot != null) island3rdRoot.SetActive(is3rdActive);

        Debug.Log($"<color=green>[IslandController] 섬 형태 스위칭 완료 (현재 레벨: {level})</color>");

        // 활성화된 섬 기준으로 NavMesh 갱신
        RebakeNavMesh();
    }

    // [핵심 연출 및 외부 호출용] 섬을 드래그 등으로 이동시킨 직후 또는 지형 변경 시 호출
    public void RebakeNavMesh()
    {
        if (navMeshSurface == null) return;

        // Step 1: 메모리에 남아있는 이전 NavMesh 데이터 삭제 (유령 길찾기 방지)
        navMeshSurface.RemoveData();

        // Step 2: 현재 활성화된 자식 섬 메쉬를 기반으로 실시간 베이킹
        navMeshSurface.BuildNavMesh();

        Debug.Log("<color=cyan>[IslandController] NavMesh 클리어 및 재베이킹 완료!</color>");

        // Step 3: AI 에이전트 경로 재계산 알림 발송
        OnNavMeshRebaked?.Invoke();
    }
}