//NB

using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// 건물의 실시간 배치 및 낙하 연출 이후 NavMesh에 국소적인 구멍(Carve)을 내어 AI가 건물을 뚫고 지나가지 못하도록 제어하는 컴포넌트입니다.
[RequireComponent(typeof(NavMeshObstacle))]
public class DynamicBuildingObstacle : MonoBehaviour
{
    [Header("Carving 설정")]
    [SerializeField] private bool carveOnStart = false; // 시작하자마자 Carving 켤지 여부
    [SerializeField] private float carvingDelayAfterDrop = 0.05f; // 낙하 완료 후 딜레이 시간

    private NavMeshObstacle obstacle;

    private void Awake()
    {
        obstacle = GetComponent<NavMeshObstacle>();

        // 1. 기본 옵션 설정 (이동하지 않는 고정 장애물 모드)
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.carveOnlyStationary = true;

        // 초기 상태에는 Carving을 꺼두어 낙하 전 길을 막지 않도록 함
        obstacle.carving = carveOnStart;
    }

    // BuildingDropEffect의 낙하 애니메이션이 완료된 직후 이 메서드를 호출
    public void ActivateObstacleAfterDrop()
    {
        StartCoroutine(EnableCarvingRoutine());
    }

    private IEnumerator EnableCarvingRoutine()
    {
        // 물리 및 애니메이션 연산이 정착할 때까지 1프레임 대기
        yield return new WaitForSeconds(carvingDelayAfterDrop);

        if (obstacle != null)
        {
            obstacle.carving = true;
            Debug.Log($"<color=yellow>[DynamicBuildingObstacle] '{gameObject.name}' 위치에 NavMesh Carving 적용 완료!</color>");
        }
    }

    // 건물이 파괴되거나 제거될 때 Carving을 해제
    public void DisableObstacle()
    {
        if (obstacle != null)
        {
            obstacle.carving = false;
        }
    }
}