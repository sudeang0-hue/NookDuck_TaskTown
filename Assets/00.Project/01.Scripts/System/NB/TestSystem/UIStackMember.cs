//NB

using UnityEngine;

// 팀원들의 기존 코드를 전혀 수정하지 않고, GameObject의 활성화 상태만 감지하는 클래스
public class UIStackMember : MonoBehaviour
{
    [Header("UI 설정")]
    [Tooltip("ESC 키로 닫힐 때 완전히 끄지 않고 숨기기만 할지 여부")]
    [SerializeField] private bool _deactivateOnClose = true;

    private void OnEnable()
    {
        // UI가 화면에 켜지는 순간, 중앙 매니저 스택에 자기 자신을 등록
        if (GlobalUIStackManager.Instance != null)
        {
            GlobalUIStackManager.Instance.RegisterUI(this);
        }
    }

    private void OnDisable()
    {
        // Instance 접근 시 null이 반환될 수 있으므로 안전하게 호출
        var manager = GlobalUIStackManager.Instance;
        if (manager != null)
        {
            manager.UnregisterUI(this);
        }
    }

    // ESC 키 입력 시 중앙 매니저가 호출하는 UI 닫기 메서드
    public void CloseSelf()
    {
        if (_deactivateOnClose)
        {
            gameObject.SetActive(false);
        }
    }
}