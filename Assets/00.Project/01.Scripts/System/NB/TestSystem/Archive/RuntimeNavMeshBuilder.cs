using System;
using UnityEngine;
using Unity.AI.Navigation; // AI Navigation 패키지 필수

[RequireComponent(typeof(NavMeshSurface))]
public class RuntimeNavMeshBuilder : MonoBehaviour
{
    private NavMeshSurface navMeshSurface;

    // 네브매쉬 재구성 완료 시 주민들에게 알릴 이벤트
    public static event Action OnNavMeshRebaked;

    private void Awake()
    {
        navMeshSurface = GetComponent<NavMeshSurface>();
    }

    /// <summary>
    /// 섬 연출이 끝난 후 호출하여 네브매쉬를 실시간으로 재구현합니다.
    /// </summary>
    public void RebakeNavMesh()
    {
        if (navMeshSurface == null) return;

        // 런타임 네브매쉬 실시간 계산
        navMeshSurface.BuildNavMesh();
        Debug.Log("[RuntimeNavMeshBuilder] 네브매쉬 리베이킹 완료!");

        // 구독 중인 주민 AI들에게 알림
        OnNavMeshRebaked?.Invoke();
    }
}