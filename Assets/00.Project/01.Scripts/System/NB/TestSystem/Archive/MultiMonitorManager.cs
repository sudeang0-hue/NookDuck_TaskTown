//NB

using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

// 싱글/다중 모니터 대응 및 Window 좌표 위치 강제 보정 컨트롤러
[RequireComponent(typeof(Camera))]
public class MultiMonitorManager : MonoBehaviour
{
    #region Win32 API
    [StructLayout(LayoutKind.Sequential)]
    private struct MARGINS
    {
        public int cxLeftWidth;
        public int cxRightWidth;
        public int cyTopHeight;
        public int cyBottomHeight;
    }

    private const int GWL_STYLE = -16;
    private const int GWL_EXSTYLE = -20;
    private const uint WS_POPUP = 0x80000000;
    private const uint WS_VISIBLE = 0x10000000;
    private const uint WS_EX_TOPMOST = 0x00000008;
    private const uint WS_EX_LAYERED = 0x00080000;
    private const uint SWP_FRAMECHANGED = 0x0020;
    private const uint SWP_SHOWWINDOW = 0x0040;

    private const int SM_XVIRTUALSCREEN = 76;
    private const int SM_YVIRTUALSCREEN = 77;
    private const int SM_CXVIRTUALSCREEN = 78;
    private const int SM_CYVIRTUALSCREEN = 79;
    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

    [DllImport("user32.dll")] private static extern IntPtr GetActiveWindow();
    [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);
    [DllImport("user32.dll")] private static extern uint GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    [DllImport("Dwmapi.dll")] private static extern uint DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS margins);
    [DllImport("user32.dll")] private static extern int GetSystemMetrics(int nIndex);
    [DllImport("user32.dll")] private static extern bool SetProcessDpiAwarenessContext(IntPtr dpiContext);

    private static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);
    #endregion

    private Camera targetCam;
    private IntPtr hWnd;

    private void Awake()
    {
        targetCam = GetComponent<Camera>();

        // 카메라 배경을 투명 알파 처리
        targetCam.clearFlags = CameraClearFlags.SolidColor;
        targetCam.backgroundColor = new Color(0f, 0f, 0f, 0f);

#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
        try
        {
            // Windows DPI Scale 찌그러짐 방지
            SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"DPI Awareness 설정 실패: {e.Message}");
        }
#endif
    }

    private IEnumerator Start()
    {
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
        // 렌더링 프레임이 완전히 준비될 때까지 1프레임 대기
        yield return new WaitForEndOfFrame();

        hWnd = GetActiveWindow();

        int vLeft = GetSystemMetrics(SM_XVIRTUALSCREEN);
        int vTop = GetSystemMetrics(SM_YVIRTUALSCREEN);
        int vWidth = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        int vHeight = GetSystemMetrics(SM_CYVIRTUALSCREEN);

        int pWidth = GetSystemMetrics(SM_CXSCREEN);
        int pHeight = GetSystemMetrics(SM_CYSCREEN);

        // 가상 화면 너비와 주 모니터 너비 차이가 유의미하지 않다면 싱글 모니터로 정밀 판단
        bool isMultiMonitor = (vWidth > pWidth + 100) || (vHeight > pHeight + 100);

        if (!isMultiMonitor)
        {
            Debug.Log("싱글 모니터 환경: 좌표를 (0,0) 위치로 강제 고정합니다.");
            
            // 뷰포트 영역 전체화면 보정
            targetCam.rect = new Rect(0f, 0f, 1f, 1f);

            // 해상도 고정 및 위치 (0, 0) 설정
            Screen.SetResolution(pWidth, pHeight, FullScreenMode.Windowed);

            ApplyTransparentWindow(hWnd);

            // (0, 0) 즉, 주 모니터 화면 좌상단으로 창을 직접 이동
            SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, pWidth, pHeight, SWP_FRAMECHANGED | SWP_SHOWWINDOW);
        }
        else
        {
            Debug.Log("다중 모니터 환경: 가상 화면 스캔 및 뷰포트 오프셋 계산을 적용합니다.");
            
            Screen.SetResolution(vWidth, vHeight, FullScreenMode.Windowed);
            yield return new WaitForSecondsRealtime(0.05f);

            ApplyTransparentWindow(hWnd);

            SetWindowPos(hWnd, HWND_TOPMOST, vLeft, vTop, vWidth, vHeight, SWP_FRAMECHANGED | SWP_SHOWWINDOW);

            // 뷰포트 비율 계산 ($0.0 \sim 1.0$ 수학적 정규화)
            float rectX = (0f - vLeft) / (float)vWidth;
            float rectY = (vHeight - pHeight - (0 - vTop)) / (float)vHeight;
            float rectW = (float)pWidth / vWidth;
            float rectH = (float)pHeight / vHeight;

            targetCam.rect = new Rect(rectX, rectY, rectW, rectH);
        }
#else
        targetCam.rect = new Rect(0, 0, 1, 1);
        yield break;
#endif
    }

    private void ApplyTransparentWindow(IntPtr handle)
    {
        MARGINS margins = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
        DwmExtendFrameIntoClientArea(handle, ref margins);

        SetWindowLong(handle, GWL_STYLE, WS_POPUP | WS_VISIBLE);
        uint exStyle = GetWindowLong(handle, GWL_EXSTYLE);
        SetWindowLong(handle, GWL_EXSTYLE, exStyle | WS_EX_TOPMOST | WS_EX_LAYERED);
    }
}