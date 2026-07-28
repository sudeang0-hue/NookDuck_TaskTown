//NB

using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Animal.Data;
using TaskTown.Gacha;

public class VillagerPlacementDirector : MonoBehaviour
{
    [Header("오브젝트 및 부모 연결")]
    [SerializeField] private Transform villageOrigin;
    [SerializeField] private Transform villagerParent;
    [SerializeField] private Transform busObject;
    [SerializeField] private Transform trunkTransform;

    [Header("데이터베이스 및 기본 프리팹")]
    [SerializeField] private VillagePlacementDatabaseSO placementDB;
    [SerializeField] private GameObject animalPrefab;

    [Header("버스 이동 경로 (Waypoints)")]
    [SerializeField] private Transform[] pathPoints;

    [Header("버스 이동 옵션")]
    [SerializeField, Range(1f, 10f)] private float busMoveDuration = 3.0f;
    [SerializeField, Range(0.01f, 0.2f)] private float lookAheadValue = 0.05f;

    [Header("트렁크 승/하차 점프 연출 옵션")]
    [SerializeField, Range(0.5f, 5.0f)] private float exitJumpDistance = 2.0f;
    [SerializeField, Range(0.2f, 3.0f)] private float exitJumpHeight = 1.2f;
    [SerializeField, Range(0.1f, 2.0f)] private float exitJumpDuration = 0.6f;
    [SerializeField, Range(-180f, 180f)] private float exitAngleOffset = 0f;

    [Header("다중 배치(Batch) 옵션")]
    [Tooltip("한 번의 버스 연출로 생성할 수 있는 최대 주민 수")]
    [SerializeField, Range(1, 10)] private int maxBatchCount = 10;

    [Tooltip("주민들이 연속으로 튀어나오는 시간 간격 (초)")]
    [SerializeField, Range(0.05f, 0.5f)] private float batchSpawnInterval = 0.2f;

    [Tooltip("여러 주민이 내릴 때 퍼지는 부채꼴 총 각도")]
    [SerializeField, Range(30f, 180f)] private float spreadAngle = 120f;

    // 외부 UI 스크립트나 입력 관리자에서 참조할 버스 연출 실행 상태
    public bool IsBusSummoning { get; private set; } = false;

    private Vector3[] _waypointsCache;
    private Quaternion _initialBusRotation;
    private Sequence _busSequence;
    private readonly List<GameObject> _overrideAnimalPrefabList = new List<GameObject>();

    private void Awake()
    {
        if (villageOrigin == null) villageOrigin = transform.root;

        if (busObject != null)
        {
            Vector3 initEuler = busObject.rotation.eulerAngles;
            _initialBusRotation = Quaternion.Euler(0f, initEuler.y, 0f);

            if (villageOrigin != null && busObject.parent != villageOrigin)
                busObject.SetParent(villageOrigin, true);

            busObject.gameObject.SetActive(false);
        }

        InitializeWaypoints();
    }

    private void InitializeWaypoints()
    {
        if (pathPoints == null || pathPoints.Length < 2) return;

        _waypointsCache = new Vector3[pathPoints.Length];
        for (int i = 0; i < pathPoints.Length; i++)
        {
            if (pathPoints[i] != null) _waypointsCache[i] = pathPoints[i].position;
        }
    }

    // =========================================================================
    // [외부 UI / 시스템 호출용 API]
    // =========================================================================

    /// <summary>
    /// 단일 동물 데이터(AnimalDataSO)를 인자로 받아 버스 소환을 시작합니다.
    /// UI 버튼 클릭 시 개별 동물을 배치할 때 호출합니다.
    /// </summary>
    public void StartBusSummon(AnimalDataSO animalData)
    {
        if (IsBusSummoning)
        {
            Debug.LogWarning("[VillagerPlacementDirector] 이미 버스 연출이 진행 중입니다.");
            return;
        }

        List<AnimalDataSO> list = new List<AnimalDataSO>();
        if (animalData != null) list.Add(animalData);
        StartBatchBusSummon(list);
    }

    /// <summary>
    /// 단일 프리팹(GameObject)을 인자로 받아 버스 소환을 시작합니다.
    /// </summary>
    public void StartBusSummon(GameObject customAnimalPrefab = null)
    {
        if (IsBusSummoning)
        {
            Debug.LogWarning("[VillagerPlacementDirector] 이미 버스 연출이 진행 중입니다.");
            return;
        }

        List<GameObject> list = new List<GameObject>();
        if (customAnimalPrefab != null) list.Add(customAnimalPrefab);
        StartBatchBusSummon(list);
    }

