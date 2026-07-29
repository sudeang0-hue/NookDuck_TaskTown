//NB

using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Animal.Data;
using UI;

// 버스 연출 및 마을 동물의 스폰, 삭제, 교체(Swap) 시스템을 총괄하는 디렉터 클래스
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

    [Header("UI 연동 (옵션)")]
    [SerializeField] private VillageAnimalSetUI_Manager uiManager;
    [SerializeField] private Button confirmButton;

    [Header("버스 이동 경로 (Waypoints)")]
    [SerializeField] private Transform[] pathPoints;

    [Header("버스 이동 옵션")]
    [SerializeField, Range(1f, 10f)] private float busMoveDuration = 3.0f;
    [SerializeField, Range(0.01f, 0.2f)] private float lookAheadValue = 0.05f;

    [Header("트렁크 승/하차 점프 연출 옵션")]
    [SerializeField, Range(0.5f, 5.0f)] private float exitJumpDistance = 2.0f;
    [SerializeField, Range(0.2f, 3.0f)] private float exitJumpHeight = 1.2f;
    [SerializeField, Range(0.1f, 2.0f)] private float exitJumpDuration = 0.6f;

    /// <summary>버스가 현재 연출 중인지 여부 (중복 트리거 방지용)</summary>
    public bool IsBusSummoning { get; private set; } = false;

    private Vector3[] _waypointsCache;
    private Quaternion _initialBusRotation;
    private Sequence _busSequence;

    private void Awake()
    {
        if (villageOrigin == null) villageOrigin = transform.root;

        if (busObject != null)
        {
            Vector3 initEuler = busObject.rotation.eulerAngles;
            _initialBusRotation = Quaternion.Euler(0f, initEuler.y, 0f);
            busObject.gameObject.SetActive(false);
        }

        InitializeWaypoints();
    }

    private void Start()
    {
        if (uiManager == null)
            uiManager = FindFirstObjectByType<VillageAnimalSetUI_Manager>();

        BindConfirmButton();
    }

    #region PUBLIC API (외부 UI 및 Bridge 연동 진입점)

    // 3D 월드와 UI 상태를 자동 비교하여 승차(퇴장) 및 하차(신규) 연출을 완벽하게 동기화
    public void RequestVillagerSync()
    {
        if (IsBusSummoning) return;
        StartCoroutine(SyncVillagersRoutine());
    }

    // [Public API - 호환용] 단일 동물의 배치 연출 요청 시 호출

    public void StartBusSummon(AnimalDataSO selectedAnimal)
    {
        if (selectedAnimal == null) return;
        RequestVillagerSync();
    }

    // [Public API - 호환용] 여러 동물의 배치 연출 요청 시 호출
    public void StartBatchBusSummon(List<AnimalDataSO> animalDataList)
    {
        RequestVillagerSync();
    }

    #endregion

    private void BindConfirmButton()
    {
        if (confirmButton == null && uiManager != null)
        {
            Button[] buttons = uiManager.GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                if (btn.name.Contains("Confirm"))
                {
                    confirmButton = btn;
                    break;
                }
            }
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(OnConfirmButtonClicked);
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        }
    }

    private void OnConfirmButtonClicked()
    {
        RequestVillagerSync();
    }

    private IEnumerator SyncVillagersRoutine()
    {
        yield return null; // UI Manager 확정 반영 1프레임 대기

        if (uiManager == null) yield break;

        Transform parent = (villagerParent != null) ? villagerParent : villageOrigin;
        if (parent == null) yield break;

        List<string> remainingUIIds = new List<string>();
        foreach (var id in uiManager.PlacedAnimalIds)
        {
            if (!string.IsNullOrEmpty(id))
                remainingUIIds.Add(id.Trim().ToLower());
        }

        List<GameObject> outgoingObjects = new List<GameObject>();
        List<string> incomingIds = new List<string>();

        foreach (Transform child in parent)
        {
            if (child.TryGetComponent<VillagerIdentity>(out var identity))
            {
                string worldId = !string.IsNullOrEmpty(identity.AnimalId) ? identity.AnimalId.Trim().ToLower() : string.Empty;

                if (remainingUIIds.Contains(worldId))
                {
                    remainingUIIds.Remove(worldId);
                }
                else
                {
                    outgoingObjects.Add(child.gameObject);
                }
            }
        }

        incomingIds.AddRange(remainingUIIds);

        Debug.Log($"<color=cyan> 퇴장(승차): {outgoingObjects.Count}마리 | 입장(하차): {incomingIds.Count}마리</color>");

        if (outgoingObjects.Count > 0 || incomingIds.Count > 0)
        {
            ExecuteSwapBusSequence(outgoingObjects, incomingIds);
        }
    }

    private void ExecuteSwapBusSequence(List<GameObject> outgoingList, List<string> incomingIds)
    {
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

        _busSequence.Append(busObject.DOPunchPosition(busObject.forward * 0.4f, 0.35f, 8, 1f));
        _busSequence.AppendCallback(SanitizeBusRotation);
        _busSequence.AppendInterval(0.2f);

        // 2. 퇴장 동물 트렁크로 승차
        if (outgoingList != null && outgoingList.Count > 0)
        {
            foreach (var outgoingObj in outgoingList)
            {
                GameObject targetObj = outgoingObj;
                _busSequence.AppendCallback(() => BoardAnimalToTrunk(targetObj));
                _busSequence.AppendInterval(exitJumpDuration * 0.8f);
            }
            _busSequence.AppendInterval(0.3f);
        }

        // 3. 신규 동물 트렁크(후방)에서 하차
        if (incomingIds != null && incomingIds.Count > 0)
        {
            for (int i = 0; i < incomingIds.Count; i++)
            {
                int index = i;
                string newAnimalId = incomingIds[i];

                _busSequence.AppendCallback(() => SpawnAnimalFromTrunk(newAnimalId, index, incomingIds.Count));
                _busSequence.AppendInterval(0.25f);
            }
        }

        // 4. 버스 퇴장
        _busSequence.AppendInterval(1.0f);
        Vector3 exitTargetPosition = _waypointsCache[_waypointsCache.Length - 1] + busObject.forward * 20f;
        _busSequence.Append(
            busObject.DOMove(exitTargetPosition, 1.5f)
                     .SetEase(Ease.InQuad)
                     .OnUpdate(SanitizeBusRotation)
        );

        // 5. 시퀀스 종료
        _busSequence.OnComplete(() =>
        {
            busObject.gameObject.SetActive(false);
            IsBusSummoning = false;
        });

        _busSequence.OnKill(() => IsBusSummoning = false);
    }

    private void BoardAnimalToTrunk(GameObject targetAnimal)
    {
        if (targetAnimal == null) return;

        if (targetAnimal.TryGetComponent<NavMeshAgent>(out var agent))
            agent.enabled = false;

        Transform trunk = (trunkTransform != null) ? trunkTransform : busObject;

        Vector3 dirToTrunk = (trunk.position - targetAnimal.transform.position).normalized;
        if (dirToTrunk != Vector3.zero)
            targetAnimal.transform.rotation = Quaternion.LookRotation(dirToTrunk);

        Sequence boardSeq = DOTween.Sequence();
        boardSeq.Join(targetAnimal.transform.DOJump(trunk.position, exitJumpHeight, 1, exitJumpDuration).SetEase(Ease.InQuad));
        boardSeq.Join(targetAnimal.transform.DOScale(Vector3.zero, exitJumpDuration).SetEase(Ease.InBack));

        boardSeq.OnComplete(() => Destroy(targetAnimal));
    }

    //  버스 후방 트렁크에서 뛰어내리는 연출 로직
    private void SpawnAnimalFromTrunk(string animalId, int index, int totalCount)
    {
        GameObject prefabToSpawn = GetPrefabByAnimalId(animalId);
        if (prefabToSpawn == null) prefabToSpawn = animalPrefab;
        if (prefabToSpawn == null) return;

        // 트렁크 스폰 위치 설정
        Transform spawnPoint = (trunkTransform != null) ? trunkTransform : busObject;

        // 1. 부채꼴 하차 각도 $\theta$ 계산 (-60도 ~ +60도)
        float angleOffset = (totalCount > 1) ? (-60f + (index * (120f / (totalCount - 1)))) : 0f;
        Quaternion spreadRotation = Quaternion.Euler(0f, angleOffset, 0f);

        // 2. 버스 트렁크 후방(Vector3.back = -Vector3.forward) 방향 계산
        Vector3 exitDirection = (spawnPoint.rotation * spreadRotation) * Vector3.back;

        // 3. 내리는 동물이 바깥쪽(뛰어내리는 방향)을 바라보도록 회전 설정
        Quaternion finalAnimalRotation = Quaternion.LookRotation(exitDirection, Vector3.up);

        Transform targetParent = (villagerParent != null) ? villagerParent : villageOrigin;

        // 트렁크 위치에서 동물 생성
        GameObject newAnimal = Instantiate(prefabToSpawn, spawnPoint.position, finalAnimalRotation, targetParent);

        var identity = newAnimal.GetComponent<VillagerIdentity>();
        if (identity == null) identity = newAnimal.AddComponent<VillagerIdentity>();
        identity.Init(animalId);

        Vector3 targetScale = newAnimal.transform.localScale;
        newAnimal.transform.localScale = Vector3.zero;

        // 4. 착지 지점 계산: 트렁크 위치 + (후방 하차 벡터 * 거리)
        Vector3 landingPosition = spawnPoint.position + (exitDirection * exitJumpDistance);

        // 에디터 Scene 뷰에서 하차 궤적을 노란색 선으로 시각화
#if UNITY_EDITOR
        Debug.DrawRay(spawnPoint.position, exitDirection * exitJumpDistance, Color.yellow, 3.0f);
#endif

        // 5. 점프 하차 Tween 애니메이션
        Sequence spawnSeq = DOTween.Sequence();
        spawnSeq.Join(newAnimal.transform.DOScale(targetScale, exitJumpDuration * 0.8f).SetEase(Ease.OutBack));
        spawnSeq.Join(newAnimal.transform.DOJump(landingPosition, exitJumpHeight, 1, exitJumpDuration).SetEase(Ease.OutQuad));

        // 착지 후 살짝 찌그러졌다가 펴지는 Bouncy 효과
        Vector3 punchAmount = targetScale * 0.2f;
        punchAmount.y = -punchAmount.y;
        spawnSeq.Append(newAnimal.transform.DOPunchScale(punchAmount, 0.25f, 5, 0.5f));
    }

    private string GetCleanIdFromSO(AnimalDataSO data)
    {
        if (data == null) return string.Empty;
        string cleanId = data.name;
        int underscoreIndex = cleanId.IndexOf('_');
        if (underscoreIndex >= 0 && underscoreIndex < cleanId.Length - 1)
        {
            cleanId = cleanId.Substring(underscoreIndex + 1);
        }
        return cleanId;
    }

    private GameObject GetPrefabByAnimalId(string animalId)
    {
        if (placementDB == null || placementDB.entries == null) return null;

        foreach (var entry in placementDB.entries)
        {
            if (entry.animalData == null) continue;

            string cleanId = GetCleanIdFromSO(entry.animalData);

            if (cleanId.Trim().ToLower() == animalId.Trim().ToLower())
            {
                return placementDB.GetVisualPrefab(entry.animalData);
            }
        }
        return null;
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