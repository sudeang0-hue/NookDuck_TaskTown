using UnityEngine;

/// <summary>
/// 타겟 전환 시 부드러운 이동과 자동 줌인을 지원하는 실무형 카메라 추적 시스템
/// </summary>
public class FollowCamera : MonoBehaviour
{
    [Header("추적 및 줌 설정")]
    [SerializeField, Tooltip("초기 카메라 오프셋 (바라보는 각도 결정)")]
    private Vector3 initialOffset = new Vector3(0, 5f, -10f);

    [SerializeField, Tooltip("카메라가 타겟 위치에 도달하는 시간 (부드러움의 척도)")]
    private float moveSmoothTime = 0.5f; // 이전보다 약간 늘려 부드러움을 극대화

    [SerializeField, Tooltip("카메라의 최대 이동 속도 제한 (화면 튕김 방지)")]
    private float maxMoveSpeed = 50f;

    [Header("자동 줌인(Zoom) 설정")]
    [SerializeField, Tooltip("새로운 타겟 클릭 시 자동으로 줌인 될 목표 거리")]
    private float autoZoomInDistance = 5f;

    [SerializeField, Tooltip("마우스 휠 감도")]
    private float zoomSpeed = 5f;

    [SerializeField, Tooltip("최대 접근 거리")]
    private float minDistance = 2f;

    [SerializeField, Tooltip("최대 이탈 거리")]
    private float maxDistance = 20f;

    // 내부 상태 변수
    private Transform _currentTarget;
    private Vector3 _moveVelocity = Vector3.zero;
    private float _zoomVelocity = 0f;

    private Vector3 _offsetDirection;
    private float _targetDistance;
    private float _currentDistance;

    private void Awake()
    {
        // 1. 방향 단위 벡터 구하기 (방향은 유지하되 거리만 따로 스케일링하기 위함)
        _offsetDirection = initialOffset.normalized;

        // 초기 거리 설정
        _currentDistance = initialOffset.magnitude;
        _targetDistance = _currentDistance;
    }

    private void OnEnable()
    {
        TargetSelector.OnTargetSelected += SetTarget;
    }

    private void OnDisable()
    {
        TargetSelector.OnTargetSelected -= SetTarget;
    }

    // ★ 핵심 수정부: 타겟이 변경될 때 발생하는 충격(Jitter) 완화 로직
    private void SetTarget(Transform newTarget)
    {
        if (_currentTarget == newTarget) return; // 동일 타겟 클릭 시 무시 (불필요한 연산 방지)

        _currentTarget = newTarget;

        if (_currentTarget != null)
        {
            // 1. 관성 초기화: 이전 타겟을 쫓던 속도를 0으로 만들어 튕김 방지
            _moveVelocity = Vector3.zero;
            _zoomVelocity = 0f;

            // 2. 현재 거리 동기화: 현재 카메라 위치와 새 타겟 사이의 실제 거리를 계산
            // 이를 통해 SmoothDamp가 아주 먼 거리에서부터 자연스럽게 시작하도록 유도
            _currentDistance = Vector3.Distance(transform.position, _currentTarget.position);

            // 3. 목표 거리를 '자동 줌인' 설정값으로 덮어씌움
            _targetDistance = autoZoomInDistance;
        }
    }

    private void Update()
    {
        HandleZoomInput();
    }

    private void LateUpdate()
    {
        if (_currentTarget == null) return;

        // 1. 줌 거리 보간 (수학적 지수 감속 모델 적용)
        _currentDistance = Mathf.SmoothDamp(
            _currentDistance,
            _targetDistance,
            ref _zoomVelocity,
            0.2f // 줌 동작은 이동보다 약간 더 민첩하게 처리
        );

        // 2. 동적 오프셋 계산: (정규화된 방향 벡터 * 현재 보간된 거리)
        Vector3 dynamicOffset = _offsetDirection * _currentDistance;
        Vector3 targetPosition = _currentTarget.position + dynamicOffset;

        // 3. 위치 부드럽게 이동 (maxMoveSpeed를 추가하여 폭주 방지)
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref _moveVelocity,
            moveSmoothTime,
            maxMoveSpeed // ★ 카메라가 낼 수 있는 최고 속도를 제한
        );

        // 4. 회전 보간: 타겟의 중심을 부드럽게 바라보도록 처리
        // 짐벌락(Gimbal Lock) 방지를 위해 Slerp(구면 선형 보간) 사용
        Quaternion targetRotation = Quaternion.LookRotation(_currentTarget.position - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
    }

    private void HandleZoomInput()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            _targetDistance -= scrollInput * zoomSpeed;
            _targetDistance = Mathf.Clamp(_targetDistance, minDistance, maxDistance);
        }
    }
}