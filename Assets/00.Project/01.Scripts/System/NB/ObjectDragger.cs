using UnityEngine;
using UnityEngine.AI;

public class ObjectDragger : MonoBehaviour
{
    private Vector3 offset;
    private bool isDragging = false;
    private GameMasterManager gameManager;
    private Camera _mainCamera; // 메인 카메라 캐싱 변수 추가

    [Header("관성(부드러움) 설정")]
    [Range(0.01f, 0.5f)]
    public float smoothTime = 0f; // 수치가 작을수록 마우스에 딱 붙고, 클수록 묵직하게 늦게 따라옴 (기본값 0.1 추천)
    private Vector3 dragVelocity = Vector3.zero; // SmoothDamp 내부 물리 계산용 속도 변수


    void Awake()
    {
        _mainCamera = Camera.main; // 매 프레임 Camera.main 호출을 막기 위한 캐싱
    }

    void Start()
    {
        gameManager = Object.FindFirstObjectByType<GameMasterManager>();
    }
    void Update()
    {
        if (gameManager != null && !gameManager.GetIsExpanded())
        {
            if (isDragging) ToggleChildAgents(true);
            isDragging = false;
            return;
        }
        // 마우스 왼쪽 클릭 시 (드래그 시작)
        if (Input.GetMouseButtonDown(0))
        {
            // 커서가 UI 위에 있다면 섬 드래그 동작을 원천 차단
            if (InputGuard.IsPointerOverUI())
            {
                return;
            }

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.transform == this.transform)
                {
                    isDragging = true;

                    // $z = 10$ 깊이값을 기준으로 마우스 월드 좌표 오프셋 계산
                    Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(
                        new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10f));

                    offset = gameObject.transform.position - mouseWorldPos;

                    // 드래그를 새로 시작할 때 기존 관성 속도 초기화
                    dragVelocity = Vector3.zero;
                    ToggleChildAgents(false);
                }
            }
        }
        // 드래그 중
        if (isDragging && Input.GetMouseButton(0))
        {
            // 마우스가 도달해야 할 최종 '목표 위치'를 먼저 계산
            Vector3 targetPos = Camera.main.ScreenToWorldPoint(
                new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10)) + offset;

            // 바로 위치를 대입하지 않고, smoothTime초에 걸쳐 부드럽게 감속하며 추적
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref dragVelocity, smoothTime);
        }
        else
        {
            if (isDragging)
            {
                ToggleChildAgents(true);
                isDragging = false;
            }
        }
    }
    private void ToggleChildAgents(bool enable)
    {
        NavMeshAgent[] agents = GetComponentsInChildren<NavMeshAgent>(true);
        foreach (var agent in agents)
        {
            if (agent == null) continue;

            if (!enable)
            {
                agent.enabled = false;
                Animator anim = agent.GetComponent<Animator>();
                if (anim != null) anim.SetBool("isWalking", false);
            }
            else
            {
                agent.enabled = true;
            }
        }
    }
}
