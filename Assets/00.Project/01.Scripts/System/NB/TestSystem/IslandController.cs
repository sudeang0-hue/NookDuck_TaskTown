using UnityEngine;
using Unity.AI.Navigation;

public class IslandController : MonoBehaviour
{
    [Header("섬 오브젝트 (단계별 - 각각 Pre-baked NavMeshSurface 보유)")]
    [SerializeField] private GameObject island1st; // 1레벨 ~ 4레벨
    [SerializeField] private GameObject island2nd; // 5레벨 ~ 9레벨
    [SerializeField] private GameObject island3rd; // 10레벨 이상

    private void Start()
    {
        if (TownManager.Instance != null)
        {
            TownManager.Instance.OnTownLevelChanged += HandleLevelChanged;
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
        UpdateIslandVisuals(level);
    }

    private void UpdateIslandVisuals(int level)
    {
        bool is1stActive = (level >= 1 && level < 5);
        bool is2ndActive = (level >= 5 && level < 10);
        bool is3rdActive = (level >= 10);

        if (island1st != null) island1st.SetActive(is1stActive);
        if (island2nd != null) island2nd.SetActive(is2ndActive);
        if (island3rd != null) island3rd.SetActive(is3rdActive);

        Debug.Log($"<color=green>[IslandController] 섬 형태 스위칭 완료 (현재 레벨: {level})</color>");
    }

    /// <summary>
    /// [핵심 메서드] 섬의 위치를 드래그 등으로 이동시킨 '직후'에 외부(이동 스크립트)에서 반드시 호출해 주어야 합니다!
    /// 현재 활성화되어 있는 섬의 네브메시를 새로운 월드 좌표($World\ Space$) 기준으로 다시 구워줍니다.
    /// </summary>
    public void RebakeCurrentIslandNavMesh()
    {
        NavMeshSurface targetSurface = null;

        // 현재 켜져 있는 섬을 판별하여 해당 섬의 NavMeshSurface를 타겟으로 잡습니다.
        if (island1st != null && island1st.activeSelf)
        {
            targetSurface = island1st.GetComponentInChildren<NavMeshSurface>();
        }
        else if (island2nd != null && island2nd.activeSelf)
        {
            targetSurface = island2nd.GetComponentInChildren<NavMeshSurface>();
        }
        else if (island3rd != null && island3rd.activeSelf)
        {
            targetSurface = island3rd.GetComponentInChildren<NavMeshSurface>();
        }

        // 타겟이 존재한다면 이동된 좌표계에 맞춰 네브메시를 안전하게 1회 갱신
        if (targetSurface != null)
        {
            targetSurface.BuildNavMesh();
            Debug.Log($"<color=cyan>[IslandController]  섬 이동 완료 감지! 현재 활성 섬의 NavMesh 재베이킹 완료.</color>");
        }
        else
        {
            Debug.LogWarning("<color=orange>[IslandController] 현재 활성화된 섬에서 NavMeshSurface를 찾지 못했습니다!</color>");
        }
    }
}