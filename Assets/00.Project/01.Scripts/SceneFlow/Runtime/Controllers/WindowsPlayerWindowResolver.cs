using System;
using System.Runtime.InteropServices;
using System.Text;

namespace TaskTown.SceneFlow
{
    /// <summary>
    /// 해상도 변경 중 교체될 수 있는 Unity Player의 실제 표시 창을 찾습니다.
    /// 창 크기 제어와 투명 창 처리가 같은 HWND를 사용하도록 별도 클래스로 분리했습니다.
    /// </summary>
    internal static class WindowsPlayerWindowResolver
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private const uint GwOwner = 4;
        private const string UnityWindowClassName = "UnityWndClass";
        private const int ClassNameCapacity = 256;

        private static readonly uint CurrentProcessId = unchecked(
            (uint)System.Diagnostics.Process.GetCurrentProcess().Id);

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        private delegate bool EnumWindowsCallback(IntPtr windowHandle, IntPtr parameter);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(EnumWindowsCallback callback, IntPtr parameter);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindow(IntPtr windowHandle);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr windowHandle);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr windowHandle, uint command);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(
            IntPtr windowHandle,
            out uint processId);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(
            IntPtr windowHandle,
            StringBuilder className,
            int maxCount);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetClientRect(
            IntPtr windowHandle,
            out NativeRect clientRect);

        public static bool TryGetVisiblePlayerWindow(
            out IntPtr windowHandle,
            int expectedClientWidth = 0,
            int expectedClientHeight = 0)
        {
            IntPtr bestWindow = IntPtr.Zero;
            long bestScore = long.MinValue;

            EnumWindows((candidate, _) =>
            {
                if (!TryEvaluateCandidate(
                        candidate,
                        expectedClientWidth,
                        expectedClientHeight,
                        out long score))
                {
                    return true;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestWindow = candidate;
                }

                return true;
            }, IntPtr.Zero);

            windowHandle = bestWindow;
            return windowHandle != IntPtr.Zero;
        }

        public static bool IsVisiblePlayerWindow(IntPtr windowHandle)
        {
            return TryEvaluateCandidate(windowHandle, 0, 0, out _);
        }

        private static bool TryEvaluateCandidate(
            IntPtr windowHandle,
            int expectedClientWidth,
            int expectedClientHeight,
            out long score)
        {
            score = long.MinValue;

            if (windowHandle == IntPtr.Zero ||
                !IsWindow(windowHandle) ||
                !IsWindowVisible(windowHandle) ||
                GetWindow(windowHandle, GwOwner) != IntPtr.Zero)
            {
                return false;
            }

            GetWindowThreadProcessId(windowHandle, out uint ownerProcessId);
            if (ownerProcessId != CurrentProcessId ||
                !GetClientRect(windowHandle, out NativeRect clientRect))
            {
                return false;
            }

            int clientWidth = clientRect.Right - clientRect.Left;
            int clientHeight = clientRect.Bottom - clientRect.Top;
            if (clientWidth <= 0 || clientHeight <= 0)
            {
                return false;
            }

            StringBuilder className = new(ClassNameCapacity);
            GetClassName(windowHandle, className, className.Capacity);
            bool isUnityWindow = string.Equals(
                className.ToString(),
                UnityWindowClassName,
                StringComparison.Ordinal);
            if (!isUnityWindow)
            {
                return false;
            }

            long sizeDifference = 0;
            if (expectedClientWidth > 0 && expectedClientHeight > 0)
            {
                sizeDifference =
                    Math.Abs((long)clientWidth - expectedClientWidth) +
                    Math.Abs((long)clientHeight - expectedClientHeight);
            }

            long clientArea = (long)clientWidth * clientHeight;
            score = -sizeDifference * 100_000_000L +
                    Math.Min(clientArea, 99_999_999L);
            return true;
        }
#endif
    }
}
