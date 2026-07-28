using UnityEngine;
using DG.Tweening;
using System;
using Animal.Data;     // AnimalDataSO 네임스페이스
using TaskTown.Gacha;  // GachaEntryData 네임스페이스

public class VillagerPlacementDirector : MonoBehaviour
{
    [Header("오브젝트 및 부모 연결")]
    [Tooltip("마을/섬의 최상위 부모 Transform (주민 및 연출 오브젝트가 귀속될 최상위 루트)")]
    [SerializeField] private Transform villageOrigin;

    [Tooltip("생성된 주민들이 배치될 전용 부모 Transform ('Villager' 오브젝트 연결)")]
    [SerializeField] private Transform villagerParent; // Villager 할당

    [Tooltip("버스 3D 모델 Transform")]
    [SerializeField] private Transform busObject;

    [Tooltip("버스 모델 하위에 부착된 트렁크/하차 지점 Transform")]
    [SerializeField] private Transform trunkTransform;

    [Header("데이터베이스 및 기본 프리팹")]
    [Tooltip("마을 배치 DB 에셋 (AnimalDataSO로 프리팹을 찾을 때 사용)")]
    [SerializeField] private VillagePlacementDatabaseSO placementDB;

    [Tooltip("소환될 동물 주민 프리팹 (DB나 외부 전달값이 없을 때 사용할 기본값)")]
    [SerializeField] private GameObject animalPrefab;

    [Header("버스 이동 경로 (Waypoints)")]
    [Tooltip("씬에 배치된 경로 Transform 배열 (VillageOrigin의 자식으로 배치 권장)")]
    [SerializeField] private Transform[] pathPoints;

    [Header("버스 이동 옵션")]
    [SerializeField, Range(1f, 10f)] private float busMoveDuration = 3.0f;
    [SerializeField, Range(0.01f, 0.2f)] private float lookAheadValue = 0.05f;

    [Header("트렁크 하차 점프 연출 옵션")]
    [SerializeField, Range(0.5f, 5.0f)] private float exitJumpDistance = 1.8f;
    [SerializeField, Range(0.2f, 3.0f)] private float exitJumpHeight = 1.2f;
    [SerializeField, Range(0.1f, 2.0f)] private float exitJumpDuration = 0.6f;

    [Tooltip("트렁크 Forward 기준 동물이 튀어나갈 각도 (0: 직진, 90: 오른쪽, -90: 왼쪽, 180: 정반대)")]
    [SerializeField, Range(-180f, 180f)] private float exitAngleOffset = 0f;

    // 현재 버스 소환 연출이 진행 중인지 여부 (외부 스크립트 참고용)
    public bool IsBusSummoning { get; private set; } = false;

    // 캐싱 변수
    private Vector3[] _waypointsCache;
    private Quaternion _initialBusRotation;
    private Sequence _busSequence;
    private GameObject _overrideAnimalPrefab;

    private void Awake()
    {
        // 1. VillageOrigin 미할당 시 자동 탐색 예외 처리 (최상위 루트 탐색)
        if (villageOrigin == null)
        {
            villageOrigin = transform.root;
            Debug.LogWarning($"[VillagerPlacementDirector] villageOrigin이 설정되지 않아 최상위 루트({villageOrigin.name})로 자동 할당했습니다.");
        }

        // 2. 버스 초기 설정 및 숨김 처리
        if (busObject != null)
        {
            Vector3 initEuler = busObject.rotation.eulerAngles;
            _initialBusRotation = Quaternion.Euler(0f, initEuler.y, 0f);

            // 버스를 마을 부모 하위로 끌어와 섬 이동 시에도 함께 움직이도록 보장
            if (villageOrigin != null && busObject.parent != villageOrigin)
            {
                busObject.SetParent(villageOrigin, true);
            }

            busObject.gameObject.SetActive(false);
        }

        // 초기 경로 캡처
        InitializeWaypoints();
    }

