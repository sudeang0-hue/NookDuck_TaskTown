//NB

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;

// Windows OS 레벨의 Low-Level 키보드 후킹을 통해  비포커스 상태에서도 키보드 타자를 감지하는 클래스

public class GlobalKeyboardHook : MonoBehaviour
{
    #region Win32 API Definitions
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);
    #endregion

    // 스레드간 안전한 데이터 전달을 위한 락프리 ConcurrentQueue
    private static readonly ConcurrentQueue<bool> keyInputQueue = new ConcurrentQueue<bool>();

    // GC에 의해 대리자(Delegate)가 수거되어 런타임 래퍼 크래시가 발생하는 것을 방지하기 위해 멤버 변수로 유지
    private LowLevelKeyboardProc proc;
    private IntPtr hookId = IntPtr.Zero;

    // 키보드 타자가 발생했을 때 유니티 메인 스레드에서 발행되는 이벤트
    public static event Action OnGlobalKeyPressed;

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
        // OS 스레드에서 들어온 키 입력을 유니티 메인 스레드에서 안전하게 디큐(Dequeue)
        while (keyInputQueue.TryDequeue(out _))
        {
            OnGlobalKeyPressed?.Invoke();
        }
    }

    private IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule)
        {
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    // OS 스레드에서 호출되는 키보드 후킹 콜백 (반드시 최소 연산 $O(1)$ 후 빠르게 반환해야 함)
    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
        {
            // 유니티 API를 직접 부르지 않고 큐에 신호만 삽입
            keyInputQueue.Enqueue(true);
        }

        return CallNextHookEx(hookId, nCode, wParam, lParam);
    }
}