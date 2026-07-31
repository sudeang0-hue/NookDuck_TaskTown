//NB

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;

public enum MouseClickType
{
    Left,
    Right
}

/// Windows OS 레벨의 Low-Level 마우스 후킹을 통해 비포커스 상태에서도 클릭을 감지하는 클래스
public class GlobalMouseHook : MonoBehaviour
{
    #region Win32 API Definitions
    private const int WH_MOUSE_LL = 14;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_RBUTTONDOWN = 0x0204;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);
    #endregion

    // 멀티스레드 환경에서 안전한 락프리 큐
    private static readonly ConcurrentQueue<MouseClickType> mouseInputQueue = new ConcurrentQueue<MouseClickType>();

    // GC 수거 방지를 위한 대리자 참조 유지
    private LowLevelMouseProc proc;
    private IntPtr hookId = IntPtr.Zero;

    // 마우스 클릭 시 유니티 메인 스레드에서 발행되는 이벤트 (클릭 타입 전달)
    public static event Action<MouseClickType> OnGlobalMouseClicked;

    private void OnEnable()
    {
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
        proc = HookCallback;
        hookId = SetHook(proc);
#endif
    }

    private void OnDisable()
    {
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
        UnhookWindowsHookEx(hookId);
#endif
    }

    private void OnApplicationQuit()
    {
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
        UnhookWindowsHookEx(hookId);
#endif
    }

    private void Update()
    {
        // OS 스레드에서 들어온 마우스 클릭 신호를 유니티 메인 스레드에서 안전하게 처리
        while (mouseInputQueue.TryDequeue(out MouseClickType clickType))
        {
            OnGlobalMouseClicked?.Invoke(clickType);
        }
    }

    private IntPtr SetHook(LowLevelMouseProc proc)
    {
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule)
        {
            return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    // OS 스레드에서 호출되는 마우스 콜백 (반드시 $O(1)$ 연산으로 극도로 빠르게 반환해야 마우스 렉이 없음)
    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int message = wParam.ToInt32();

            if (message == WM_LBUTTONDOWN)
            {
                mouseInputQueue.Enqueue(MouseClickType.Left);
            }
            else if (message == WM_RBUTTONDOWN)
            {
                mouseInputQueue.Enqueue(MouseClickType.Right);
            }
        }

        return CallNextHookEx(hookId, nCode, wParam, lParam);
    }
}