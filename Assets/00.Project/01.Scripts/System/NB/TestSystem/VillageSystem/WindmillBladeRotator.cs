using UnityEngine;

/// <summary>
/// Tripo AI 등 외부 에셋의 자식 노드 순서에 맞추어 풍차 날개 회전을 제어하는 컴포넌트입니다.
/// </summary>
public class WindmillBladeRotator : MonoBehaviour
{
    [Header("오브젝트 참조 및 자식 설정")]
    [Tooltip("회전시킬 자식 날개 메쉬의 Transform")]
    [SerializeField] private Transform bladeTransform;

    [Tooltip("날개 오브젝트가 위치한 자식 인덱스 (Tripo 모델은 보통 1번)")]
    [SerializeField] private int targetChildIndex = 1;

    [Header("회전 및 축 설정")]
    [Tooltip("회전축 (로컬 Z축: Vector3.forward, 로컬 X축: Vector3.right)")]
    [SerializeField] private Vector3 rotationAxis = Vector3.forward;

    [Tooltip("기본 각속도 $\\omega_{base}$ (deg/sec)")]
    [SerializeField] private float baseRotationSpeed = 90f;

    [Header("바람 유동 연출 (Juice)")]
    [Tooltip("Perlin Noise 기반 바람 요동 연출 사용 여부")]
    [SerializeField] private bool useWindNoise = true;

    [Tooltip("바람 변화 주기")]
    [SerializeField] private float noiseFrequency = 0.5f;

    private float currentWindMultiplier = 1.0f;

    /// <summary>
    /// 에디터에서 컴포넌트를 붙이거나 Reset할 때 안전하게 지정된 자식 인덱스를 자동 할당합니다.
    /// </summary>
    private void Reset()
    {
        AutoAssignBladeTransform();
    }

    private void Awake()
    {
        // 런타임 시작 시 할당 상태 재검증
        if (bladeTransform == null)
        {
            AutoAssignBladeTransform();
        }
    }

    /// <summary>
    /// 안전 범위 검사($N_{child} > Index_{target}$) 후 지정된 인덱스의 자식을 자동으로 바인딩합니다.
    /// </summary>
    private void AutoAssignBladeTransform()
    {
        int childCount = transform.childCount;

        // 지정된 자식 인덱스가 실제 자식 개수 범위 내에 존재하는지 검증
        if (childCount > targetChildIndex)
        {
            bladeTransform = transform.GetChild(targetChildIndex);
            Debug.Log($"<color=green>[WindmillBladeRotator]</color> '{gameObject.name}'의 {targetChildIndex}번째 자식({bladeTransform.name})을 날개로 자동 지정했습니다.");
        }
        else if (childCount > 0)
        {
            // 예외 처리: 지정 인덱스가 없으면 가장 마지막 자식을 할당
            bladeTransform = transform.GetChild(childCount - 1);
            Debug.LogWarning($"<color=yellow>[WindmillBladeRotator]</color> {targetChildIndex}번 자식이 없어 마지막 자식({bladeTransform.name})을 지정했습니다.");
        }
        else
        {
            bladeTransform = transform;
            Debug.LogError($"<color=red>[WindmillBladeRotator]</color> '{gameObject.name}'에 자식 오브젝트가 존재하지 않습니다.");
        }
    }

    private void Update()
    {
        RotateBlade();
    }

    private void RotateBlade()
    {
        if (bladeTransform == null) return;

        // Perlin Noise 유동 연출: $N(t) = \text{Noise}(t) \cdot 0.4 + 0.8$
        float noise = useWindNoise ? (Mathf.PerlinNoise(Time.time * noiseFrequency, 0f) * 0.4f + 0.8f) : 1.0f;
        float finalSpeed = baseRotationSpeed * currentWindMultiplier * noise;

        // 로컬 좌표계 기준 회전 연산
        bladeTransform.Rotate(rotationAxis.normalized * finalSpeed * Time.deltaTime, Space.Self);
    }

    public void SetWindMultiplier(float multiplier)
    {
        currentWindMultiplier = Mathf.Max(0f, multiplier);
    }

    private void OnDrawGizmosSelected()
    {
        if (bladeTransform == null) return;

        Gizmos.color = Color.cyan;
        Vector3 worldAxis = bladeTransform.TransformDirection(rotationAxis.normalized);
        Gizmos.DrawRay(bladeTransform.position, worldAxis * 1.5f);
        Gizmos.DrawWireSphere(bladeTransform.position, 0.15f);
    }
}