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
    [SerializeField] public Transform propTransform;

    [Tooltip("메인 건물 착지 후 서브 프롭 낙하까지의 추가 지연 시간 (초)")]
    [SerializeField] public float delay = 0.05f;

    [Tooltip("서브 프롭 전용 낙하 높이 (0일 경우 메인 dropHeight 사용)")]
    [SerializeField] public float customDropHeight = 0f;

    // 내부 계산용 로컬 위치 캡처 변수
    [HideInInspector] public Vector3 cachedLocalTargetPos;
}

// 메인 건물 및 서브 프롭 수직 낙하, 바운스 연출 제어 컴포넌트
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

    [Header("서브 프롭(부속품) 연쇄 낙하 설정")]
    [SerializeField] private List<SubPropData> subProps = new List<SubPropData>();
    [SerializeField] private float subPropDropDuration = 0.2f;

    [Header("메인 건물 VFX (먼지/스모그 이펙트) 설정")]
    [Tooltip("메인 건물 착지 시에만 생성할 대표 먼지/스모그 VFX 프리팹")]
    [SerializeField] private GameObject mainDustVfxPrefab;
    [SerializeField] private Vector3 mainVfxOffset = Vector3.zero;
    [SerializeField] private Vector3 mainVfxScale = Vector3.one;

    // C# Action 이벤트 (필요 시 외부 스크립트 연동)
    public event Action<Vector3> OnMainLandedAction;
    public event Action<Vector3> OnSubPropLandedAction;

    //월드 좌표 대신 안전한 로컬 목표 좌표를 저장합니다.
    private Vector3 targetLocalPosition;
    private bool isPositionCached = false;
    private DynamicBuildingObstacle buildingObstacle;
    private bool isMainLanded = false;

    // GC Alloc 최적화를 위한 WaitForSeconds 캐싱
    private readonly Dictionary<float, WaitForSeconds> waitForSecondsCache = new Dictionary<float, WaitForSeconds>();

    private void Awake()
    {
        buildingObstacle = GetComponent<DynamicBuildingObstacle>();
    }

    private void Start()
    {
        CacheTargetPosition();
    }

    // 건물 및 부속 오브젝트의 원본 '로컬(Local)' 위치를 안전하게 백업
    public void CacheTargetPosition()
    {
        if (isPositionCached) return;

        // 월드 좌표(transform.position)가 아닌 로컬 좌표(transform.localPosition) 백업
        targetLocalPosition = transform.localPosition;

        foreach (var propData in subProps)
        {
            if (propData != null && propData.propTransform != null)
            {
                propData.cachedLocalTargetPos = propData.propTransform.localPosition;
            }
        }
        isPositionCached = true;
    }

    // 외부에서 연쇄 낙하 연출을 시작하는 메인 함수
    public void PlayDropAnimation()
    {
        CacheTargetPosition();
        StopAllCoroutines();

        isMainLanded = false;
        SetupInitialPositions();

        // 1. 메인 건물 낙하 시작
        StartCoroutine(MainDropRoutine());

        // 2. 부속품(서브 프롭) 연쇄 낙하 시작
        foreach (var propData in subProps)
        {
            if (propData != null && propData.propTransform != null)
            {
                StartCoroutine(SubPropDropRoutine(propData));
            }
        }
    }

    private void SetupInitialPositions()
    {
        // 로컬 Y축 기준으로 낙하 시작 위치 설정
        transform.localPosition = targetLocalPosition + new Vector3(0, dropHeight, 0);

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

    private Vector3 CalculateLocalWorldUpOffset(Transform propTransform, float height)
    {
        Vector3 worldUpOffset = Vector3.up * height;
        if (propTransform.parent != null)
        {
            return propTransform.parent.InverseTransformDirection(worldUpOffset);
        }
        return worldUpOffset;
    }

    // 메인 건물 낙하 코루틴 (로컬 좌표계 기반)
    private IEnumerator MainDropRoutine()
    {
        Vector3 startLocalPos = targetLocalPosition + new Vector3(0, dropHeight, 0);
        float elapsedTime = 0f;

        // 1. 수직 낙하 (가속도 적용: EaseInQuad -> t^2)
        while (elapsedTime < dropDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / dropDuration;
            t = t * t;

            // localPosition을 보간하여 섬이 드래그되어도 완벽히 추종함
            transform.localPosition = Vector3.Lerp(startLocalPos, targetLocalPosition, t);
            yield return null;
        }

        transform.localPosition = targetLocalPosition;
        isMainLanded = true;

        // 2. 메인 건물 착지 VFX 스폰 (VFX는 스폰 시점의 실시간 월드 좌표 전달)
        Vector3 mainLandWorldPos = transform.TransformPoint(mainVfxOffset);
        SpawnDustVfx(mainDustVfxPrefab, mainLandWorldPos, mainVfxScale);

        OnMainLandedAction?.Invoke(transform.position);

        OnDropComplete();

        // 3. 메인 건물 바운스 연출 (로컬 좌표 기준)
        if (enableBounce && bounceCount > 0)
        {
            elapsedTime = 0f;
            while (elapsedTime < bounceDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / bounceDuration;

                // 감쇄 파동 수학식 적용
                float bounceOffset = Mathf.Abs(Mathf.Sin(bounceCount * Mathf.PI * t))
                                   * Mathf.Pow(1f - t, dampingPower)
                                   * bounceHeight;

                transform.localPosition = targetLocalPosition + new Vector3(0, bounceOffset, 0);
                yield return null;
            }
        }

        transform.localPosition = targetLocalPosition;
    }

    // 서브 프롭(부속품) 연쇄 낙하 코루틴
    private IEnumerator SubPropDropRoutine(SubPropData propData)
    {
        while (!isMainLanded)
        {
            yield return null;
        }

        if (propData.delay > 0f)
        {
            yield return GetCachedWaitForSeconds(propData.delay);
        }

        Transform propTransform = propData.propTransform;
        Vector3 targetLocalPos = propData.cachedLocalTargetPos;
        float actualHeight = (propData.customDropHeight > 0f) ? propData.customDropHeight : dropHeight;

        Vector3 localUpOffset = CalculateLocalWorldUpOffset(propTransform, actualHeight);
        Vector3 startLocalPos = targetLocalPos + localUpOffset;

        float elapsedTime = 0f;
        while (elapsedTime < subPropDropDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / subPropDropDuration;
            t = t * t;

            propTransform.localPosition = Vector3.Lerp(startLocalPos, targetLocalPos, t);
            yield return null;
        }

        propTransform.localPosition = targetLocalPos;

        OnSubPropLandedAction?.Invoke(propTransform.position);

        // 서브 프롭 바운스 연출
        if (enableBounce && bounceCount > 0)
        {
            elapsedTime = 0f;
            float subBounceDuration = bounceDuration * 0.8f;
            float subBounceHeight = bounceHeight * 0.6f;

            while (elapsedTime < subBounceDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / subBounceDuration;

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

    private void SpawnDustVfx(GameObject vfxPrefab, Vector3 spawnPosition, Vector3 scale)
    {
        if (vfxPrefab == null) return;

        GameObject vfxInstance = Instantiate(vfxPrefab, spawnPosition, Quaternion.identity);
        vfxInstance.transform.localScale = scale;

        ParticleSystem ps = vfxInstance.GetComponentInChildren<ParticleSystem>();
        if (ps != null)
        {
            float totalLifeTime = ps.main.duration + ps.main.startLifetime.constantMax;
            Destroy(vfxInstance, totalLifeTime);
        }
        else
        {
            Destroy(vfxInstance, 2.5f);
        }
    }

    private void OnDropComplete()
    {
        if (buildingObstacle != null)
        {
            buildingObstacle.ActivateObstacleAfterDrop();
        }
    }

    private WaitForSeconds GetCachedWaitForSeconds(float seconds)
    {
        if (!waitForSecondsCache.TryGetValue(seconds, out var wait))
        {
            wait = new WaitForSeconds(seconds);
            waitForSecondsCache.Add(seconds, wait);
        }
        return wait;
    }

    //씬 뷰에서 목표 로컬 위치를 실시간 모니터링
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 currentTargetWorldPos = transform.parent != null
            ? transform.parent.TransformPoint(targetLocalPosition)
            : targetLocalPosition;

        Gizmos.DrawWireSphere(currentTargetWorldPos, 0.5f);
    }
}