    /// <summary>
    /// 여러 동물 데이터 리스트를 받아 다중 소환 연출을 시작합니다. (최대 maxBatchCount마리)
    /// </summary>
    public void StartBatchBusSummon(List<AnimalDataSO> animalDataList)
    {
        if (IsBusSummoning)
        {
            Debug.LogWarning("[VillagerPlacementDirector] 이미 버스 연출이 진행 중입니다.");
            return;
        }

        List<GameObject> prefabList = new List<GameObject>();
        if (placementDB != null && animalDataList != null)
        {
            foreach (var data in animalDataList)
            {
                GameObject prefab = placementDB.GetVisualPrefab(data);
                if (prefab != null) prefabList.Add(prefab);
            }
        }
        StartBatchBusSummon(prefabList);
    }

    /// <summary>
    /// 프리팹 리스트를 직접 전달하여 다중 소환을 진행하는 최하단 메인 API입니다.
    /// </summary>
    public void StartBatchBusSummon(List<GameObject> customAnimalPrefabList)
    {
        // 중복 실행 방지 가드 클로즈
        if (IsBusSummoning) return;

        _overrideAnimalPrefabList.Clear();

        if (customAnimalPrefabList != null && customAnimalPrefabList.Count > 0)
        {
            // 최대 허용 개수만큼 Clamping
            int count = Mathf.Min(customAnimalPrefabList.Count, maxBatchCount);
            for (int i = 0; i < count; i++)
            {
                _overrideAnimalPrefabList.Add(customAnimalPrefabList[i]);
            }
        }
        else
        {
            // 비어있다면 기본 예비 프리팹 1개 추가
            if (animalPrefab != null)
                _overrideAnimalPrefabList.Add(animalPrefab);
        }

        ExecuteBusSequence(outgoingAnimal: null, isSwap: false);
    }

    /// <summary>
    /// DB에서 무작위 동물을 선택해 소환하는 UI 테스트/이벤트용 임시 버튼 연결 함수입니다.
    /// </summary>
    public void StartRandomBusSummonFromDB(int count = 1)
    {
        if (IsBusSummoning) return;

        if (placementDB != null && placementDB.entries.Count > 0)
        {
            List<AnimalDataSO> randomList = new List<AnimalDataSO>();
            int targetCount = Mathf.Clamp(count, 1, maxBatchCount);

            for (int i = 0; i < targetCount; i++)
            {
                int randomIndex = Random.Range(0, placementDB.entries.Count);
                randomList.Add(placementDB.entries[randomIndex].animalData);
            }

            StartBatchBusSummon(randomList);
        }
    }

    // =========================================================================
    // 메인 오케스트레이터 시퀀스 (DOTween)
    // =========================================================================
    private void ExecuteBusSequence(GameObject outgoingAnimal, bool isSwap)
    {
        if (busObject == null) return;

        InitializeWaypoints();
        if (_waypointsCache == null || _waypointsCache.Length < 2) return;

        if (_busSequence != null && _busSequence.IsActive()) _busSequence.Kill();

        IsBusSummoning = true;
        busObject.gameObject.SetActive(true);
        busObject.position = _waypointsCache[0];
        busObject.rotation = _initialBusRotation;

        _busSequence = DOTween.Sequence();

        // 1. 버스 진입
        _busSequence.Append(
            busObject.DOPath(_waypointsCache, busMoveDuration, PathType.CatmullRom, PathMode.Full3D)
                     .SetLookAt(lookAheadValue, Vector3.forward, Vector3.up)
                     .SetEase(Ease.InOutQuad)
                     .OnUpdate(SanitizeBusRotation)
        );

        // 2. 정차 반동 연출
        _busSequence.Append(busObject.DOPunchPosition(busObject.forward * 0.4f, 0.35f, 8, 1f));
        _busSequence.AppendCallback(SanitizeBusRotation);
        _busSequence.AppendInterval(0.2f);

        // 3. 기존 주민 퇴장 (교체 모드일 경우)
        if (outgoingAnimal != null)
        {
            _busSequence.AppendCallback(() => DespawnAnimalToTrunk(outgoingAnimal));
            _busSequence.AppendInterval(exitJumpDuration + 0.1f);
        }

        // 4. [다중 소환 연출] 주민 연속 하차
        if (outgoingAnimal == null || isSwap)
        {
            int totalCount = _overrideAnimalPrefabList.Count;

            for (int i = 0; i < totalCount; i++)
            {
                int index = i; // 클로저 이슈 방지용 로컬 변수 캡처

                // 하차 콜백
                _busSequence.AppendCallback(() => SpawnSingleAnimalFromBatch(index, totalCount));

                // 튀어나오는 시간 간격 대기
                if (i < totalCount - 1)
                {
                    _busSequence.AppendInterval(batchSpawnInterval);
                }
            }
        }

        // 5. 버스 퇴장
        _busSequence.AppendInterval(1.2f);
        Vector3 exitTargetPosition = _waypointsCache[_waypointsCache.Length - 1] + busObject.forward * 20f;
        _busSequence.Append(
            busObject.DOMove(exitTargetPosition, 1.5f)
                     .SetEase(Ease.InQuad)
                     .OnUpdate(SanitizeBusRotation)
        );

        // 6. 상태 복원 및 종료 처리
        _busSequence.OnComplete(() =>
        {
            busObject.gameObject.SetActive(false);
            IsBusSummoning = false;
            _overrideAnimalPrefabList.Clear();
        });

        _busSequence.OnKill(() => IsBusSummoning = false);
    }

