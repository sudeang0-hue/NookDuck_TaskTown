//NB

using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

// 섬 오브젝트의 드래그 및 관성 이동, 예외 레이어 감지를 전담하는 클래스
// 축소 상태에서의 주민 정지 상태를 완벽히 보존하도록 보완
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

    void Awake()
    {
        _mainCamera = Camera.main;
    }

    void Start()
    {
        gameManager = Object.FindFirstObjectByType<GameMasterManager>();
        placementDirector = Object.FindFirstObjectByType<VillagerPlacementDirector>();

        CacheVillageHouseColliders();
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
        }
        else
        {
            if (isDragging)
            {
                //드래그가 끝나도 확대(Expanded) 상태일 때만 Agent를 켭니다.
                ToggleChildAgents(true);
                isDragging = false;
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

    // 주민 AI 에이전트 활성/비활성화 제어 메서드
    // 축소 모드(!isExpanded)일 경우, 드래그 종료 요청이 와도 Agent를 켜지 않습니다.

    private void ToggleChildAgents(bool enable)
    {
        // 현재 게임 모드가 확장 모드인지 확인
        bool isExpanded = gameManager == null || gameManager.GetIsExpanded();

        // 최종 판단: 드래그가 끝났더라도(!enable == false), '축소 모드(!isExpanded)' 상태라면 계속 꺼져 있어야 함!
        bool finalEnableState = enable && isExpanded;

        NavMeshAgent[] agents = GetComponentsInChildren<NavMeshAgent>(true);
        foreach (var agent in agents)
        {
            if (agent == null) continue;

            if (!finalEnableState)
            {
                agent.enabled = false;

                // 애니메이션 가두기
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