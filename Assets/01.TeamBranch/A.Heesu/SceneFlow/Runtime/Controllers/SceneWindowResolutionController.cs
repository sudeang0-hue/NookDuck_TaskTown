using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

namespace TaskTown.SceneFlow
{
    /// <summary>
    /// Scene 진입 시 Windows Player 창의 해상도를 지정합니다.
    /// 창 테두리와 투명 처리는 테스트용 TransparentWindowFlowTest가 담당합니다.
    /// 팀원의 원본 TransparentWindow는 수정하지 않습니다.
    /// </summary>
    [DefaultExecutionOrder(-20000)]
    public sealed class SceneWindowResolutionController : MonoBehaviour
    {
        private const float WindowResolveTimeoutSeconds = 3f;
        private const int WindowFrameSizeTolerance = 64;

        public static event Action ResolutionApplied;

        [Header("Window Resolution")]
        [SerializeField] private bool useCurrentDisplayResolution;
        [SerializeField] private Vector2Int windowSize = new(1000, 1000);
        [SerializeField] private bool centerOnCurrentMonitor;
        [SerializeField] private bool applyInEditor;

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private const uint MonitorDefaultToNearest = 0x00000002;
        private const uint SwpNoSize = 0x0001;
        private const uint SwpNoZOrder = 0x0004;
        private const uint SwpNoActivate = 0x0010;

        private Coroutine positionRoutine;

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MonitorInfo
        {
            public uint Size;
            public NativeRect Monitor;
            public NativeRect WorkArea;
            public uint Flags;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr windowHandle, uint flags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr monitorHandle, ref MonitorInfo monitorInfo);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr windowHandle, out NativeRect windowRect);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(
            IntPtr windowHandle,
            IntPtr insertAfter,
            int x,
            int y,
            int width,
            int height,
            uint flags);
#endif

        private void Awake()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            ApplyResolution();
#elif UNITY_EDITOR
            if (applyInEditor)
            {
                ApplyResolution();
            }
#endif
        }

        public void ApplyResolution()
        {
            int width = useCurrentDisplayResolution
                ? Display.main.systemWidth
                : windowSize.x;
            int height = useCurrentDisplayResolution
                ? Display.main.systemHeight
                : windowSize.y;

            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);

            Screen.SetResolution(width, height, FullScreenMode.Windowed);

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if (positionRoutine != null)
            {
                StopCoroutine(positionRoutine);
            }

            positionRoutine = StartCoroutine(
                FinalizeWindowAfterResolution(
                    width,
                    height,
                    centerOnCurrentMonitor || useCurrentDisplayResolution,
                    centerOnCurrentMonitor));
#elif UNITY_EDITOR
            ResolutionApplied?.Invoke();
#endif
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private IEnumerator FinalizeWindowAfterResolution(
            int targetWidth,
            int targetHeight,
            bool shouldPositionWindow,
            bool centerWindow)
        {
            float timeoutAt = Time.realtimeSinceStartup + WindowResolveTimeoutSeconds;
            bool windowResolved = false;

            while (Time.realtimeSinceStartup < timeoutAt)
            {
                yield return null;

                bool resolutionApplied =
                    Mathf.Abs(Screen.width - targetWidth) <= WindowFrameSizeTolerance &&
                    Mathf.Abs(Screen.height - targetHeight) <= WindowFrameSizeTolerance;

                if (shouldPositionWindow)
                {
                    bool windowPositioned = PositionPlayerWindow(
                        centerWindow,
                        targetWidth,
                        targetHeight,
                        false);

                    // 전체 모니터 크기는 네이티브 SetWindowPos로 먼저 확정할 수 있습니다.
                    // 중앙 배치는 요청한 Windowed 해상도가 실제 적용된 뒤 위치를 계산합니다.
                    if (windowPositioned && (!centerWindow || resolutionApplied))
                    {
                        windowResolved = true;
                        break;
                    }

                    continue;
                }

                if (resolutionApplied)
                {
                    windowResolved = true;
                    break;
                }
            }

            if (!windowResolved)
            {
                Debug.LogWarning(
                    $"[SceneWindowResolutionController] 제한 시간 안에 표시 중인 Player 창에 해상도와 위치를 적용하지 못했습니다. " +
                    $"Target={targetWidth}x{targetHeight}, Actual={Screen.width}x{Screen.height}",
                    this);
            }

            positionRoutine = null;
            ResolutionApplied?.Invoke();
        }

        private bool PositionPlayerWindow(
            bool centerWindow,
            int expectedClientWidth,
            int expectedClientHeight,
            bool logFailure)
        {
            if (!WindowsPlayerWindowResolver.TryGetVisiblePlayerWindow(
                    out IntPtr windowHandle,
                    expectedClientWidth,
                    expectedClientHeight))
            {
                if (logFailure)
                {
                    Debug.LogWarning(
                        "[SceneWindowResolutionController] 표시 중인 Player 창 핸들을 찾지 못했습니다.",
                        this);
                }

                return false;
            }

            IntPtr monitorHandle = MonitorFromWindow(windowHandle, MonitorDefaultToNearest);
            MonitorInfo monitorInfo = new()
            {
                Size = (uint)Marshal.SizeOf<MonitorInfo>()
            };

            if (monitorHandle == IntPtr.Zero || !GetMonitorInfo(monitorHandle, ref monitorInfo))
            {
                if (logFailure)
                {
                    Debug.LogWarning(
                        "[SceneWindowResolutionController] 모니터 또는 창 영역을 확인하지 못했습니다.",
                        this);
                }

                return false;
            }

            int targetX = monitorInfo.Monitor.Left;
            int targetY = monitorInfo.Monitor.Top;
            int targetWidth = monitorInfo.Monitor.Right - monitorInfo.Monitor.Left;
            int targetHeight = monitorInfo.Monitor.Bottom - monitorInfo.Monitor.Top;
            uint positionFlags = SwpNoZOrder | SwpNoActivate;

            if (centerWindow)
            {
                if (!GetWindowRect(windowHandle, out NativeRect windowRect))
                {
                    if (logFailure)
                    {
                        Debug.LogWarning(
                            "[SceneWindowResolutionController] Player 창 영역을 확인하지 못했습니다.",
                            this);
                    }

                    return false;
                }

                int windowWidth = windowRect.Right - windowRect.Left;
                int windowHeight = windowRect.Bottom - windowRect.Top;
                NativeRect workArea = monitorInfo.WorkArea;
                targetX = workArea.Left + (workArea.Right - workArea.Left - windowWidth) / 2;
                targetY = workArea.Top + (workArea.Bottom - workArea.Top - windowHeight) / 2;
                targetWidth = 0;
                targetHeight = 0;
                positionFlags |= SwpNoSize;
            }

            if (!SetWindowPos(
                    windowHandle,
                    IntPtr.Zero,
                    targetX,
                    targetY,
                    targetWidth,
                    targetHeight,
                    positionFlags))
            {
                if (logFailure)
                {
                    Debug.LogWarning(
                        "[SceneWindowResolutionController] Player 창 위치를 적용하지 못했습니다.",
                        this);
                }

                return false;
            }

            return true;
        }

        private void OnDestroy()
        {
            if (positionRoutine == null)
            {
                return;
            }

            StopCoroutine(positionRoutine);
            positionRoutine = null;
        }
#endif
    }
}