    // =========================================================================
    // 다중 주민 하차 처리 (부채꼴 착지 오프셋 계산)
    // =========================================================================
    private void SpawnSingleAnimalFromBatch(int index, int totalCount)
    {
        if (index >= _overrideAnimalPrefabList.Count) return;

        GameObject prefabToSpawn = _overrideAnimalPrefabList[index];
        if (prefabToSpawn == null) prefabToSpawn = animalPrefab;
        if (prefabToSpawn == null) return;

        Transform spawnPoint = (trunkTransform != null) ? trunkTransform : busObject;

        // 부채꼴 각도 분할 계산
        float calculatedAngleOffset = exitAngleOffset;

        if (totalCount > 1)
        {
            float angleStep = spreadAngle / (totalCount - 1);
            calculatedAngleOffset += (-spreadAngle * 0.5f) + (index * angleStep);
        }

        // 지그재그 거리를 주어 캐릭터 상호 겹침 방지
        float staggeredDistance = exitJumpDistance + ((index % 2 == 0) ? 0f : 0.4f);

        // 회전 및 착지 벡터 계산
        Quaternion angleRotation = Quaternion.Euler(0f, calculatedAngleOffset, 0f);
        Quaternion finalSpawnRotation = spawnPoint.rotation * angleRotation;
        Vector3 exitDirection = finalSpawnRotation * Vector3.forward;

        Transform targetParent = (villagerParent != null) ? villagerParent : villageOrigin;

        // 인스턴스화
        GameObject newAnimal = Instantiate(prefabToSpawn, spawnPoint.position, finalSpawnRotation, targetParent);

        // 스케일 보존 및 초기화
        Vector3 targetScale = newAnimal.transform.localScale;
        newAnimal.transform.localScale = Vector3.zero;

        // 착지 지점 계산
        Vector3 landingPosition = spawnPoint.position + (exitDirection * staggeredDistance);

        // 점프 트윈 연출
        Sequence animalSeq = DOTween.Sequence();
        animalSeq.Join(newAnimal.transform.DOScale(targetScale, exitJumpDuration * 0.8f).SetEase(Ease.OutBack));
        animalSeq.Join(newAnimal.transform.DOJump(landingPosition, exitJumpHeight, 1, exitJumpDuration).SetEase(Ease.OutQuad));

        Vector3 punchAmount = targetScale * 0.2f;
        punchAmount.y = -punchAmount.y;
        animalSeq.Append(newAnimal.transform.DOPunchScale(punchAmount, 0.25f, 5, 0.5f));
    }

    private void DespawnAnimalToTrunk(GameObject targetAnimal)
    {
        if (targetAnimal == null) return;

        if (targetAnimal.TryGetComponent<NavMeshAgent>(out var agent))
        {
            agent.enabled = false;
        }

        Transform trunk = (trunkTransform != null) ? trunkTransform : busObject;

        Vector3 dirToTrunk = (trunk.position - targetAnimal.transform.position).normalized;
        if (dirToTrunk != Vector3.zero)
        {
            targetAnimal.transform.rotation = Quaternion.LookRotation(dirToTrunk);
        }

        Sequence boardSeq = DOTween.Sequence();
        boardSeq.Join(targetAnimal.transform.DOJump(trunk.position, exitJumpHeight, 1, exitJumpDuration).SetEase(Ease.InQuad));
        boardSeq.Join(targetAnimal.transform.DOScale(Vector3.zero, exitJumpDuration).SetEase(Ease.InBack));

        boardSeq.OnComplete(() =>
        {
            Destroy(targetAnimal);
        });
    }

    private void SanitizeBusRotation()
    {
        if (busObject == null) return;
        Vector3 currentEuler = busObject.rotation.eulerAngles;
        busObject.rotation = Quaternion.Euler(0f, currentEuler.y, 0f);
    }

    private void OnDestroy()
    {
        IsBusSummoning = false;
        if (_busSequence != null && _busSequence.IsActive()) _busSequence.Kill();
    }
}