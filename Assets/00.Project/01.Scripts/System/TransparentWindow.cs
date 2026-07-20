//NB

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;

public class TransparentWindow : MonoBehaviour
{
    private struct MARGINS { public int cxLeftWidth; public int cxRightWidth; public int cyTopHeight; public int cyBottomHeight; }

    private const int GWL_EXSTYLE = -20;
    private const uint WS_EX_LAYERED = 0x00080000;
    private const uint WS_EX_TRANSPARENT = 0x00000020;
    private const uint WS_EX_TOPMOST = 0x00000008;

    [DllImport("user32.dll")] private static extern IntPtr GetActiveWindow();
    [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);
    [DllImport("user32.dll")] private static extern uint GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")] private static extern int SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    [DllImport("Dwmapi.dll")] private static extern uint DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS margins);

    private IntPtr hWnd;
    private Camera mainCam;

    private void Start()
    {
        mainCam = Camera.main;
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
        hWnd = GetActiveWindow();

        MARGINS margins = new MARGINS { cxLeftWidth = -1 };
        DwmExtendFrameIntoClientArea(hWnd, ref margins);
        
        SetWindowLong(hWnd, -16, 0x80000000 | 0x10000000); 
        SetWindowPos(hWnd, new IntPtr(-1), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0020);
#endif
    }

    private void Update()
    {
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
        // [수정된 부분] 매 프레임 UI와 3D 오브젝트를 확인하여 창 상태를 결정합니다.
        HandleClickThrough();
#endif
    }

    private void HandleClickThrough()
    {
        // 1. UIController_AnimalInvPage 또는 3D 오브젝트 위에 마우스가 있는지 확인
        bool isOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        bool isOver3D = false;

        if (mainCam != null)
        {
            Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
            // 광산 오브젝트에 Collider가 필수입니다.
            if (Physics.Raycast(ray))
            {
                isOver3D = true;
            }
        }

        uint currentStyle = GetWindowLong(hWnd, GWL_EXSTYLE);

        // 2. 게임 요소 위에 있으면: 투명 관통 속성(TRANSPARENT)을 끕니다. (클릭 가능)
        if (isOverUI || isOver3D)
        {
            if ((currentStyle & WS_EX_TRANSPARENT) != 0)
            {
                SetWindowLong(hWnd, GWL_EXSTYLE, currentStyle & ~WS_EX_TRANSPARENT);
            }
        }
        // 3. 허공에 있으면: 투명 관통 속성(TRANSPARENT)을 켭니다. (바탕화면 클릭 통과)
        else
        {
            if ((currentStyle & WS_EX_TRANSPARENT) == 0)
            {
                SetWindowLong(hWnd, GWL_EXSTYLE, currentStyle | WS_EX_TRANSPARENT);
            }
        }
    }
}