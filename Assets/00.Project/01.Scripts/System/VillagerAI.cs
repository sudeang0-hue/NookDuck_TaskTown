//NB
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
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
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh)
        {
            if (animator != null)
            {
                animator.SetBool("isWalking", false);
            }
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