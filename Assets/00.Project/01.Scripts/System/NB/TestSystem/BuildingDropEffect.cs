using System.Collections;
using UnityEngine;

public class BuildingDropEffect : MonoBehaviour
{
    [Header("낙하 연출 설정")]
    [SerializeField] private float dropHeight = 15f;   // 하늘에서 떨어지는 시작 높이
    [SerializeField] private float dropDuration = 0.4f; // 낙하에 걸리는 시간 (초)

    private Vector3 targetPosition;
    private bool isPositionCached = false;

    // 건물의 본래 위치를 미리 기억해둡니다.
    public void CacheTargetPosition()
    {
        if (!isPositionCached)
        {
            targetPosition = transform.position;
            isPositionCached = true;
        }
    }

    public void PlayDropAnimation()
    {
        CacheTargetPosition();
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
            float t = elapsedTime / dropDuration;

            // 수학 함수를 이용해 쿵 하고 안착하는 느낌 연출
            t = Mathf.Sin(t * Mathf.PI * 0.5f);

            transform.position = Vector3.Lerp(startPos, targetPosition, t);
            yield return null;
        }

        // 3. 최종 위치 고정
        transform.position = targetPosition;
    }
}