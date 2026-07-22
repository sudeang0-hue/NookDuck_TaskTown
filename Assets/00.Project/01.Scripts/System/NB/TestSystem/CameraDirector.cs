//NB

using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.UI;
using System;

// 카메라의 모든 이동, 줌, 추적 연출을 전담하는 싱글톤 디렉터
public class CameraDirector : MonoBehaviour
{
    // 싱글톤
    public static CameraDirector Instance { get; private set; }

    //카메라가 팔로우 모드에 진입할 때 발생하는 이벤트
    public static event Action OnCameraFocusStarted;


    [Header("카메라 기본 설정")]
    private Camera _mainCamera;
    private Vector3 _camOriginalPos;
    private float _camOriginalSize;

    [Header("팔로우 카메라 설정")]
    [SerializeField] private float zoomInSize = 2.5f;
    [SerializeField] private float followSmoothTime = 0.3f;
    [SerializeField] private Vector3 followOffset = Vector3.zero;
    [SerializeField] private Button btnUnfocus;

    [Header("축소 화면 카메라 설정")]
    [SerializeField] private Transform miniVillagePos;
    private Vector3 _originalVillagePos; // 마을 원본 좌표 캐싱용

    // 상태 변수들
    private Transform _targetAnimal;
    private bool _isFollowing;
    private Vector3 _camVelocity = Vector3.zero;
    private bool _isExpanded = true; // 현재 화면(UI) 상태


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject); // 중복 방지

        _mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        TargetSelector.OnTargetSelected += HandleTargetSelection;
    }

    private void OnDisable()
    {
        TargetSelector.OnTargetSelected -= HandleTargetSelection;
    }

    private void HandleTargetSelection(Transform target)
    {
        //섬이 축소일때 클릭하더라도 카메라가 반응하지않게 차단
        if (!_isExpanded) return;

        if (target != null)
        {
            FocusOnAnimal(target);
        }
        else
        {
            UnfocusAnimal(); // 빈공간 클릭시 타켓 해제
        }
    }

    private void Start()
    {
        _mainCamera = Camera.main;
        if (_mainCamera != null)
        {
            _camOriginalPos = _mainCamera.transform.position;
            _camOriginalSize = _mainCamera.orthographicSize;
        }

        if (btnUnfocus != null)
        {
            btnUnfocus.onClick.AddListener(UnfocusAnimal);
            btnUnfocus.gameObject.SetActive(false);
        }
    }

    // GameMasterManager에서 초기 마을 좌표를 받아오는 메서드
    public void SetupVillageOrigin(Vector3 originPos)
    {
        _originalVillagePos = originPos;
    }

    private void LateUpdate()
    {
        if (_isFollowing && _targetAnimal != null)
        {

            // 2.5D/Isometric 카메라의 각도와 오프셋을 유지하는 상대 거리 계산식
            Vector3 targetWorldPos = GetTargetPosition();
            Vector3 targetPos = targetWorldPos + (_camOriginalPos - _originalVillagePos) + followOffset;

            //시네머신처럼 움직임을 위한 SmoothDamp추적 로직
            _mainCamera.transform.position = Vector3.SmoothDamp(
                _mainCamera.transform.position, targetPos, ref _camVelocity, followSmoothTime
            );
        }
    }

    //타켓의 실제 중심 좌표를 안전하게 계산하는 핼퍼메서드
    private Vector3 GetTargetPosition()
    {
        if (_targetAnimal == null) return Vector3.zero;

        //타겟에 Collider나 Renderer가 있다면 그 중심점(bounds.center)을 활용
        if (_targetAnimal.TryGetComponent<Collider>(out var collider))
        {
            return collider.bounds.center;
        }
        else if (_targetAnimal.TryGetComponent<Renderer>(out var renderer))
        {
            return renderer.bounds.center;
        }

        return _targetAnimal.position;
    }


    public void FocusOnAnimal(Transform animalTransform)
    {
        if (!_isExpanded || _mainCamera == null) return;

        //동물을 쫒기 시작하면 열려있는 모든 UI를 닫도록 이벤트 알림
        OnCameraFocusStarted?.Invoke();

        _targetAnimal = animalTransform;

        // [디버깅용] 콘솔창에 찍히는 좌표를 꼭 확인, 만약 (0,0,0) 이라면 TargetSelector가 부모의 Transform을 넘기고 있는 것
        Debug.Log($"[CameraDirector] 타겟 지정됨: {_targetAnimal.name} / 월드 좌표: {GetTargetPosition()}");

        _mainCamera.DOKill();
        _isFollowing = false;        //LateUpdate의 충돌을 막기 위해 추적을 잠시 끔
        _camVelocity = Vector3.zero; //이전 속도 누적값 초기화

        //줌 크기 변경
        _mainCamera.DOOrthoSize(zoomInSize, 0.6f).SetEase(Ease.OutCubic);

        //이동 목표 좌표 계산 시 카메라와 마을 원본 간의 상대 거리 벡터를 그대로 적용
        Vector3 targetWorldPos = GetTargetPosition();
        Vector3 targetPos = targetWorldPos + (_camOriginalPos - _originalVillagePos) + followOffset;

        _mainCamera.transform.DOMove(targetPos, 0.5f).SetEase(Ease.OutCubic).OnComplete(() =>
        {            
            _isFollowing = true;    //DOTween 연출 완료 후 추적을 활성화
        });

        if (btnUnfocus != null) btnUnfocus.gameObject.SetActive(true);
    }

    public void UnfocusAnimal()
    {

        // 이미 축소 화면 상태면 Unfocus 연출을 실행하지 않기
        if (!_isExpanded) return;

        _isFollowing = false;
        _targetAnimal = null;
        _camVelocity = Vector3.zero;

        if (_mainCamera == null) return;

        _mainCamera.DOKill();
        _mainCamera.DOOrthoSize(_camOriginalSize, 0.5f).SetEase(Ease.InOutQuad);
        _mainCamera.transform.DOMove(_camOriginalPos, 0.5f).SetEase(Ease.InOutQuad);

        if (btnUnfocus != null) btnUnfocus.gameObject.SetActive(false);
    }

    // 축소/확대 뷰 상태 제어용 메서드
    public void SetMinimizedView()
    {
        UnfocusAnimal();
        _isExpanded = false;

        if (_mainCamera != null && miniVillagePos != null)
        {
            float targetSize = _camOriginalSize / 0.3f;
            _mainCamera.DOOrthoSize(targetSize, 0.5f).SetEase(Ease.InOutQuad);

            Vector3 targetCamPos = miniVillagePos.position + (_camOriginalPos - _originalVillagePos);
            _mainCamera.transform.DOMove(targetCamPos, 0.5f).SetEase(Ease.InOutQuad);
        }
    }

    public void SetExpandedView()
    {
        _isExpanded = true;
        if (_mainCamera != null)
        {
            _mainCamera.DOOrthoSize(_camOriginalSize, 0.5f).SetEase(Ease.InOutQuad);
            _mainCamera.transform.DOMove(_camOriginalPos, 0.5f).SetEase(Ease.InOutQuad);
        }
    }

    // [시각적 디버깅] 카메라가 누구를 쫓고 있는지 에디터에서 선으로 표시
    private void OnDrawGizmos()
    {
        if (_isFollowing && _targetAnimal != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 targetPos = new Vector3(_targetAnimal.position.x, _targetAnimal.position.y, transform.position.z) + followOffset;
            Gizmos.DrawWireSphere(targetPos, 0.3f);
            Gizmos.DrawLine(transform.position, targetPos);
        }
    }
}