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

// 메인 건물 및 서브 프롭 수직 낙하, 바운스, 충돌 연출 제어 컴포넌트
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

    // C# Action을 활용한 고성능 매핑 이벤트 (필요 시 코드에서 구독 가능, GC Alloc 없음)
    public event Action<Vector3> OnMainLandedAction;
    public event Action<Vector3> OnSubPropLandedAction;

    private Vector3 targetPosition;
    private bool isPositionCached = false;
    private DynamicBuildingObstacle buildingObstacle;
    private bool isMainLanded = false;

    // GC Alloc 최적화를 위한 WaitForSeconds 캐싱 딕셔너리
    private readonly Dictionary<float, WaitForSeconds> waitForSecondsCache = new Dictionary<float, WaitForSeconds>();

    private void Awake()
    {
        buildingObstacle = GetComponent<DynamicBuildingObstacle>();
    }

    private void Start()
    {
        CacheTargetPosition();
    }

    // 건물 및 부속 오브젝트의 원본 로컬 위치를 백업합니다.
    public void CacheTargetPosition()
    {
        if (isPositionCached) return;

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
        transform.position = targetPosition + new Vector3(0, dropHeight, 0);

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

    // 메인 건물 낙하 코루틴
    private IEnumerator MainDropRoutine()
    {
        Vector3 startPos = targetPosition + new Vector3(0, dropHeight, 0);
        float elapsedTime = 0f;

        // 1. 수직 낙하 (가속도 적용: EaseInQuad -> t^2)
        while (elapsedTime < dropDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / dropDuration;
            t = t * t;

            transform.position = Vector3.Lerp(startPos, targetPosition, t);
            yield return null;
        }

        transform.position = targetPosition;
        isMainLanded = true;

        // 2. 메인 건물 착지 VFX 스폰 및 C# Action 이벤트 호출
        Vector3 mainLandWorldPos = transform.TransformPoint(mainVfxOffset);
        SpawnDustVfx(mainDustVfxPrefab, mainLandWorldPos, mainVfxScale);

        OnMainLandedAction?.Invoke(targetPosition);

        OnDropComplete();

        // 3. 메인 건물 바운스 연출
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

                transform.position = targetPosition + new Vector3(0, bounceOffset, 0);
                yield return null;
            }
        }

        transform.position = targetPosition;
    }

    // 서브 프롭(부속품) 연쇄 낙하 코루틴
    private IEnumerator SubPropDropRoutine(SubPropData propData)
    {
        // 메인 건물이 착지할 때까지 대기
        while (!isMainLanded)
        {
            yield return null;
        }

        // 지연 시간(Delay) 대기
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
            t = t * t; // 가속 낙하

            propTransform.localPosition = Vector3.Lerp(startLocalPos, targetLocalPos, t);
            yield return null;
        }

        propTransform.localPosition = targetLocalPos;

        // C# Action 이벤트만 발동 (필요 시 외부 스크립트 연동용)
        Vector3 landedWorldPos = propTransform.position;
        OnSubPropLandedAction?.Invoke(landedWorldPos);

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

    // 메인 건물 전용 VFX 파티클 스폰 및 수명 자동 관리
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

    // WaitForSeconds 객체 재사용으로 GC 할당 최소화
    private WaitForSeconds GetCachedWaitForSeconds(float seconds)
    {
        if (!waitForSecondsCache.TryGetValue(seconds, out var wait))
        {
            wait = new WaitForSeconds(seconds);
            waitForSecondsCache.Add(seconds, wait);
        }
        return wait;
    }
}