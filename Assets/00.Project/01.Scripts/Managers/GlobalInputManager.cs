using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class GlobalInputManager : MonoBehaviour
{
    private EarnProcessor earnProcessor;

    private void Start()
    {
        earnProcessor = EarnProcessor.Instance;
    }

    private void Update()
    {
        // 1. 마우스 왼쪽 클릭 감지 (화면 아무 데나 눌러도 작동)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            // [선택 사항] 만약 나중에 게임에 상점 버튼 등이 추가되었을 때, 
            // 버튼을 누르는 클릭은 '빈 곳 클릭'에서 제외하고 싶다면 아래 조건문을 사용합니다.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                // UIController_AnimalInvPage 요소를 클릭한 경우이므로 빈 곳 클릭 처리 안 함
                return;
            }

            earnProcessor.ProcessGlobalClick();
        }

        // 2. 키보드 타이핑 감지 (빈 상태에서 키보드를 쳐도 실시간 입력 문자열을 가져옴)
        if (Keyboard.current == null) return;
        foreach (var key in Keyboard.current.allKeys)
        {
            if (key != null && key.wasPressedThisFrame)
            {
                earnProcessor.ProcessGlobalTyping();
                break; // 같은 프레임에 여러 키가 눌려도 한 번만 처리
            }
        }
    }
}
