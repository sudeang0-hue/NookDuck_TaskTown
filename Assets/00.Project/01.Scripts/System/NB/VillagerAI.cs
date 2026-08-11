//NB

using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Collider))]
public class VillagerAI : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;

    [Header("배회 설정")]
    public float wanderRadius = 8f;
    public float minWanderInterval = 4f;
    public float maxWanderInterval = 8f;

    private float wanderTimer;
    private float currentInterval;
    private bool isWaiting = false;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        agent.avoidancePriority = Random.Range(0, 100);
    }
    private void OnEnable()
    {
        // 이벤트 구독
        CameraDirector.OnExpandedStateChanged += HandleExpandedStateChanged;
    }

    private void OnDisable()
    {
        // 메모리 누수 방지를 위한 이벤트 해제
        CameraDirector.OnExpandedStateChanged -= HandleExpandedStateChanged;
    }

    // 축소/확대 상태 변경 시 호출되는 핸들러
    private void HandleExpandedStateChanged(bool isExpanded)
    {
        if (agent == null) return;

        if (!isExpanded)
        {
            // [축소 모드] AI 정지 및 NavMeshAgent 비활성화로 CPU 연산 절감
            if (agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }
            agent.enabled = false;

            if (animator != null)
            {
                animator.SetBool("isWalking", false);
            }
        }
        else
        {
            // [확장 모드 복귀] 섬 위치 이동에 따른 NavMeshAgent 내부 C++ 데이터 강제 동기화

            // 1. 먼저 NavMeshAgent 컴포넌트를 활성화합니다.
            agent.enabled = true;

            // 2. 현재 트랜스폼의 월드 좌표($\vec{P}_{\text{world}}$)를 기준으로 주변 NavMesh 표면을 탐색합니다.
            NavMeshHit hit;
            float searchRadius = 5.0f * transform.lossyScale.x; // 스케일 변화에 대응하는 가변 탐색 반지름 $r$

            if (NavMesh.SamplePosition(transform.position, out hit, searchRadius, NavMesh.AllAreas))
            {
                // [핵심] simple transform 대입이 아닌 agent.Warp()를 호출하여 C++ 공간 인덱스 노드를 강제 재배치합니다.
                agent.Warp(hit.position);
            }
            else
            {
                // 안전장치: 표면을 찾지 못했을 경우 현재 위치로 즉시 Warp 연산 수행
                agent.Warp(transform.position);
            }

            // 3. AI 상태 리셋 및 배회 타이머 재설정
            isWaiting = true;
            wanderTimer = 0f;
            currentInterval = Random.Range(minWanderInterval, maxWanderInterval);
        }
    }

    void Start()
    {
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            if (!SetRandomDestination())
            {
                isWaiting = true;
                currentInterval = 1f;
            }
        }
        else
        {
            isWaiting = true;
            currentInterval = 1f;
        }
    }

    void Update()
    {
        // NavMesh AI 배회 로직
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh)
        {
            if (animator != null) animator.SetBool("isWalking", false);
            return;
        }

        if (isWaiting)
        {
            wanderTimer += Time.deltaTime;
            if (wanderTimer >= currentInterval)
            {
                if (SetRandomDestination())
                {
                    isWaiting = false;
                    wanderTimer = 0f;
                }
                else
                {
                    currentInterval = 1f;
                    wanderTimer = 0f;
                }
            }
        }
        else
        {
            if (!agent.pathPending && (agent.remainingDistance <= agent.stoppingDistance || !agent.hasPath))
            {
                isWaiting = true;
                SetRandomInterval();
                wanderTimer = 0f;
            }
        }

        if (animator != null)
        {
            bool isMoving = agent.velocity.magnitude > 0.1f;
            animator.SetBool("isWalking", isMoving);
        }
    }

    void SetRandomInterval()
    {
        currentInterval = Random.Range(minWanderInterval, maxWanderInterval);
    }

    bool SetRandomDestination()
    {
        float currentScale = transform.lossyScale.x;
        float dynamicRadius = wanderRadius * currentScale;

        Vector3 randomDirection = Random.insideUnitSphere * dynamicRadius;
        randomDirection += transform.position;

        NavMeshHit hit;
        float searchDistance = Mathf.Max(5f * currentScale, 0.5f);

        if (NavMesh.SamplePosition(randomDirection, out hit, searchDistance, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            return true;
        }

        return false;
    }
}