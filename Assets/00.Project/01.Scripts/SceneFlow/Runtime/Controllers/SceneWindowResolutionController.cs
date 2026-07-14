using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

namespace TaskTown.SceneFlow
{
    /// <summary>
    /// Scene 진입 시 Windows Player 창의 해상도와 위치를 지정합니다.
    /// 불투명 구간에서는 선택적으로 창 테두리만 제거할 수 있습니다.
    /// Title 이후의 투명 처리와 클릭 관통은 테스트용 TransparentWindowFlowTest가 담당합니다.
    /// 팀원의 원본 TransparentWindow는 수정하지 않습니다.
    /// </summary>
    [DefaultExecutionOrder(-20000)]
    public sealed class SceneWindowResolutionController : MonoBehaviour
    {
        private const float WindowResolveTimeoutSeconds = 3f;
        private const float WindowSettleDelaySeconds = 0.25f;
        private const int WindowFrameSizeTolerance = 64;
        private const int NativePositionTolerance = 1;
        private const int RequiredStableFrameCount = 3;

        public static event Action ResolutionApplied;

        [Header("Window Resolution")]
        [SerializeField] private bool useCurrentDisplayResolution;
        [SerializeField] private Vector2Int windowSize = new(1000, 1000);
        [SerializeField] private bool centerOnCurrentMonitor;

        [Header("Native Window")]
        [Tooltip("Windows Player에서 DWM 투명화 없이 창 테두리만 제거합니다.")]
        [SerializeField] private bool useBorderlessWindow;

        [Header("Editor")]
        [SerializeField] private bool applyInEditor;

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private const int GwlStyle = -16;
        private const uint WsPopup = 0x80000000;
        private const uint WsVisible = 0x10000000;
        private const uint WsCaption = 0x00C00000;
        private const uint WsThickFrame = 0x00040000;
        private const uint MonitorDefaultToNearest = 0x00000002;
        private const uint SwpNoSize = 0x0001;
        private const uint SwpNoMove = 0x0002;
        private const uint SwpNoZOrder = 0x0004;
        private const uint SwpNoActivate = 0x0010;
        private const uint SwpFrameChanged = 0x0020;

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
        private static extern int SetWindowLong(IntPtr windowHandle, int index, uint newStyle);

        [DllImport("user32.dll")]
        private static extern uint GetWindowLong(IntPtr windowHandle, int index);

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

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 네이티브 창 옵션은 Windows Player 전용 분기에서 사용됩니다.
            _ = useBorderlessWindow;
        }
#endif

        private void LateUpdate()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            // 상시 복구는 Scene-local인 Logo 무테 창에만 한정합니다.
            // Title의 Controller는 투명 창 오브젝트와 함께 Main까지 유지될 수 있으므로
            // 전환 직후 안정화가 끝난 뒤 다른 Scene의 창 영역을 계속 덮어쓰지 않습니다.
            if (!useBorderlessWindow || positionRoutine != null)
            {
                return;
            }

            int width = useCurrentDisplayResolution
                ? Display.main.systemWidth
                : Mathf.Max(1, windowSize.x);
            int height = useCurrentDisplayResolution
                ? Display.main.systemHeight
                : Mathf.Max(1, windowSize.y);

            bool fillCurrentMonitor = useCurrentDisplayResolution;
            bool shouldPositionWindow =
                fillCurrentMonitor || centerOnCurrentMonitor || useBorderlessWindow;

            if (!shouldPositionWindow ||
                !TryResolvePlayerWindow(width, height, out IntPtr windowHandle))
            {
                return;
            }

            bool styleApplied =
                !useBorderlessWindow || HasBorderlessWindowStyle(windowHandle);
            bool geometryApplied = IsWindowGeometryApplied(
                windowHandle,
                fillCurrentMonitor,
                centerOnCurrentMonitor,
                width,
                height,
                useBorderlessWindow);

            if (styleApplied && geometryApplied)
            {
                return;
            }

            if (useBorderlessWindow &&
                !styleApplied &&
                !ApplyBorderlessWindowStyle(windowHandle, false))
            {
                return;
            }

