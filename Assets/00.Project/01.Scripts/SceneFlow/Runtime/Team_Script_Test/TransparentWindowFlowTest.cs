/*
 * [원본 및 테스트 목적]
 * - 원본 파일: Assets/00.Project/01.Scripts/System/TransparentWindow.cs
 * - 팀원이 작성한 원본은 수정하지 않고, Scene Flow 환경에서 먼저 검증하기 위해 복사·확장한 테스트 버전입니다.
 *
 * [원본과 다른 점 / 수정한 내용]
 * 1. Title에서 생성된 인스턴스를 DontDestroyOnLoad로 유지하되, 다음 Scene에 원본 TransparentWindow가 있으면 제어권을 넘깁니다.
 * 2. Scene 전환 및 해상도 적용 완료 이벤트에서 스타일을 재적용하고, Unity가 테두리를 복원하면 즉시 다시 무테로 교정합니다.
 * 3. Title Flow 재진입 시 중복 인스턴스가 생성되지 않도록 방지합니다.
 * 4. 원본의 UI 및 3D Collider 클릭 판정을 유지하면서 2D Collider 판정도 추가했습니다.
 * 5. 다른 앱의 Foreground 창을 잘못 수정하지 않도록 프로세스 소유 창 핸들만 사용하고 실패 경고를 추가했습니다.
 * 6. 실제 표시 중인 UnityWndClass 창을 다시 찾아 해상도 전환 중 교체된 HWND에 잘못 적용하는 문제를 방지합니다.
 * 7. 투명 구간 Camera를 Alpha가 있는 LDR 출력으로 고정해 HDR 32-bit 버퍼의 Alpha 소실을 방지합니다.
 * 8. Main Scene에 팀 원본 TransparentWindow가 있으면 테스트 복사본 GameObject를 제거해 Win32 창 제어 충돌을 방지합니다.
 *
 * [역할 분리]
 * - 이 스크립트: Windows 창의 무테, DWM 투명화, Topmost, 빈 영역 클릭 관통을 담당합니다.
 * - SceneWindowResolutionController: Scene별 창 크기와 모니터 중앙 배치를 담당합니다.
 */

using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace TaskTown.SceneFlow.Testing
{
    [DefaultExecutionOrder(-19000)]
    public sealed class TransparentWindowFlowTest : MonoBehaviour
    {
        private const float WindowRefreshTimeoutSeconds = 3f;
        private const int RequiredStableFrameCount = 3;

        [Header("Native Window")]
        [SerializeField] private bool keepWindowTopmost = true;

        [Header("Click Through")]
        [SerializeField] private bool detectUi = true;
        [SerializeField] private bool detect3DCollider = true;
        [SerializeField] private bool detect2DCollider = true;

        private static TransparentWindowFlowTest instance;

        private Camera mainCamera;
        private Coroutine sceneRefreshRoutine;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 아래 옵션들은 Windows Player 전용 분기에서 사용됩니다.
            _ = keepWindowTopmost;
            _ = detectUi;
            _ = detect3DCollider;
            _ = detect2DCollider;
        }
#endif

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private const int GwlStyle = -16;
        private const int GwlExStyle = -20;
        private const uint WsPopup = 0x80000000;
        private const uint WsVisible = 0x10000000;
        private const uint WsCaption = 0x00C00000;
        private const uint WsThickFrame = 0x00040000;
        private const uint WsExTransparent = 0x00000020;
        private const uint SwpNoSize = 0x0001;
        private const uint SwpNoMove = 0x0002;
        private const uint SwpNoZOrder = 0x0004;
        private const uint SwpNoActivate = 0x0010;
        private const uint SwpFrameChanged = 0x0020;

        private static readonly IntPtr HwndTopmost = new(-1);

        private IntPtr windowHandle;

        [StructLayout(LayoutKind.Sequential)]
        private struct Margins
        {
            public int LeftWidth;
            public int RightWidth;
            public int TopHeight;
            public int BottomHeight;
        }

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr window, int index, uint newStyle);

        [DllImport("user32.dll")]
        private static extern uint GetWindowLong(IntPtr window, int index);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(
            IntPtr window,
            IntPtr insertAfter,
            int x,
            int y,
            int width,
            int height,
            uint flags);

        [DllImport("Dwmapi.dll")]
        private static extern uint DwmExtendFrameIntoClientArea(IntPtr window, ref Margins margins);
#endif

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.activeSceneChanged += HandleActiveSceneChanged;
            SceneWindowResolutionController.ResolutionApplied += HandleResolutionApplied;
        }

        private void Start()
        {
            BindAndConfigureMainCamera();
            ApplyNativeWindowStyle();
            QueueSceneRefresh();
        }

        private void LateUpdate()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if (mainCamera == null || !mainCamera.isActiveAndEnabled)
            {
                BindAndConfigureMainCamera();
            }

            EnsureNativeWindowStyle();
            HandleClickThrough();
