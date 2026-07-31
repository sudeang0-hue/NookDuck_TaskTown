//NB

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

// UI와 3D 월드 간의 입력 충돌을 지능적으로 방지하는 스마트 입력 가드 유틸리티
public static class InputGuard
{
    private static PointerEventData _pointerEventData;
    private static List<RaycastResult> _raycastResults = new List<RaycastResult>();

    // 현재 마우스 커서가 '실제 상호작용 가능한 UI(버튼, 슬라이더, 드래그 패널 등)' 위에 있는지 검사
    public static bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

        // 마우스 포인터 이벤트 데이터 생성
        _pointerEventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        _raycastResults.Clear();
        EventSystem.current.RaycastAll(_pointerEventData, _raycastResults);

        // 레이캐스트에 감지된 모든 UI 요소 탐색
        foreach (var result in _raycastResults)
        {
            GameObject hitObject = result.gameObject;

            // 감지된 UI가 '실제 상호작용 가능한 UI'인지 검증
            if (IsInteractiveUI(hitObject))
            {
                // 디버깅용 로그
                Debug.Log($"[InputGuard] 3D 클릭 차단 - 원인 UI: {hitObject.name}");
                return true; // 차단 실행
            }
        }

        return false; // 단순 배경이나 허공이면 3D 클릭 허용!
    }

    // 해당 UI 오브젝트 또는 부모가 실제 클릭/드래그 이벤트를 받는 인터랙티브 요소인지 판별
    private static bool IsInteractiveUI(GameObject obj)
    {
        if (obj == null) return false;

        // 1. 유니티 UI 컴포넌트(Button, Toggle, Slider, ScrollRect, InputField 등)가 존재하는지 확인
        if (obj.GetComponentInParent<Selectable>() != null) return true;

        // 2. 드래그 가능한 UI 창(ScrollRect, 커스텀 UI 드래그 스크립트 등)
        if (obj.GetComponentInParent<ScrollRect>() != null) return true;

        // 3. EventSystem의 클릭/드래그 인터페이스 핸들러를 구현한 스크립트 확인
        if (ExecuteEvents.GetEventHandler<IPointerClickHandler>(obj) != null) return true;
        if (ExecuteEvents.GetEventHandler<IDragHandler>(obj) != null) return true;

        // 4. 위의 어디에도 해당하지 않는 단순 배경 Image/Panel은 통과
        return false;
    }
}