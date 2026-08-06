//NB

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 서브 프롭(부속 오브젝트) 낙하 데이터 구조체
[Serializable]
public class SubPropData
{
    [Tooltip("드롭시킬 부속 오브젝트의 Transform")]
    public Transform propTransform;

    [Tooltip("메인 건물 낙하 시작 대비 지연 시간 (초)")]
    public float delay = 0.1f;

    [Tooltip("서브 프롭 전용 낙하 높이 (0일 경우 메인 dropHeight 사용)")]
    public float customDropHeight = 0f;

    [HideInInspector] public Vector3 cachedLocalTargetPos;
}

// 메인 건물 및 서브 프롭들의 100% 수직 낙하 및 바운스 연출을 제어하는 컴포넌트
public class BuildingDropEffect : MonoBehaviour
{
    [Header("메인 건물 낙하 설정")]
    [SerializeField] private float dropHeight = 15f;    // 시작 높이
    [SerializeField] private float dropDuration = 0.35f; // 지면까지 떨어지는 시간

    [Header("통통 튕김(Bounce) 연출 설정")]
    [SerializeField] private bool enableBounce = true;
    [SerializeField] private int bounceCount = 3;
    [SerializeField] private float bounceHeight = 1.2f;
    [SerializeField] private float bounceDuration = 0.4f;
    [SerializeField] private float dampingPower = 2.0f;

    [Header("서브 프롭 설정")]
    [SerializeField] private List<SubPropData> subProps = new List<SubPropData>();
    [SerializeField] private float subPropDropDuration = 0.25f;

    private Vector3 targetPosition;
    private bool isPositionCached = false;
    private DynamicBuildingObstacle buildingObstacle;

    private void Awake()
    {
        buildingObstacle = GetComponent<DynamicBuildingObstacle>();
    }

    public void CacheTargetPosition()
    {
        if (!isPositionCached)
        {
            targetPosition = transform.position;

            foreach (var propData in subProps)
            {
                if (propData != null && propData.propTransform != null)
                {
                    propData.cachedLocalTargetPos = propData.propTransform.localPosition;
                }
            }
            isPositionCached = true;
        }
    }

    public void PlayDropAnimation()
    {
        CacheTargetPosition();
        StopAllCoroutines();

        // 1프레임 번쩍임 방지 및 공중 좌표 즉시 세팅
        SetupInitialPositions();

        StartCoroutine(MainDropRoutine());

        foreach (var propData in subProps)
        {
            if (propData != null && propData.propTransform != null)
            {
                StartCoroutine(SubPropDropRoutine(propData));
            }
        }
    }

    //월드 Y축(Vector3.up)을 기준으로 로컬 좌표계 오프셋을 역계산
    private Vector3 CalculateLocalWorldUpOffset(Transform propTransform, float height)
    {
        Vector3 worldUpOffset = Vector3.up * height;

        // 부모 오브젝트가 존재할 경우, 부모의 회전값을 역연산(InverseTransformDirection)하여 월드 수직 방향이 로컬 공간에서 어느 방향인지 정밀 계산
        if (propTransform.parent != null)
        {
            return propTransform.parent.InverseTransformDirection(worldUpOffset);
        }

        return worldUpOffset;
    }

    private void SetupInitialPositions()
    {
        // 메인 건물: 월드 좌표계 기준 정수직 위로 설정
        transform.position = targetPosition + new Vector3(0, dropHeight, 0);

        // 서브 프롭: 월드 수직 벡터를 로컬 좌표로 변환하여 설정
        foreach (var propData in subProps)
        {
            if (propData != null && propData.propTransform != null)
            {
                float actualHeight = (propData.customDropHeight > 0f) ? propData.customDropHeight : dropHeight;
                Vector3 localUpOffset = CalculateLocalWorldUpOffset(propData.propTransform, actualHeight);

                propData.propTransform.localPosition = propData.cachedLocalTargetPos + localUpOffset;
            }
        }
    }

    private IEnumerator MainDropRoutine()
    {
        Vector3 startPos = targetPosition + new Vector3(0, dropHeight, 0);
        float elapsedTime = 0f;

        while (elapsedTime < dropDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / dropDuration;
            t = t * t; // Ease-In

            transform.position = Vector3.Lerp(startPos, targetPosition, t);
            yield return null;
        }

        transform.position = targetPosition;
        OnDropComplete();

        // 바운스 연출
        if (enableBounce && bounceCount > 0)
        {
            elapsedTime = 0f;
            while (elapsedTime < bounceDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / bounceDuration;

                float bounceOffset = Mathf.Abs(Mathf.Sin(bounceCount * Mathf.PI * t))
                                   * Mathf.Pow(1f - t, dampingPower)
                                   * bounceHeight;

                transform.position = targetPosition + new Vector3(0, bounceOffset, 0);
                yield return null;
            }
        }

        transform.position = targetPosition;
    }

    private IEnumerator SubPropDropRoutine(SubPropData propData)
    {
        Transform propTransform = propData.propTransform;
        Vector3 targetLocalPos = propData.cachedLocalTargetPos;
        float actualHeight = (propData.customDropHeight > 0f) ? propData.customDropHeight : dropHeight;

        // 월드 수직 오프셋 역계산
        Vector3 localUpOffset = CalculateLocalWorldUpOffset(propTransform, actualHeight);
        Vector3 startLocalPos = targetLocalPos + localUpOffset;

        if (propData.delay > 0f)
        {
            yield return new WaitForSeconds(propData.delay);
        }

        float elapsedTime = 0f;
        while (elapsedTime < subPropDropDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / subPropDropDuration;
            t = t * t;

            propTransform.localPosition = Vector3.Lerp(startLocalPos, targetLocalPos, t);
            yield return null;
        }

        if (enableBounce && bounceCount > 0)
        {
            elapsedTime = 0f;
            float subBounceDuration = bounceDuration * 0.8f;
            float subBounceHeight = bounceHeight * 0.6f;

            while (elapsedTime < subBounceDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / subBounceDuration;

                // 바운스 시에도 월드 수직 방향으로만 튕기도록 오프셋 계산
                float bounceScalar = Mathf.Abs(Mathf.Sin(bounceCount * Mathf.PI * t))
                                   * Mathf.Pow(1f - t, dampingPower)
                                   * subBounceHeight;

                Vector3 bounceLocalOffset = CalculateLocalWorldUpOffset(propTransform, bounceScalar);
                propTransform.localPosition = targetLocalPos + bounceLocalOffset;
                yield return null;
            }
        }

        propTransform.localPosition = targetLocalPos;
    }

    private void OnDropComplete()
    {
        if (buildingObstacle != null)
        {
            buildingObstacle.ActivateObstacleAfterDrop();
        }
    }
}