#endif
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                InvalidateWindowHandle();
                QueueSceneRefresh();
            }
        }

        private void HandleActiveSceneChanged(Scene previousScene, Scene nextScene)
        {
            if (TryHandOffToOriginalTransparentWindow(nextScene))
            {
                return;
            }

            InvalidateWindowHandle();
            BindAndConfigureMainCamera();
            QueueSceneRefresh();
        }

        private bool TryHandOffToOriginalTransparentWindow(Scene nextScene)
        {
            if (!nextScene.IsValid() || !nextScene.isLoaded)
            {
                return false;
            }

            GameObject[] rootObjects = nextScene.GetRootGameObjects();
            for (int i = 0; i < rootObjects.Length; i++)
            {
                if (rootObjects[i].GetComponentInChildren<global::TransparentWindow>(true) == null)
                {
                    continue;
                }

                Debug.Log(
                    "[TransparentWindowFlowTest] 다음 Scene의 원본 TransparentWindow에 창 제어를 인계합니다.",
                    this);
                Destroy(gameObject);
                return true;
            }

            return false;
        }

        private void HandleResolutionApplied()
        {
            InvalidateWindowHandle();
            QueueSceneRefresh();
        }

        private void QueueSceneRefresh()
        {
            if (sceneRefreshRoutine != null)
            {
                StopCoroutine(sceneRefreshRoutine);
            }

            sceneRefreshRoutine = StartCoroutine(RefreshWindowForScene());
        }

        private IEnumerator RefreshWindowForScene()
        {
            float timeoutAt = Time.realtimeSinceStartup + WindowRefreshTimeoutSeconds;
            int stableFrameCount = 0;

            while (Time.realtimeSinceStartup < timeoutAt &&
                   stableFrameCount < RequiredStableFrameCount)
            {
                yield return null;
                BindAndConfigureMainCamera();
                stableFrameCount = ApplyNativeWindowStyle()
                    ? stableFrameCount + 1
                    : 0;
            }

            if (stableFrameCount < RequiredStableFrameCount)
            {
                Debug.LogWarning(
                    "[TransparentWindowFlowTest] 제한 시간 안에 표시 중인 Player 창의 투명 스타일을 안정적으로 적용하지 못했습니다.",
                    this);
            }

            sceneRefreshRoutine = null;
        }

        private void BindAndConfigureMainCamera()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }

            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = Color.clear;
            mainCamera.allowHDR = false;
            mainCamera.allowMSAA = false;
        }

        private bool ApplyNativeWindowStyle()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if (!TryResolvePlayerWindowHandle())
            {
                return false;
            }

            SetWindowLong(windowHandle, GwlStyle, WsPopup | WsVisible);

            IntPtr insertAfter = keepWindowTopmost ? HwndTopmost : IntPtr.Zero;
            uint flags = SwpNoSize | SwpNoMove | SwpNoActivate | SwpFrameChanged;
            if (!keepWindowTopmost)
            {
                flags |= SwpNoZOrder;
            }

            if (!SetWindowPos(windowHandle, insertAfter, 0, 0, 0, 0, flags))
            {
                Debug.LogWarning("[TransparentWindowFlowTest] 무테 창 스타일 적용에 실패했습니다.", this);
                return false;
            }

            Margins margins = new()
            {
                LeftWidth = -1
            };

            uint dwmResult = DwmExtendFrameIntoClientArea(windowHandle, ref margins);
            if (dwmResult != 0)
            {
                Debug.LogWarningFormat(
                    this,
                    "[TransparentWindowFlowTest] DWM 투명 영역 확장에 실패했습니다. HRESULT=0x{0:X8}",
                    dwmResult);
                return false;
            }

            return true;
#else
            return true;
#endif
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private void EnsureNativeWindowStyle()
        {
            if (!TryResolvePlayerWindowHandle())
            {
                return;
            }

            uint currentStyle = GetWindowLong(windowHandle, GwlStyle);
            bool hasPopupStyle = (currentStyle & WsPopup) != 0;
            bool hasWindowBorder = (currentStyle & (WsCaption | WsThickFrame)) != 0;

            if (!hasPopupStyle || hasWindowBorder)
            {
                ApplyNativeWindowStyle();
            }
        }

        private bool TryResolvePlayerWindowHandle()
        {
            if (WindowsPlayerWindowResolver.IsVisiblePlayerWindow(windowHandle))
            {
                return true;
            }

            return WindowsPlayerWindowResolver.TryGetVisiblePlayerWindow(
                out windowHandle,
                Screen.width,
                Screen.height);
        }

        private void HandleClickThrough()
        {
            if (windowHandle == IntPtr.Zero)
            {
                return;
            }

            bool isOverUi = detectUi &&
                            EventSystem.current != null &&
                            EventSystem.current.IsPointerOverGameObject();
            bool isOver3D = false;
            bool isOver2D = false;

            if (mainCamera != null)
            {
                Ray pointerRay = mainCamera.ScreenPointToRay(Input.mousePosition);

                if (detect3DCollider)
                {
                    isOver3D = Physics.Raycast(pointerRay);
                }

                if (detect2DCollider)
                {
                    isOver2D = Physics2D.GetRayIntersection(pointerRay).collider != null;
                }
            }

            SetClickThrough(!(isOverUi || isOver3D || isOver2D));
        }

        private void SetClickThrough(bool shouldClickThrough)
        {
            uint currentStyle = GetWindowLong(windowHandle, GwlExStyle);
            bool isCurrentlyClickThrough = (currentStyle & WsExTransparent) != 0;
            if (isCurrentlyClickThrough == shouldClickThrough)
            {
                return;
            }

            uint nextStyle = shouldClickThrough
                ? currentStyle | WsExTransparent
                : currentStyle & ~WsExTransparent;
            SetWindowLong(windowHandle, GwlExStyle, nextStyle);
        }
#endif

        private void InvalidateWindowHandle()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            windowHandle = IntPtr.Zero;
#endif
        }

        private void OnDestroy()
        {
            if (instance != this)
            {
                return;
            }

            SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
            SceneWindowResolutionController.ResolutionApplied -= HandleResolutionApplied;

            if (sceneRefreshRoutine != null)
            {
                StopCoroutine(sceneRefreshRoutine);
                sceneRefreshRoutine = null;
            }

            instance = null;
        }
    }
}
