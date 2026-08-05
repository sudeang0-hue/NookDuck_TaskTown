using System.Collections.Generic;
using UnityEngine;

public class IslandObjectPlacer : MonoBehaviour
{
    [Header("지형 배치 레이어 설정")]
    [SerializeField] private LayerMask groundLayer; // 섬 메쉬가 속한 레이어
    [SerializeField] private float raycastHeight = 50f; // 위에서 아래로 쏠 레이거리

    [Header("배치할 오브젝트들 (주민, 주요 건물 등)")]
    [SerializeField] private List<Transform> persistentObjects; // 섬이 바뀌어도 유지될 주민/건물 Transform

    /// <summary>
    /// 새 섬이 생성되고 NavMesh 베이킹이 끝난 후 호출하여 오브젝트 위치를 보정합니다.
    /// </summary>
    /// <param name="currentIslandInstance">생성된 섬 오브젝트</param>
    public void RepositionObjectsToIsland(GameObject currentIslandInstance)
    {
        if (currentIslandInstance == null) return;

        // 1. 새 섬에 만들어둔 'Sockets' 자식 오브젝트 탐색
        Transform socketRoot = currentIslandInstance.transform.Find("Sockets");
        if (socketRoot == null)
        {
            Debug.LogWarning("[ObjectPlacer] 섬에 'Sockets' 자식 오브젝트가 없습니다. 기본 레이캐스트 스냅을 시도합니다.");
            SnapObjectsToGroundDirectly();
            return;
        }

        // 2. 소켓 자식 위치에 맞추어 유지 오브젝트들 재배치
        for (int i = 0; i < persistentObjects.Count; i++)
        {
            if (persistentObjects[i] == null) continue;

            // Socket_0, Socket_1 등 순서대로 매핑
            if (i < socketRoot.childCount)
            {
                Transform socketTarget = socketRoot.GetChild(i);
                Vector3 targetPos = socketTarget.position;

                // 레이캐스트를 통해 지형의 정확한 Height(Y) 값 추출
                if (Physics.Raycast(targetPos + Vector3.up * raycastHeight, Vector3.down, out RaycastHit hit, raycastHeight * 2f, groundLayer))
                {
                    targetPos.y = hit.point.y;
                }

                persistentObjects[i].position = targetPos;
                persistentObjects[i].rotation = socketTarget.rotation;
            }
        }

        Debug.Log("[IslandObjectPlacer] 모든 주민 및 주요 오브젝트 재배치 완료!");
    }

    private void SnapObjectsToGroundDirectly()
    {
        foreach (var obj in persistentObjects)
        {
            if (obj == null) continue;

            Vector3 origin = obj.position + Vector3.up * raycastHeight;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, raycastHeight * 2f, groundLayer))
            {
                obj.position = hit.point;
            }
        }
    }
}