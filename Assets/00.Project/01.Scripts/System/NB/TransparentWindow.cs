//NB

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;

// 데스크톱 컴패니언 게임을 위한 윈도우 투명화 및 클릭 투과(Click-Through) 제어 클래스
public class TransparentWindow : MonoBehaviour
{
    #region Win32 API Structs & Constants
    [StructLayout(LayoutKind.Sequential)]
    private struct MARGINS
    {
        public int cxLeftWidth;
        public int cxRightWidth;
        public int cyTopHeight;
        public int cyBottomHeight;
    }

    // Window Long Index
    private const int GWL_STYLE = -16;
    private const int GWL_EXSTYLE = -20;

    // Window Styles
    private const uint WS_POPUP = 0x80000000;
    private const uint WS_VISIBLE = 0x10000000;

    // Extended Window Styles
    private const uint WS_EX_TOPMOST = 0x00000008;
    private const uint WS_EX_LAYERED = 0x00080000;     // 투명도 및 레이어드 창 처리에 필수
    private const uint WS_EX_TRANSPARENT = 0x00000020;   // 마우스 클릭 투과 플래그

    // SetWindowPos Flags
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_FRAMECHANGED = 0x0020;       // 스타일 변경 후 창 프레임 즉시 갱신
    private const uint SWP_SHOWWINDOW = 0x0040;

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

    // DPI Awareness Context 값 (Per-Monitor DPI Aware v2)
    private static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);

    #endregion

    #region Win32 API Imports
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

    [DllImport("user32.dll")]
    private static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("Dwmapi.dll")]
    private static extern uint DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS margins);

    // OS 레벨 DPI 인식을 강제 지정하는 Win32 API
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetProcessDpiAwarenessContext(IntPtr dpiContext);
    #endregion

    [Header("Settings")]
    [SerializeField] private LayerMask clickable3DLayers = ~0; // 3D 레이캐스트 대상 레이어

    private IntPtr hWnd;
    private Camera mainCam;
    private bool isClickThrough = false; // 현재 클릭 투과 상태 캐싱
    private PointerEventData cachedPointerEventData;
    private List<RaycastResult> cachedRaycastResults;

    private void Awake()
    {
        mainCam = Camera.main;
        cachedPointerEventData = new PointerEventData(EventSystem.current);
        cachedRaycastResults = new List<RaycastResult>();
    }

    private void Start()
    {
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
        // 1. DWM 연산 전, OS에 Per-Monitor V2 DPI 인식을 최우선으로 선언!
        // 이 코드가 적용되면 OS가 가상 비트맵 스케일링을 건너뛰고 1:1 물리 픽셀을 직접 전달
        try
        {
            SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"DPI Context 설정 중 예외 발생 (구형 Windows OS 환경 가능성): {ex.Message}");
        }

        // 2. 현재 유니티 윈도우 핸들 수집
        hWnd = GetActiveWindow();

        // 3. DWM 프레임 확장 (1:1 물리 픽셀 기준 바탕화면 투명화)
        MARGINS margins = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
        DwmExtendFrameIntoClientArea(hWnd, ref margins);

        // 4. 기본 창 스타일 설정 (테두리 없는 팝업 창)
        SetWindowLong(hWnd, GWL_STYLE, WS_POPUP | WS_VISIBLE);

        // 5. 확장 창 스타일 설정 (TOPMOST + LAYERED 필수 적용)
        uint exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
        SetWindowLong(hWnd, GWL_EXSTYLE, exStyle | WS_EX_TOPMOST | WS_EX_LAYERED);

        // 6. 창 프레임 강제 갱신 (SWP_FRAMECHANGED를 호출하여 DWM 버퍼 재계산 유도)
        SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_FRAMECHANGED | SWP_SHOWWINDOW);
#endif
    }

    private void Update()
    {
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
        UpdateClickThroughState();
#endif
    }

    // 마우스 위치의 UI 및 3D 물체 존재 여부를 파악하여 Win32 API를 효율적으로 호출
    private void UpdateClickThroughState()
    {
        bool isOverGameElement = IsPointerOverUI() || IsPointerOver3D();

        // 상태가 변경되었을 때만 Win32 API를 호출하여 OS 과부하 방지 ($O(1)$ 상태 비교)
        if (isOverGameElement && isClickThrough)
        {
            // 게임 요소 위에 마우스가 있음 -> 클릭 가능 상태로 전환 (TRANSPARENT 해제)
            SetClickThrough(false);
        }
        else if (!isOverGameElement && !isClickThrough)
        {
            // 허공(투명 영역)에 마우스가 있음 -> 클릭 투과 상태로 전환 (TRANSPARENT 설정)
            SetClickThrough(true);
        }
    }

    // UI 요소 위에 마우스가 있는지 검사 (GC Alloc 최적화)
    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

        cachedPointerEventData.position = Input.mousePosition;
        cachedRaycastResults.Clear();
        EventSystem.current.RaycastAll(cachedPointerEventData, cachedRaycastResults);

        return cachedRaycastResults.Count > 0;
    }

    // 3D 오브젝트(마을, 동물 등) 위에 마우스가 있는지 광선 추적 검사
    private bool IsPointerOver3D()
    {
        if (mainCam == null) return false;

        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        // Raycast 거리 제한 및 LayerMask를 통한 연산 최적화
        return Physics.Raycast(ray, float.MaxValue, clickable3DLayers);
    }

    // 윈도우 스타일의 WS_EX_TRANSPARENT 플래그를 토글
    private void SetClickThrough(bool transparent)
    {
        uint currentStyle = GetWindowLong(hWnd, GWL_EXSTYLE);

        if (transparent)
        {
            SetWindowLong(hWnd, GWL_EXSTYLE, currentStyle | WS_EX_TRANSPARENT);
        }
        else
        {
            SetWindowLong(hWnd, GWL_EXSTYLE, currentStyle & ~WS_EX_TRANSPARENT);
        }

        // 플래그 변경 후 FRAMECHANGED 옵션으로 OS에 스타일 변경 고지
        SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_FRAMECHANGED);
        isClickThrough = transparent;
    }
    private void OnDrawGizmos()
    {
        if (mainCam == null) return;

        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        bool hit = Physics.Raycast(ray, out RaycastHit hitInfo, float.MaxValue, clickable3DLayers);

        Gizmos.color = hit ? Color.green : Color.red;
        Gizmos.DrawRay(ray.origin, ray.direction * 100f);

        if (hit)
        {
            Gizmos.DrawWireSphere(hitInfo.point, 0.5f);
        }
    }

}
