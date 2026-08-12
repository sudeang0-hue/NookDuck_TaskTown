//NB

using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

// 섬 오브젝트의 드래그 및 관성 이동, 예외 레이어 감지를 전담하는 클래스
// 불필요한 미사용 변수를 제거하고 깔끔하게 정돈된 최신 버전
public class ObjectDragger : MonoBehaviour
{
    private Vector3 offset;
    private bool isDragging = false;
    private GameMasterManager gameManager;
    private VillagerPlacementDirector placementDirector;
    private Camera _mainCamera;

    [Header("관성(부드러움) 설정")]
    [Range(0.01f, 0.5f)]
    public float smoothTime = 0f;
    private Vector3 dragVelocity = Vector3.zero;

    private List<Collider> _villageHouseColliders = new List<Collider>();
    private bool _lastExpandedState = true;

    // 위치 상태 관리 필드 변수 (미사용 변수 제거 완료)
    private Vector3 savedExpandedPos;
    private Vector3 savedMinimizedPos;

    void Awake()
    {
        _mainCamera = Camera.main;
    }

    void Start()
    {
        gameManager = Object.FindFirstObjectByType<GameMasterManager>();
        placementDirector = Object.FindFirstObjectByType<VillagerPlacementDirector>();

        CacheVillageHouseColliders();

        // 저장된 데이터가 있는지 먼저 확인 후 로드
        LoadIslandPosition();

        if (gameManager != null)
        {
            _lastExpandedState = gameManager.GetIsExpanded();
        }
    }

    /// <summary>
    /// 저장된 섬의 위치를 각각 독립적으로 불러옵니다.
    /// </summary>
    private void LoadIslandPosition()
    {
        // 1. 플레이어 프리팹에 최초 실행 플래그("IsPositionInitialized")가 존재하는지 확인합니다.
        if (PlayerPrefs.HasKey("IsPositionInitialized"))
        {
            // --- [재방문 유저]: 저장된 정밀 위치 로드 ---
            float expX = PlayerPrefs.GetFloat("Island_Exp_X", 0f);
            float expY = PlayerPrefs.GetFloat("Island_Exp_Y", 0f);
            float expZ = PlayerPrefs.GetFloat("Island_Exp_Z", 0f);
            savedExpandedPos = new Vector3(expX, expY, expZ);

            float minX = PlayerPrefs.GetFloat("Island_Min_X", 0f);
            float minY = PlayerPrefs.GetFloat("Island_Min_Y", 0f);
            float minZ = PlayerPrefs.GetFloat("Island_Min_Z", 0f);
            savedMinimizedPos = new Vector3(minX, minY, minZ);

            // 현재 확장/축소 상태에 맞는 위치를 즉시 반영합니다.
            bool isExpanded = gameManager == null || gameManager.GetIsExpanded();
            transform.position = isExpanded ? savedExpandedPos : savedMinimizedPos;

            Debug.Log($"<color=cyan>[ObjectDragger] 저장된 위치 로드 완료 - 확장: {savedExpandedPos}, 축소: {savedMinimizedPos}</color>");
        }
        else
        {
            // --- [최초 실행]: (0, 0, 0) 원점 고정 및 초기화 ---
            savedExpandedPos = Vector3.zero;
            savedMinimizedPos = Vector3.zero;
            transform.position = Vector3.zero;

            // 최초 실행 플래그를 심고 현재의 제로(0) 벡터 상태를 데이터베이스에 영구 기록합니다.
            PlayerPrefs.SetInt("IsPositionInitialized", 1);
            SaveIslandPosition();

            Debug.Log("<color=yellow>[ObjectDragger] 감지된 저장 데이터 없음 (최초 실행): 섬 위치를 (0,0,0) 원점으로 초기화합니다.</color>");
        }
    }

    /// <summary>
    /// 드래그가 종료되거나 게임이 꺼질 때 현재 위치를 저장하는 메서드입니다.
    /// </summary>
    private void SaveIslandPosition()
    {
        PlayerPrefs.SetFloat("Island_Exp_X", savedExpandedPos.x);
        PlayerPrefs.SetFloat("Island_Exp_Y", savedExpandedPos.y);
        PlayerPrefs.SetFloat("Island_Exp_Z", savedExpandedPos.z);

        PlayerPrefs.SetFloat("Island_Min_X", savedMinimizedPos.x);
        PlayerPrefs.SetFloat("Island_Min_Y", savedMinimizedPos.y);
        PlayerPrefs.SetFloat("Island_Min_Z", savedMinimizedPos.z);

        PlayerPrefs.Save();
    }

