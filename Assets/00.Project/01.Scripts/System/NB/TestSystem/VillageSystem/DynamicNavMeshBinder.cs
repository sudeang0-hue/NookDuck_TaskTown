//NB

using System;
using UnityEngine;
using Unity.AI.Navigation; // Unity AI Navigation 패키지 참조
using UnityEngine.AI;       // NavMesh 관련 Enum 참조
using Manager;

// 마을 레벨에 따라 자식 섬 오브젝트를 스위칭하고, 기존 NavMesh를 Clean한 뒤 새로 Baking하는 통합 바인더 스크립트입니다.

[RequireComponent(typeof(NavMeshSurface))]
public class DynamicNavMeshBinder : MonoBehaviour
{
    [Header("하이어라키 섬 루트 오브젝트 참조")]
    [SerializeField] private GameObject island1stRoot; // 1st_Island_Root
    [SerializeField] private GameObject island2ndRoot; // 2nd_Island_Root
    [SerializeField] private GameObject island3rdRoot; // 3rd_Island_Root

    private NavMeshSurface navMeshSurface;

    //NavMesh 재구성 완료 시 씬 내 AI들에게 알릴 글로벌 이벤트
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
            // 1. 마을 레벨 변경 이벤트 구독
            VillageSystemManager.Instance.OnVillageStateChanged += HandleVillageStateChanged;

            // 2. 게임 시작 시 현재 레벨에 맞춰 최초 섬 스위칭 및 베이킹
            UpdateIslandAndRebake(VillageSystemManager.Instance.TownLevel);
        }
        else
        {
            Debug.LogError("<color=red>[DynamicNavMeshBinder] 씬에 VillageSystemManager가 없습니다!</color>");
        }
    }

    private void OnDestroy()
    {
        if (VillageSystemManager.Instance != null)
        {
            VillageSystemManager.Instance.OnVillageStateChanged -= HandleVillageStateChanged;
        }
    }

    // NavMeshSurface 기본 수집 범위 설정
    private void ConfigureSurfaceSettings()
    {
        if (navMeshSurface == null) return;

        // Active 상태인 자식(Children) 오브젝트의 RenderMesh 형상만 수집하도록 설정
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

    // 레벨 구간 검사 후 섬 스위칭 및 NavMesh 클리어/재베이킹을 수행
    // <param name="currentLevel">현재 마을 레벨</param>
    public void UpdateIslandAndRebake(int currentLevel)
    {
        // 레벨 조건 분기 (1~4렙: Stage 1 / 5~9렙: Stage 2 / 10렙 이상: Stage 3)
        int targetStage = currentLevel >= 10 ? 3 : (currentLevel >= 5 ? 2 : 1);

        bool is1stActive = (targetStage == 1);
        bool is2ndActive = (targetStage == 2);
        bool is3rdActive = (targetStage == 3);

        // 하이어라키 섬 오브젝트 활성화/비활성화 스위칭
        if (island1stRoot != null) island1stRoot.SetActive(is1stActive);
        if (island2ndRoot != null) island2ndRoot.SetActive(is2ndActive);
        if (island3rdRoot != null) island3rdRoot.SetActive(is3rdActive);

        // 이전 NavMesh 삭제 후 신규 베이킹 진행
        ClearAndRebakeNavMesh();
    }

    // 기존 NavMesh 데이터를 완전히 클리어하고 현재 활성화된 지형을 다시 베이크
    public void ClearAndRebakeNavMesh()
    {
        if (navMeshSurface == null) return;

        // Step 1: 메모리에 남아있는 이전 NavMesh 데이터 완전히 삭제 (Clear)
        navMeshSurface.RemoveData();

        // Step 2: 현재 활성화된 자식 메쉬 기준으로 NavMesh 재구성 (Bake)
        navMeshSurface.BuildNavMesh();

        Debug.Log("<color=green>[DynamicNavMeshBinder] 기존 NavMesh 클리어 및 신규 베이킹 완료!</color>");

        // Step 3: 주민/동물 AI Agent들에게 경로 재계산 알림
        OnNavMeshRebaked?.Invoke();
    }
}