    private void Update()
    {
        // 테스트용 단축키 (E)
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (placementDB != null && placementDB.entries.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, placementDB.entries.Count);
                AnimalDataSO randomAnimalData = placementDB.entries[randomIndex].animalData;
                StartBusSummon(randomAnimalData);
            }
            else
            {
                StartBusSummon();
            }
        }
    }

    // 경로 포인터들의 현재 최신 월드 좌표를 다시 수집 및 캐싱 (섬이 드래그되어 위치가 이동해도 최신 위치로 맞춤)
    private void InitializeWaypoints()
    {
        if (pathPoints == null || pathPoints.Length < 2)
        {
            Debug.LogWarning("[VillagerPlacementDirector] 경로 포인트(pathPoints)가 최소 2개 이상 설정되어야 합니다!");
            return;
        }

        _waypointsCache = new Vector3[pathPoints.Length];
        for (int i = 0; i < pathPoints.Length; i++)
        {
            if (pathPoints[i] != null)
            {
                // 섬 이동 후에도 최신 world position을 캡처
                _waypointsCache[i] = pathPoints[i].position;
            }
        }
    }

    public void StartBusSummon(AnimalDataSO animalData)
    {
        if (placementDB != null && animalData != null)
        {
            GameObject targetPrefab = placementDB.GetVisualPrefab(animalData);
            StartBusSummon(targetPrefab);
        }
        else
        {
            Debug.LogWarning("[VillagerPlacementDirector] placementDB가 없거나 animalData가 null입니다. 기본 연출을 실행합니다.");
            StartBusSummon();
        }
    }

    public void StartBusSummon(string animalID)
    {
        if (placementDB != null && !string.IsNullOrEmpty(animalID))
        {
            GameObject targetPrefab = placementDB.GetVisualPrefab(animalID);
            StartBusSummon(targetPrefab);
        }
        else
        {
            Debug.LogWarning("[VillagerPlacementDirector] placementDB가 없거나 animalID가 유효하지 않습니다. 기본 연출을 실행합니다.");
            StartBusSummon();
        }
    }

    public void StartBusSummon(GameObject customAnimalPrefab)
    {
        _overrideAnimalPrefab = customAnimalPrefab;
        StartBusSummon();
    }

    // 버스 연출 메인 로직
    public void StartBusSummon()
    {
        if (busObject == null)
        {
            Debug.LogError("[VillagerPlacementDirector] busObject가 할당되지 않았습니다!");
            return;
        }

        //연출 시작 직전에 항상 최신 경로 좌표를 재수집
        InitializeWaypoints();

        if (_waypointsCache == null || _waypointsCache.Length < 2) return;

        // 기존 진행 중인 연출 안전하게 Kill
        if (_busSequence != null && _busSequence.IsActive())
        {
            _busSequence.Kill();
        }

        // 연출 시작 직전 'IsBusSummoning' 상태를 true로 설정!
        IsBusSummoning = true;

        // 연출 시작 전 버스 활성화 및 시작 지점으로 이동
        busObject.gameObject.SetActive(true);
        busObject.position = _waypointsCache[0];
        busObject.rotation = _initialBusRotation;

        _busSequence = DOTween.Sequence();

        // CatmullRom 경로 이동 연출
        _busSequence.Append(
            busObject.DOPath(_waypointsCache, busMoveDuration, PathType.CatmullRom, PathMode.Full3D)
                     .SetLookAt(lookAheadValue, Vector3.forward, Vector3.up)
                     .SetEase(Ease.InOutQuad)
                     .OnUpdate(SanitizeBusRotation)
        );

        _busSequence.AppendCallback(SanitizeBusRotation);

        // 정차 브레이크 반동
        _busSequence.Append(busObject.DOPunchPosition(busObject.forward * 0.4f, 0.35f, 8, 1f));
        _busSequence.AppendCallback(SanitizeBusRotation);

        _busSequence.AppendInterval(0.2f);

        // 동물 하차 스폰 실행
        _busSequence.AppendCallback(SpawnAnimalFromTrunk);

        // 화면 밖으로 퇴장
        _busSequence.AppendInterval(1.5f);
        Vector3 exitTargetPosition = _waypointsCache[_waypointsCache.Length - 1] + busObject.forward * 20f;
        _busSequence.Append(
            busObject.DOMove(exitTargetPosition, 1.5f)
                     .SetEase(Ease.InQuad)
                     .OnUpdate(SanitizeBusRotation)
        );

        _busSequence.OnComplete(() =>
        {
            busObject.gameObject.SetActive(false);
            IsBusSummoning = false; // 드래그 잠금 해제
            Debug.Log("[VillagerPlacementDirector] 버스 연출 종료 및 비활성화 완료.");
        });

        // 예외 상황으로 Kill 되었을 때도 잠금을 안전하게 해제
        _busSequence.OnKill(() =>
        {
            IsBusSummoning = false;
        });
    }

    private void SanitizeBusRotation()
    {
        if (busObject == null) return;
        Vector3 currentEuler = busObject.rotation.eulerAngles;
        busObject.rotation = Quaternion.Euler(0f, currentEuler.y, 0f);
    }

    // 동물을 스폰하고 VillageOrigin 하위로 귀속시킨 뒤 하차 점프 연출을 수행
    private void SpawnAnimalFromTrunk()
    {
        GameObject prefabToSpawn = (_overrideAnimalPrefab != null) ? _overrideAnimalPrefab : animalPrefab;

        if (prefabToSpawn == null)
        {
            Debug.LogWarning("[VillagerPlacementDirector] 소환할 동물 프리팹이 설정되지 않았습니다!");
            _overrideAnimalPrefab = null;
            return;
        }

        Transform spawnPoint = (trunkTransform != null) ? trunkTransform : busObject;

        Quaternion angleRotation = Quaternion.Euler(0f, exitAngleOffset, 0f);
        Quaternion finalSpawnRotation = spawnPoint.rotation * angleRotation;
        Vector3 exitDirection = finalSpawnRotation * Vector3.forward;

        // villagerParent가 할당되어 있다면 Villager 폴더 하위로, 없으면 villageOrigin 하위로 안전하게 지정
        Transform targetParent = (villagerParent != null) ? villagerParent : villageOrigin;

        // 결정된 targetParent 하위 자식으로 인스턴스화
        GameObject newAnimal = Instantiate(prefabToSpawn, spawnPoint.position, finalSpawnRotation, targetParent);

        // 프리팹 고유의 설정 스케일을 캐싱
        Vector3 targetScale = newAnimal.transform.localScale;

        // 생성 후 연출을 위해 localScale 0으로 초기화
        newAnimal.transform.localScale = Vector3.zero;

        // 착지 위치 연산
        Vector3 landingPosition = spawnPoint.position + (exitDirection * exitJumpDistance);

        // DOTween 연출 목적지를 Vector3.one이 아닌 targetScale로 변경
        Sequence animalSeq = DOTween.Sequence();

        // 1. Vector3.one 대신 캡처한 targetScale까지 커지도록 설정
        animalSeq.Join(newAnimal.transform.DOScale(targetScale, exitJumpDuration * 0.8f).SetEase(Ease.OutBack));
        animalSeq.Join(newAnimal.transform.DOJump(landingPosition, exitJumpHeight, 1, exitJumpDuration).SetEase(Ease.OutQuad));

        // 2. 바운스 반동 효과(Punch)도 고정값이 아닌, 동물 스케일에 비례하도록 상대값 연산 (targetScale * 0.2f)
        Vector3 punchAmount = targetScale * 0.2f;
        punchAmount.y = -punchAmount.y; // Y축은 눌리는 효과를 위해 음수화
        animalSeq.Append(newAnimal.transform.DOPunchScale(punchAmount, 0.25f, 5, 0.5f));

        _overrideAnimalPrefab = null;
    }

    private void OnDestroy()
    {
        if (_busSequence != null && _busSequence.IsActive())
        {
            _busSequence.Kill();
        }
    }

    private void OnDrawGizmos()
    {
        if (pathPoints != null && pathPoints.Length >= 2)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < pathPoints.Length - 1; i++)
            {
                if (pathPoints[i] != null && pathPoints[i + 1] != null)
                {
                    Gizmos.DrawLine(pathPoints[i].position, pathPoints[i + 1].position);
                    Gizmos.DrawWireSphere(pathPoints[i].position, 0.2f);
                }
            }
        }

        Transform debugPoint = (trunkTransform != null) ? trunkTransform : busObject;
        if (debugPoint != null)
        {
            Gizmos.color = Color.yellow;
            Quaternion debugRot = debugPoint.rotation * Quaternion.Euler(0f, exitAngleOffset, 0f);
            Vector3 debugDir = debugRot * Vector3.forward;
            Vector3 debugLanding = debugPoint.position + (debugDir * exitJumpDistance);

            Gizmos.DrawRay(debugPoint.position, debugDir * exitJumpDistance);
            Gizmos.DrawWireSphere(debugLanding, 0.3f);
        }
    }
}