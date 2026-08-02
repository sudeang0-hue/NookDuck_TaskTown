using UnityEngine;

/// <summary>
/// 동물의 마우스 호버 이벤트를 감지하고 CameraDirector 포커스 상태에 따라 돋보기 아이콘을 제어합니다.
/// </summary>
public class AnimalHoverHandler : MonoBehaviour
{
    [Header("머리 위 돋보기 아이콘 오브젝트")]
    [SerializeField] private GameObject magnifierIcon;

    private void Awake()
    {
        // 돋보기 아이콘 참조가 비어있다면 자식에서 탐색
        if (magnifierIcon == null)
        {
            Transform foundInstance = transform.Find("MagnifierIcon");
            if (foundInstance != null) magnifierIcon = foundInstance.gameObject;
        }
    }

    private void OnEnable()
    {
        // CameraDirector 이벤트 구독
        CameraDirector.OnCameraFocusStarted += DisableHoverIcon;
        CameraDirector.OnCameraFocusEnded += HandleFocusEnded;
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제 (메모리 누수 방지)
        CameraDirector.OnCameraFocusStarted -= DisableHoverIcon;
        CameraDirector.OnCameraFocusEnded -= HandleFocusEnded;
    }

    private void OnMouseEnter()
    {
        // 카메라가 이미 동물을 추적(포커스) 중이면 돋보기 노출 차단!
        if (CameraDirector.Instance != null && CameraDirector.Instance.IsFocused)
        {
            return;
        }

        SetMagnifierActive(true);
    }

    private void OnMouseOver()
    {
        // 추적 중으로 전환되었을 때 마우스 잔상 제거 2차 방어
        if (CameraDirector.Instance != null && CameraDirector.Instance.IsFocused)
        {
            if (magnifierIcon != null && magnifierIcon.activeSelf)
            {
                SetMagnifierActive(false);
            }
        }
    }

    private void OnMouseExit()
    {
        SetMagnifierActive(false);
    }

    private void DisableHoverIcon()
    {
        SetMagnifierActive(false);
    }

    private void HandleFocusEnded()
    {
        SetMagnifierActive(false);
    }

    private void SetMagnifierActive(bool active)
    {
        if (magnifierIcon != null)
        {
            magnifierIcon.SetActive(active);
        }
    }
}