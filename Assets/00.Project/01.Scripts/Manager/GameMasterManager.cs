using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

/// <summary>
/// 메인 화면의 확장/축소 상태, 카메라 포커스 연동, 하단 아이콘 애니메이션을 전담하는 메인 매니저
/// </summary>
public class GameMasterManager : MonoBehaviour
{
    [Header("3D 오브젝트 및 카메라 설정")]
    public Transform villageOrigin;
    private Vector3 originalVillagePos;
    private Vector3 savedDraggedPosition;

    [Header("메인 패널 참조")]
    public GameObject expandedPanel;
    public GameObject minimizedPanel;
    public GameObject menuPanel;
    public CanvasGroup menuCanvasGroup;

    [Header("전환 및 시스템 버튼들")]
    public Button btnMinimize;
    public Button btnMaximize;
    public Button btnQuit;
    public Button btnCloseMenu;

    [Header("하단 메인 아이콘들")]
    public RectTransform[] bottomIcons;
    private Vector2[] iconOriginalPositions;

    [Header("티켓 알림 설정")]
    public GameObject ticketNotification;
    private float ticketTimer = 30f;
    private const float TICKET_COOLDOWN = 1800f; // 쿨타임 시간(초): $T_{\text{cooldown}} = 1800\text{s}$

    // 상태 관리 플래그
    private bool isExpanded = true;
    private bool isMenuOpen = false;
    private bool isMenuAnimating = false;
    private bool isTransitioning = false; // 화면 전환 연타 방지용 가드 플래그

    void Start()
    {
        // 1. 마을 원본 좌표 캐싱 및 카메라 디렉터 연동
        if (villageOrigin != null)
        {
            originalVillagePos = villageOrigin.position;
            savedDraggedPosition = originalVillagePos;

            if (CameraDirector.Instance != null)
            {
                CameraDirector.Instance.SetupVillageOrigin(originalVillagePos);
            }
        }

        // 2. 하단 아이콘 원본 좌표 캐싱
        iconOriginalPositions = new Vector2[bottomIcons.Length];
        for (int i = 0; i < bottomIcons.Length; i++)
        {
            if (bottomIcons[i] == null) continue;
            iconOriginalPositions[i] = bottomIcons[i].anchoredPosition;
        }

        // 3. 버튼 리스너 바인딩
        if (btnMinimize) btnMinimize.onClick.AddListener(SetMinimizedScreen);
        if (btnMaximize) btnMaximize.onClick.AddListener(SetExpandedScreen);
        if (btnQuit) btnQuit.onClick.AddListener(QuitGame);
        if (btnCloseMenu) btnCloseMenu.onClick.AddListener(ToggleMenu);

        // 4. 초기 UI 패널 상태 설정
        expandedPanel.SetActive(true);
        minimizedPanel.SetActive(false);
        menuPanel.SetActive(false);
        if (menuCanvasGroup != null) menuCanvasGroup.alpha = 0f;

        // 5. 하단 아이콘 등장 연출
        AnimateIcons();
    }

    void Update()
    {
        // 화면이 축소 상태일 때 티켓 타이머 누적: $t_{\text{ticket}} \leftarrow t_{\text{ticket}} + \Delta t$
        if (!isExpanded)
        {
            ticketTimer += Time.deltaTime;
            if (ticketTimer >= TICKET_COOLDOWN && ticketNotification != null)
            {
                ticketNotification.SetActive(true);
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape)) ToggleMenu();
    }

    private void OnEnable()
    {
        // 카메라 포커스 시작 이벤트 수신 시 UI 정리
        CameraDirector.OnCameraFocusStarted += CloseAllUI;
    }

    private void OnDisable()
    {
        // 메모리 누수 방지를 위한 이벤트 구독 해제
        CameraDirector.OnCameraFocusStarted -= CloseAllUI;
    }

    /// <summary>
    /// 동물이 클릭되거나 카메라 포커스가 동작할 때 메인 메뉴 및 팝업 UI를 수거하는 핸들러
    /// </summary>
    public void CloseAllUI()
    {
        // 전체 메뉴 패널이 열려있다면 즉시 페이드 아웃 처리
        if (isMenuOpen && menuPanel != null && menuCanvasGroup != null)
        {
            isMenuOpen = false;
            isMenuAnimating = true;

            menuCanvasGroup.DOKill();
            menuCanvasGroup.DOFade(0f, 0.2f).SetUpdate(true).OnComplete(() =>
            {
                menuPanel.SetActive(false);
                isMenuAnimating = false;
            });
        }
    }