    private void OnApplicationQuit()
    {
        SaveIslandPosition();
    }

    private void OnDisable()
    {
        SaveIslandPosition();
    }
    public Vector3 GetSavedMinimizedPos()
    {
        return savedMinimizedPos;
    }

    void Update()
    {
        // 1. 카메라가 특정 주민을 팔로우 포커스 중일 때는 섬 드래그 차단
        if (CameraDirector.Instance != null && CameraDirector.Instance.IsFocused)
        {
            if (isDragging)
            {
                ToggleChildAgents(true);
                isDragging = false;
            }
            return;
        }

        bool isExpanded = gameManager == null || gameManager.GetIsExpanded();
        bool isBusSummoning = placementDirector != null && placementDirector.IsBusSummoning;

        // 확장/축소 상태 변경 시 Collider 상태 최적화 제어
        if (_lastExpandedState != isExpanded)
        {
            _lastExpandedState = isExpanded;
            SetVillageHouseCollidersEnabled(isExpanded);
        }

        // 2. 버스 연출 진행 중 차단
        if (isBusSummoning)
        {
            if (isDragging)
            {
                ToggleChildAgents(true);
                isDragging = false;
            }
            return;
        }

        // 3. 마우스 클릭 (드래그 시작)
        if (Input.GetMouseButtonDown(0))
        {
            if (InputGuard.IsPointerOverUI()) return;

            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.transform.GetComponentInParent<VillagerAI>() != null) return;

                int villageHouseLayer = LayerMask.NameToLayer("VillageHouse");
                if (villageHouseLayer != -1 && hit.collider.gameObject.layer == villageHouseLayer) return;

                if (hit.transform == this.transform || hit.transform.IsChildOf(this.transform))
                {
                    isDragging = true;

                    Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(
                        new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10f));

                    offset = gameObject.transform.position - mouseWorldPos;
                    dragVelocity = Vector3.zero;

                    // 드래그 중에는 주민 Agent 일시 중지
                    ToggleChildAgents(false);
                }
            }
        }

        // 4. 드래그 진행 및 해제 처리
        if (isDragging && Input.GetMouseButton(0))
        {
            Vector3 targetPos = _mainCamera.ScreenToWorldPoint(
                new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10f)) + offset;

            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref dragVelocity, smoothTime);

            // 드래그 중 실시간으로 현재 확장 위치 변수 갱신
            if (isExpanded)
            {
                savedExpandedPos = transform.position;
            }
            else
            {
                savedMinimizedPos = transform.position;
            }
        }
        else
        {
            if (isDragging)
            {
                ToggleChildAgents(true);
                isDragging = false;

                // 드래그 종료 시점 최종 저장
                SaveIslandPosition();
            }
        }
    }

    private void CacheVillageHouseColliders()
    {
        _villageHouseColliders.Clear();
        int villageLayer = LayerMask.NameToLayer("VillageHouse");

        Collider[] allColliders = GetComponentsInChildren<Collider>(true);
        foreach (var col in allColliders)
        {
            if (villageLayer != -1 && col.gameObject.layer == villageLayer)
            {
                _villageHouseColliders.Add(col);
            }
        }
    }

    private void SetVillageHouseCollidersEnabled(bool enable)
    {
        foreach (var col in _villageHouseColliders)
        {
            if (col != null) col.enabled = enable;
        }
    }

    private void ToggleChildAgents(bool enable)
    {
        bool isExpanded = gameManager == null || gameManager.GetIsExpanded();
        bool finalEnableState = enable && isExpanded;

        NavMeshAgent[] agents = GetComponentsInChildren<NavMeshAgent>(true);
        foreach (var agent in agents)
        {
            if (agent == null) continue;

            if (!finalEnableState)
            {
                agent.enabled = false;

                Animator anim = agent.GetComponent<Animator>();
                if (anim != null)
                {
                    anim.SetBool("isWalking", false);
                }
            }
            else
            {
                agent.enabled = true;
            }
        }
    }
}