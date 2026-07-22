using System;
using UnityEngine;

public class TargetSelector : MonoBehaviour
{
    // 타겟이 선택되면 이 이벤트를 구독한 모든 객체(카메라 등)에게 알림 보내기
    public static event Action<Transform> OnTargetSelected;

    // 가비지 컬렉션 방지를 위해 메인 카메라를 캐싱
    private Camera _mainCamera;

    // 레이캐스트 성능 최적화를 위한 레이어 마스크 설정
    [SerializeField] private LayerMask selectableLayer;

    private void Awake()
    {
        _mainCamera = Camera.main; // Camera.main 호출은 Awake에서 한 번만!
    }

    private void Update()
    {
        // 마우스 왼쪽 버튼 클릭 감지
        if (Input.GetMouseButtonDown(0))
        {
            // 커서가 UI 위에 있다면 3D 동물 선택 로직을 즉시 중단
            if (InputGuard.IsPointerOverUI())
            {
                return;
            }
            SelectTarget();
        }
    }

    private void SelectTarget()
    {
        // 스크린의 마우스 위치에서 월드 공간으로 향하는 광선(Ray) 생성
        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

        // 클릭 단발성이므로 일반 Raycast >> RaycastNonAlloc을 쓰면 더 좋다고 함(나중에 찾아보기)
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, selectableLayer))
        {
            // 타겟이 맞았다면 이벤트 발생 (null 체크 후 Invoke)
            OnTargetSelected?.Invoke(hit.transform);
            Debug.Log($"[TargetSelector] 타겟 선택됨: {hit.transform.name}");
        }
        else
        {
            // 빈 공간 클릭 시 타겟 해제 (선택적 구현)
            OnTargetSelected?.Invoke(null);
        }
    }
}