    #region 화면 확장 / 축소 제어

    /// <summary>
    /// 화면 축소 진입점 (연타 방지 가드 적용)
    /// </summary>
    public void SetMinimizedScreen()
    {
        if (!isExpanded || isTransitioning) return;
        StartCoroutine(MinimizeRoutine());
    }

    private IEnumerator MinimizeRoutine()
    {
        isTransitioning = true;
        isExpanded = false;

        CloseAllUI();

        expandedPanel.SetActive(false);
        minimizedPanel.SetActive(true);

        if (villageOrigin != null)
        {
            savedDraggedPosition = villageOrigin.position;
            villageOrigin.DOKill();
            villageOrigin.DOMove(originalVillagePos, 0.5f).SetEase(Ease.InOutQuad);
        }

        // 카메라 축소 연출 호출
        if (CameraDirector.Instance != null)
            CameraDirector.Instance.SetMinimizedView();

        // 하단 아이콘 숨기기 연출
        for (int i = 0; i < bottomIcons.Length; i++)
        {
            if (bottomIcons[i] == null) continue;

            bottomIcons[i].DOKill(); // 잔류 트윈 제거
            bottomIcons[i].anchoredPosition = iconOriginalPositions[i] + new Vector2(0, -400f);
        }

        yield return new WaitForSeconds(0.5f);
        isTransitioning = false;
    }

    /// <summary>
    /// 화면 확장 진입점 (연타 방지 가드 적용)
    /// </summary>
    public void SetExpandedScreen()
    {
        if (isExpanded || isTransitioning) return;
        StartCoroutine(ExpandRoutine());
    }

    private IEnumerator ExpandRoutine()
    {
        isTransitioning = true;
        isExpanded = true;
        expandedPanel.SetActive(true);
        minimizedPanel.SetActive(false);

        if (ticketNotification) ticketNotification.SetActive(false);
        ticketTimer = 0f;

        if (villageOrigin != null)
        {
            villageOrigin.DOKill();
            villageOrigin.DOMove(savedDraggedPosition, 0.5f).SetEase(Ease.InOutQuad);
        }

        // 카메라 복귀 연출 호출
        if (CameraDirector.Instance != null)
            CameraDirector.Instance.SetExpandedView();

        // 아이콘 등장 바운스 연출
        AnimateIcons();

        // 아이콘 연출 완료까지 입력 대기 (딜레이 + 재생시간 고려)
        yield return new WaitForSeconds(1.4f);
        isTransitioning = false;
    }

    public bool GetIsExpanded()
    {
        return isExpanded;
    }

    #endregion

    #region 메뉴 및 시스템 제어

    private void AnimateIcons()
    {
        for (int i = 0; i < bottomIcons.Length; i++)
        {
            if (bottomIcons[i] == null) continue;

            bottomIcons[i].DOKill();

            float startY = iconOriginalPositions[i].y - 300f;
            bottomIcons[i].anchoredPosition = new Vector2(iconOriginalPositions[i].x, startY);

            // 순차적 바운스 연출 ($t_i = 0.2 + 0.15 \times i$)
            bottomIcons[i].DOAnchorPosY(iconOriginalPositions[i].y, 0.6f)
                .SetEase(Ease.OutBounce)
                .SetDelay(0.2f + (i * 0.15f));
        }
    }

    private void ToggleMenu()
    {
        if (isMenuAnimating || menuPanel == null || menuCanvasGroup == null) return;
        isMenuOpen = !isMenuOpen;
        isMenuAnimating = true;

        if (isMenuOpen)
        {
            menuPanel.SetActive(true);
            menuCanvasGroup.DOFade(1f, 0.25f).SetUpdate(true).OnComplete(() => isMenuAnimating = false);
        }
        else
        {
            menuCanvasGroup.DOFade(0f, 0.25f).SetUpdate(true).OnComplete(() =>
            {
                menuPanel.SetActive(false);
                isMenuAnimating = false;
            });
        }
    }

    private void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    #endregion
}