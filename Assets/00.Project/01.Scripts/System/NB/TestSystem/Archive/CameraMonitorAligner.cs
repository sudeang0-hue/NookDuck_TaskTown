//NB

using UnityEngine;
using System.Runtime.InteropServices;
using System;

// 다중 모니터 환경에서 주 모니터 중앙으로 카메라와 UI를 정렬해주는 관리자 클래스
public class CameraMonitorAligner : MonoBehaviour
{
    [Header("카메라 설정")]
    [Tooltip("렌더링 짤림(Clipping) 방지를 위해 카메라를 뒤로 뺄 안전한 Z 거리")]
    [SerializeField] private float safeCameraZ = -50f;

    private Camera targetCamera;

    // --- Win32 API Import ---
    // (OS에서 모니터 정보를 가져오기 위한 구조체 및 함수)
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    // SM_CXVIRTUALSCREEN = 78, SM_CYVIRTUALSCREEN = 79 (가상 화면 크기)
    // SM_XVIRTUALSCREEN = 76, SM_YVIRTUALSCREEN = 77 (가상 화면 시작 좌표)
    private const int SM_XVIRTUALSCREEN = 76;
    private const int SM_YVIRTUALSCREEN = 77;
    private const int SM_CXVIRTUALSCREEN = 78;
    private const int SM_CYVIRTUALSCREEN = 79;
    private const int SM_CXSCREEN = 0; // 주 모니터 너비
    private const int SM_CYSCREEN = 1; // 주 모니터 높이

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();

        // 런타임 빌드 환경에서만 위치 정렬 수행 (에디터에선 단일 창이므로 무시)
#if !UNITY_EDITOR
        AlignToPrimaryMonitor();
#endif
    }

    // 주 모니터의 위치를 계산하여 카메라의 오프셋을 재조정
    private void AlignToPrimaryMonitor()
    {
        // 1. 가상 화면(모든 모니터가 합쳐진 영역)의 정보 가져오기
        int vLeft = GetSystemMetrics(SM_XVIRTUALSCREEN);
        int vTop = GetSystemMetrics(SM_YVIRTUALSCREEN);
        int vWidth = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        int vHeight = GetSystemMetrics(SM_CYVIRTUALSCREEN);

        // 가상 화면의 중앙 픽셀 좌표
        float vCenterX = vLeft + (vWidth / 2f);
        float vCenterY = vTop + (vHeight / 2f);

        // 2. 주 모니터 정보 가져오기 (주 모니터는 항상 0,0을 기준으로 합니다)
        int pWidth = GetSystemMetrics(SM_CXSCREEN);
        int pHeight = GetSystemMetrics(SM_CYSCREEN);

        // 주 모니터의 중앙 픽셀 좌표
        float pCenterX = pWidth / 2f;
        float pCenterY = pHeight / 2f;

        // 3. 중심점 간의 차이(Offset) 계산
        float deltaX = pCenterX - vCenterX;
        float deltaY = pCenterY - vCenterY;

        // 4. 카메라 픽셀 -> 월드 좌표 변환
        // (직교 카메라 기준. Perspective일 경우 거리(Z)에 따른 FOV 계산이 추가로 필요)
        float worldPerPixel = (targetCamera.orthographicSize * 2f) / Screen.height;

        // 5. 유니티 월드 좌표계 적용 (Y축은 화면과 방향이 반대일 수 있으므로 부호 주의)
        float finalWorldX = -deltaX * worldPerPixel;
        float finalWorldY = deltaY * worldPerPixel;

        // 6. 카메라 위치 갱신 (safeCameraZ를 통해 짤림 방지)
        transform.position = new Vector3(finalWorldX, finalWorldY, safeCameraZ);

        Debug.Log($"카메라 주 모니터 정렬 완료! Delta: ({deltaX}, {deltaY}), WorldPos: {transform.position}");
    }
}