//NB

using UnityEngine;

// 아이콘이 항상 메인 카메라를 바라보게 만들고, 둥실둥실 떠다니는 애니메이션을 부여하는 컴포넌트
public class BillboardIcon : MonoBehaviour
{
    [Header("Hover Animation Settings")]
    [Tooltip("상하 흔들림 진폭")]
    [SerializeField] private float amplitude = 0.1f;
    [Tooltip("상하 흔들림 속도")]
    [SerializeField] private float frequency = 3.0f;

    private Transform mainCameraTransform;
    private Vector3 initialLocalPosition;

    private void Awake()
    {
        // Update에서 Camera.main을 매 프레임 호출하면 GC/Find overhead가 발생하므로 캐싱
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }

        initialLocalPosition = transform.localPosition;
    }

    private void OnEnable()
    {
        // 아이콘이 켜질 때 위치 초기화
        transform.localPosition = initialLocalPosition;
    }

    private void Update()
    {
        if (mainCameraTransform == null) return;

        // 1. 빌보드 처리: 메인 카메라의 회전값과 동일하게 맞춰 화면을 정면으로 바라보게 함
        transform.rotation = mainCameraTransform.rotation;

        // 2. Sin 함수를 이용한 둥실둥실 부유 효과 계산
        float newY = initialLocalPosition.y + Mathf.Sin(Time.time * frequency) * amplitude;
        transform.localPosition = new Vector3(initialLocalPosition.x, newY, initialLocalPosition.z);
    }
}