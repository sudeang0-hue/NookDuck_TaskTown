//NB

using UnityEngine;
using DG.Tweening;
using System;
using Animal.Data;     // AnimalDataSO 네임스페이스
using TaskTown.Gacha;  // GachaEntryData 네임스페이스

public class VillagerPlacementDirector : MonoBehaviour
{
    [Header("오브젝트 연결")]
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
    [Tooltip("씬에 배치된 경로 Transform 배열")]
    [SerializeField] private Transform[] pathPoints;

    [Header("버스 이동 옵션")]
    [SerializeField, Range(1f, 10f)] private float busMoveDuration = 3.0f;
    [SerializeField, Range(0.01f, 0.2f)] private float lookAheadValue = 0.05f;

    [Header("트렁크 하차 점프 연출 옵션")]
    [SerializeField, Range(0.5f, 5.0f)] private float exitJumpDistance = 1.8f;
    [SerializeField, Range(0.2f, 3.0f)] private float exitJumpHeight = 1.2f;
    [SerializeField, Range(0.1f, 2.0f)] private float exitJumpDuration = 0.6f;

    // 동물이 뛰어내릴 방향 각도 오프셋
    [Tooltip("트렁크 Forward 기준 동물이 튀어나갈 각도 (0: 직진, 90: 오른쪽, -90: 왼쪽, 180: 정반대)")]
    [SerializeField, Range(-180f, 180f)] private float exitAngleOffset = 0f;

    // 초기 상태 저장 및 캐싱 변수
    private Vector3[] _waypointsCache;
    private Quaternion _initialBusRotation;
    private Sequence _busSequence;

    // 외부에서 넘겨받은 프리팹을 임시로 저장할 변수
    private GameObject _overrideAnimalPrefab;

    private void Awake()
    {
        // 1. 버스의 초기 원본 회전값 캐싱 (X, Z축 오차를 정제한 순수 Y축 회전값으로 저장)
        if (busObject != null)
        {
            Vector3 initEuler = busObject.rotation.eulerAngles;
            _initialBusRotation = Quaternion.Euler(0f, initEuler.y, 0f);

            // 씬 시작 시 버스를 꺼두어 렌더링/컬링 연산 부하 차단
            busObject.gameObject.SetActive(false);
        }

        // 2. 경로 데이터 캐싱
        InitializeWaypoints();
    }

