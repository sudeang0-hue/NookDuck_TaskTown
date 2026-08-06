//NB

using System.Collections;
using UnityEngine;

// 건물이 하늘에서 떨어지는 낙하 연출을 제어하고, 낙하 완료 시 NavMesh 장애물(Carving)을 활성화하는 컴포넌트입니다.
public class BuildingDropEffect : MonoBehaviour
{
    [Header("낙하 연출 설정")]
    [SerializeField] private float dropHeight = 15f;    // 하늘에서 떨어지는 시작 높이
    [SerializeField] private float dropDuration = 0.4f; // 낙하 시간 ($T_{drop}$)

    private Vector3 targetPosition;
    private bool isPositionCached = false;

    // [성능 최적화] 컴포넌트 사전 캐싱
    private DynamicBuildingObstacle buildingObstacle;

    private void Awake()
    {
        // 씬 시작 시 컴포넌트를 미리 캐싱하여 런타임 GC 발생 방지
        buildingObstacle = GetComponent<DynamicBuildingObstacle>();
    }

    // 건물의 본래 위치(목표 지점)를 미리 기억해둡니다.
    public void CacheTargetPosition()
    {
        if (!isPositionCached)
        {
            targetPosition = transform.position;
            isPositionCached = true;
        }
    }

    // 외부(BuildingManager 등)에서 낙하 애니메이션을 재생할 때 호출
    public void PlayDropAnimation()
    {
        CacheTargetPosition();

        // 이전 코루틴이 진행 중이라면 중단
        StopAllCoroutines();
        StartCoroutine(DropRoutine());
    }

    private IEnumerator DropRoutine()
    {
        // 1. 시작 위치를 목표 위치보다 하늘 위로 설정
        Vector3 startPos = targetPosition + new Vector3(0, dropHeight, 0);
        transform.position = startPos;

        float elapsedTime = 0f;

        // 2. 시간에 따라 부드럽게 아래로 이동 (Ease-Out 곡선 적용)
        while (elapsedTime < dropDuration)
        {
            elapsedTime += Time.deltaTime;

            // 정규화 시간 $t \in [0, 1]$
            float t = elapsedTime / dropDuration;

            // Ease-Out Sine 보간 함수: $f(t) = \sin(\frac{\pi}{2} \cdot t)$
            t = Mathf.Sin(t * Mathf.PI * 0.5f);

            transform.position = Vector3.Lerp(startPos, targetPosition, t);
            yield return null;
        }

        // 3. 최종 위치 고정
        transform.position = targetPosition;

        // 4. [핵심 수정 지점] 낙하 완전 종료 후 완충 이벤트 및 NavMesh Carving 호출
        OnDropComplete();
    }

    // 낙하 연출이 마무리된 프레임에 호출되는 콜백 메서드입니다.
    private void OnDropComplete()
    {
        // NavMesh 장애물 Carving 활성화
        if (buildingObstacle != null)
        {
            buildingObstacle.ActivateObstacleAfterDrop();
        }
        else
        {
            Debug.LogWarning($"<color=orange>[BuildingDropEffect] '{gameObject.name}'에 DynamicBuildingObstacle 컴포넌트가 붙어있지 않습니다.</color>");
        }
    }
}