            PositionPlayerWindow(
                windowHandle,
                fillCurrentMonitor,
                centerOnCurrentMonitor,
                width,
                height,
                useBorderlessWindow,
                false);
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
                    useCurrentDisplayResolution,
                    centerOnCurrentMonitor,
                    useBorderlessWindow));
#elif UNITY_EDITOR
            ResolutionApplied?.Invoke();
#endif
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private IEnumerator FinalizeWindowAfterResolution(
            int targetWidth,
            int targetHeight,
            bool fillCurrentMonitor,
            bool centerWindow,
            bool borderlessWindow)
        {
            float timeoutAt = Time.realtimeSinceStartup + WindowResolveTimeoutSeconds;
            float stableCheckStartsAt =
                Time.realtimeSinceStartup + WindowSettleDelaySeconds;
            int stableFrameCount = 0;

            while (Time.realtimeSinceStartup < timeoutAt &&
                   stableFrameCount < RequiredStableFrameCount)
            {
                yield return null;

                bool resolutionApplied =
                    Mathf.Abs(Screen.width - targetWidth) <= WindowFrameSizeTolerance &&
                    Mathf.Abs(Screen.height - targetHeight) <= WindowFrameSizeTolerance;

                // 고정 크기 창은 요청 해상도가 반영된 뒤 무테와 중앙 배치를 적용해야
                // 기존 프레임 외곽 크기가 남거나 중심이 어긋나지 않습니다.
                if ((centerWindow || borderlessWindow) &&
                    !resolutionApplied)
                {
                    stableFrameCount = 0;
                    continue;
                }

                bool shouldPositionWindow =
                    fillCurrentMonitor || centerWindow || borderlessWindow;
                if (!shouldPositionWindow)
                {
                    stableFrameCount = resolutionApplied
                        ? stableFrameCount + 1
                        : 0;
                    continue;
                }

                if (!TryResolvePlayerWindow(
                        targetWidth,
                        targetHeight,
                        out IntPtr windowHandle))
                {
                    stableFrameCount = 0;
                    continue;
                }

                bool styleApplied =
                    !borderlessWindow || HasBorderlessWindowStyle(windowHandle);
                bool geometryApplied = IsWindowGeometryApplied(
                    windowHandle,
                    fillCurrentMonitor,
                    centerWindow,
                    targetWidth,
                    targetHeight,
                    borderlessWindow);

                if (styleApplied && geometryApplied)
                {
                    stableFrameCount = Time.realtimeSinceStartup >= stableCheckStartsAt
                        ? stableFrameCount + 1
                        : 0;
                    continue;
                }

                stableFrameCount = 0;

                if (borderlessWindow &&
                    !styleApplied &&
                    !ApplyBorderlessWindowStyle(windowHandle, false))
                {
                    continue;
                }

                PositionPlayerWindow(
                    windowHandle,
                    fillCurrentMonitor,
                    centerWindow,
                    targetWidth,
                    targetHeight,
                    borderlessWindow,
                    false);
            }

            bool windowResolved = stableFrameCount >= RequiredStableFrameCount;
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
            IntPtr windowHandle,
            bool fillCurrentMonitor,
            bool centerWindow,
            int expectedClientWidth,
            int expectedClientHeight,
            bool forceExactWindowSize,
            bool logFailure)
        {
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

            int targetX;
            int targetY;
            int targetWidth;
            int targetHeight;
            uint positionFlags = SwpNoZOrder | SwpNoActivate;

            if (fillCurrentMonitor)
            {
                targetX = monitorInfo.Monitor.Left;
                targetY = monitorInfo.Monitor.Top;
                targetWidth = monitorInfo.Monitor.Right - monitorInfo.Monitor.Left;
                targetHeight = monitorInfo.Monitor.Bottom - monitorInfo.Monitor.Top;
            }
            else
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

                int windowWidth = forceExactWindowSize
                    ? expectedClientWidth
                    : windowRect.Right - windowRect.Left;
                int windowHeight = forceExactWindowSize
                    ? expectedClientHeight
                    : windowRect.Bottom - windowRect.Top;

                targetX = windowRect.Left;
                targetY = windowRect.Top;
                targetWidth = forceExactWindowSize ? windowWidth : 0;
                targetHeight = forceExactWindowSize ? windowHeight : 0;

                if (!forceExactWindowSize)
                {
                    positionFlags |= SwpNoSize;
                }

                if (centerWindow)
                {
                    NativeRect workArea = monitorInfo.WorkArea;
                    targetX = workArea.Left +
                              (workArea.Right - workArea.Left - windowWidth) / 2;
                    targetY = workArea.Top +
                              (workArea.Bottom - workArea.Top - windowHeight) / 2;
                }
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

        private static bool IsWindowGeometryApplied(
            IntPtr windowHandle,
            bool fillCurrentMonitor,
            bool centerWindow,
            int expectedClientWidth,
            int expectedClientHeight,
            bool forceExactWindowSize)
        {
            if (!GetWindowRect(windowHandle, out NativeRect windowRect))
            {
                return false;
            }

            IntPtr monitorHandle = MonitorFromWindow(windowHandle, MonitorDefaultToNearest);
            MonitorInfo monitorInfo = new()
            {
                Size = (uint)Marshal.SizeOf<MonitorInfo>()
            };

            if (monitorHandle == IntPtr.Zero || !GetMonitorInfo(monitorHandle, ref monitorInfo))
            {
                return false;
            }

            if (fillCurrentMonitor)
            {
                return RectApproximatelyEquals(
                    windowRect,
                    monitorInfo.Monitor,
                    NativePositionTolerance);
            }

            int windowWidth = windowRect.Right - windowRect.Left;
            int windowHeight = windowRect.Bottom - windowRect.Top;
            if (forceExactWindowSize &&
                (Mathf.Abs(windowWidth - expectedClientWidth) > NativePositionTolerance ||
                 Mathf.Abs(windowHeight - expectedClientHeight) > NativePositionTolerance))
            {
                return false;
            }

            if (!centerWindow)
            {
                return true;
            }

            NativeRect workArea = monitorInfo.WorkArea;
            int expectedX = workArea.Left +
                            (workArea.Right - workArea.Left - windowWidth) / 2;
            int expectedY = workArea.Top +
                            (workArea.Bottom - workArea.Top - windowHeight) / 2;
            return Mathf.Abs(windowRect.Left - expectedX) <= NativePositionTolerance &&
                   Mathf.Abs(windowRect.Top - expectedY) <= NativePositionTolerance;
        }

        private static bool RectApproximatelyEquals(
            NativeRect first,
            NativeRect second,
            int tolerance)
        {
            return Mathf.Abs(first.Left - second.Left) <= tolerance &&
                   Mathf.Abs(first.Top - second.Top) <= tolerance &&
                   Mathf.Abs(first.Right - second.Right) <= tolerance &&
                   Mathf.Abs(first.Bottom - second.Bottom) <= tolerance;
        }

        private static bool TryResolvePlayerWindow(
            int expectedClientWidth,
            int expectedClientHeight,
            out IntPtr windowHandle)
        {
            return WindowsPlayerWindowResolver.TryGetVisiblePlayerWindow(
                out windowHandle,
                expectedClientWidth,
                expectedClientHeight);
        }

        private bool ApplyBorderlessWindowStyle(IntPtr windowHandle, bool logFailure)
        {
            SetWindowLong(windowHandle, GwlStyle, WsPopup | WsVisible);

            uint flags =
                SwpNoSize |
                SwpNoMove |
                SwpNoZOrder |
                SwpNoActivate |
                SwpFrameChanged;

            bool frameChanged = SetWindowPos(
                windowHandle,
                IntPtr.Zero,
                0,
                0,
                0,
                0,
                flags);
            bool borderlessApplied = HasBorderlessWindowStyle(windowHandle);

            if ((!frameChanged || !borderlessApplied) && logFailure)
            {
                Debug.LogWarning(
                    "[SceneWindowResolutionController] Player 창의 무테 스타일을 적용하지 못했습니다.",
                    this);
            }

            return frameChanged && borderlessApplied;
        }

        private static bool HasBorderlessWindowStyle(IntPtr windowHandle)
        {
            uint currentStyle = GetWindowLong(windowHandle, GwlStyle);
            bool hasPopupStyle = (currentStyle & WsPopup) != 0;
            bool hasWindowBorder = (currentStyle & (WsCaption | WsThickFrame)) != 0;
            return hasPopupStyle && !hasWindowBorder;
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