    private void Update()
    {
        // 테스트용 단축키 (E) - placementDB에서 랜덤으로 동물을 하나 뽑아 테스트
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (placementDB != null && placementDB.entries.Count > 0)
            {
                // DB 항목 중 무작위 하나 선택
                int randomIndex = UnityEngine.Random.Range(0, placementDB.entries.Count);
                AnimalDataSO randomAnimalData = placementDB.entries[randomIndex].animalData;

                // 해당 동물의 데이터로 버스 연출 시작!
                StartBusSummon(randomAnimalData);
            }
            else
            {
                // DB가 없거나 비어있다면 기본 예비 프리팹으로 실행
                StartBusSummon();
            }
        }
    }

    // Transform 배열에서 Vector3 위치 데이터 추출 및 캐싱
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
            _waypointsCache[i] = pathPoints[i].position;
        }
    }

    // AnimalDataSO 에셋을 직접 받아서 호출하는 방식
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

    // 동물 ID(string)로 호출하는 방식
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

    // GameObject 프리팹을 직접 받아서 호출하는 방식
    public void StartBusSummon(GameObject customAnimalPrefab)
    {
        _overrideAnimalPrefab = customAnimalPrefab;
        StartBusSummon(); // 기존 연출 로직 실행
    }

    // 버스 연출 시작 메인 로직
    public void StartBusSummon()
    {
        if (busObject == null)
        {
            Debug.LogError("[VillagerPlacementDirector] busObject가 할당되지 않았습니다!");
            return;
        }

        if (_waypointsCache == null || _waypointsCache.Length < 2)
        {
            InitializeWaypoints();
            if (_waypointsCache == null || _waypointsCache.Length < 2) return;
        }

        // 진행 중인 시퀀스가 있다면 안전하게 중단 및 메모리 해제
        if (_busSequence != null && _busSequence.IsActive())
        {
            _busSequence.Kill();
        }

        // 연출 시작 직전 버스 활성화 및 초기 위치/회전 완전 정제
        busObject.gameObject.SetActive(true);
        busObject.position = _waypointsCache[0];
        busObject.rotation = _initialBusRotation;

        _busSequence = DOTween.Sequence();

        // CatmullRom 경로 이동 + 프레임별 Y축 회전 강제 고정 (Yaw Locking)
        _busSequence.Append(
            busObject.DOPath(_waypointsCache, busMoveDuration, PathType.CatmullRom, PathMode.Full3D)
                     .SetLookAt(lookAheadValue, Vector3.forward, Vector3.up)
                     .SetEase(Ease.InOutQuad)
                     .OnUpdate(SanitizeBusRotation) // 매 프레임 X, Z 축 기울어짐 제거
        );

        // 경로 완료 후 미세 회전 오차 0°로 보정
        _busSequence.AppendCallback(SanitizeBusRotation);

        // 정차 브레이크 반동 (Punch 후 회전 비틀림 방지를 위해 완료 후 보정)
        _busSequence.Append(busObject.DOPunchPosition(busObject.forward * 0.4f, 0.35f, 8, 1f));
        _busSequence.AppendCallback(SanitizeBusRotation);

        // 트렁크 문 열림 대기
        _busSequence.AppendInterval(0.2f);

        // 동물 주민 하차 스폰
        _busSequence.AppendCallback(SpawnAnimalFromTrunk);

        // 대기 후 화면 밖으로 퇴장
        _busSequence.AppendInterval(1.5f);
        Vector3 exitTargetPosition = _waypointsCache[_waypointsCache.Length - 1] + busObject.forward * 20f;
        _busSequence.Append(
            busObject.DOMove(exitTargetPosition, 1.5f)
                     .SetEase(Ease.InQuad)
                     .OnUpdate(SanitizeBusRotation)
        );

        // 퇴장 완료 시 자동 비활성화
        _busSequence.OnComplete(() =>
        {
            busObject.gameObject.SetActive(false);
            Debug.Log("[VillagerPlacementDirector] 버스가 성공적으로 퇴장하여 비활성화되었습니다.");
        });
    }

    // 버스의 X(Pitch)축과 Z(Roll)축 회전 오차를 원천 차단하고 Y(Yaw)축만 남기는 보정 메서드
    private void SanitizeBusRotation()
    {
        if (busObject == null) return;

        Vector3 currentEuler = busObject.rotation.eulerAngles;
        // X축과 Z축을 완벽히 0°로 픽스하여 평면 회전만 유지
        busObject.rotation = Quaternion.Euler(0f, currentEuler.y, 0f);
    }

    // 동물 주민 스폰 및 방향 오프셋이 적용된 하차 연출
    private void SpawnAnimalFromTrunk()
    {
        // 외부에서 넘겨받은 프리팹이 있으면 우선 사용하고, 없으면 기본 animalPrefab 사용
        GameObject prefabToSpawn = (_overrideAnimalPrefab != null) ? _overrideAnimalPrefab : animalPrefab;

        if (prefabToSpawn == null)
        {
            Debug.LogWarning("[VillagerPlacementDirector] 소환할 동물 프리팹이 설정되지 않았습니다!");
            _overrideAnimalPrefab = null;
            return;
        }

        Transform spawnPoint = (trunkTransform != null) ? trunkTransform : busObject;

        // 오프셋 회전값 계산 (Y축 회전)
        Quaternion angleRotation = Quaternion.Euler(0f, exitAngleOffset, 0f);

        // 최종 바라볼 회전 및 뛰어내릴 방향 벡터 산출
        Quaternion finalSpawnRotation = spawnPoint.rotation * angleRotation;
        Vector3 exitDirection = finalSpawnRotation * Vector3.forward;

        // 결정된 프리팹으로 인스턴스화
        GameObject newAnimal = Instantiate(prefabToSpawn, spawnPoint.position, finalSpawnRotation);
        newAnimal.transform.localScale = Vector3.zero;

        // 착지 위치 계산
        Vector3 landingPosition = spawnPoint.position + (exitDirection * exitJumpDistance);

        // 점프 연출 시퀀스
        Sequence animalSeq = DOTween.Sequence();
        animalSeq.Join(newAnimal.transform.DOScale(Vector3.one, exitJumpDuration * 0.8f).SetEase(Ease.OutBack));
        animalSeq.Join(newAnimal.transform.DOJump(landingPosition, exitJumpHeight, 1, exitJumpDuration).SetEase(Ease.OutQuad));
        animalSeq.Append(newAnimal.transform.DOPunchScale(new Vector3(0.2f, -0.2f, 0.2f), 0.25f, 5, 0.5f));

        // 사용이 끝난 임시 변수 초기화
        _overrideAnimalPrefab = null;
    }

    private void OnDestroy()
    {
        // 씬 전환/오브젝트 파괴 시 DOTween 메모리 누수 방지
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

        // [시각화 디버깅] 하차 방향 및 착지 예상 위치 기즈모 표시
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