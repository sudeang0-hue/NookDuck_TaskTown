//NB

using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;
using System.Collections;

// 메인 화면의 확장/축소 상태, 카메라 포커스 연동, 하단 아이콘 애니메이션을 전담하는 씬 전용 매니저
public class GameMasterManager : MonoBehaviour
{
    // 씬 내에서 접근 가능한 씬 싱글톤
    public static GameMasterManager Instance { get; private set; }

    // 외부 UI 매니저가 구독할 수 있는 전역 UI 수거 이벤트
    public static event Action OnCloseAllUIRequested;

    [Header("3D 오브젝트 및 카메라 설정")]
    public Transform villageOrigin;
    private Vector3 originalVillagePos;
    private Vector3 savedDraggedPosition;

    [Header("메인 패널 참조")]
    public GameObject expandedPanel;
    public GameObject minimizedPanel;

    [Header("전환 및 시스템 버튼들")]
    public Button btnMinimize;
    public Button btnMaximize;
    public Button btnQuit;

    [Header("하단 메인 아이콘들")]
    public RectTransform[] bottomIcons;
    private Vector2[] iconOriginalPositions;
    private Graphic menuPanelRaycastBlocker;

    [Header("티켓 알림 설정")]
    public GameObject ticketNotification;
    private float ticketTimer = 30f;
    private const float TICKET_COOLDOWN = 1800f; // 쿨타임 시간(초): $T_{\text{cooldown}} = 1800\text{s}$

    // 상태 관리 플래그
    private bool isExpanded = true;
    private bool isTransitioning = false; // 화면 전환 연타 방지용 가드 플래그

    private void Awake()
    {
        // 1. 씬 내부 싱글톤 할당 (씬이 재로드되면 자동으로 새 인스턴스로 교체됨)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // static 이벤트 초기화 (씬 재로드 시 이전 씬의 구독 찌꺼기 제거)
        OnCloseAllUIRequested = null;
    }

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

        // 2. 하단 아이콘 원본 좌표 캐싱 (Null 방어 구문 추가)
        if (bottomIcons != null && bottomIcons.Length > 0)
        {
            iconOriginalPositions = new Vector2[bottomIcons.Length];
            for (int i = 0; i < bottomIcons.Length; i++)
            {
                if (bottomIcons[i] == null) continue;
                bottomIcons[i].DOKill(); // 잔류 트윈 제거
                iconOriginalPositions[i] = bottomIcons[i].anchoredPosition;
            }
        }

        // [ 2026.08.11 - Choi - 축소 화면 클릭 통과 제어 ]
        // btnMinimize의 부모인 Menu_Panel_root의 투명 Graphic을 캐싱합니다.
        CacheMenuPanelRaycastBlocker();
        SetMenuPanelRaycastBlocking(true);

        // 3. 버튼 리스너 바인딩 (자체 리스너만 제거하여 다른 기능과 버튼음을 보존)
        InitButtonListeners();

        // 4. 초기 UI 패널 상태 설정
        if (expandedPanel) expandedPanel.SetActive(true);
        if (minimizedPanel) minimizedPanel.SetActive(false);
        if (ticketNotification) ticketNotification.SetActive(false);

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
    }

    private void OnEnable()
    {
        // 카메라 포커스 시작 이벤트 수신 시 UI 수거 함수 연결
        CameraDirector.OnCameraFocusStarted += CloseAllUI;
    }

    private void OnDisable()
    {
        // 메모리 누수 방지를 위한 이벤트 구독 해제
        CameraDirector.OnCameraFocusStarted -= CloseAllUI;
    }

    // 씬 전환/파괴 시 메모리 누수 방지 cleanup
    private void OnDestroy()
    {
        if (bottomIcons != null)
        {
            for (int i = 0; i < bottomIcons.Length; i++)
            {
                if (bottomIcons[i] != null) bottomIcons[i].DOKill();
            }
        }

        if (villageOrigin != null) villageOrigin.DOKill();

        OnCloseAllUIRequested = null;
        if (Instance == this) Instance = null;
    }

    private void InitButtonListeners()
    {
        if (btnMinimize)
        {
            btnMinimize.onClick.RemoveListener(SetMinimizedScreen);
            btnMinimize.onClick.AddListener(SetMinimizedScreen);
        }
        if (btnMaximize)
        {
            btnMaximize.onClick.RemoveListener(SetExpandedScreen);
            btnMaximize.onClick.AddListener(SetExpandedScreen);
        }
        if (btnQuit)
        {
            btnQuit.onClick.RemoveListener(QuitGame);
            btnQuit.onClick.AddListener(QuitGame);
        }
    }

    public void CloseAllUI()
    {
        OnCloseAllUIRequested?.Invoke();
    }

    #region 화면 확장 / 축소 제어

    public void SetMinimizedScreen()
    {
        if (!isExpanded || isTransitioning) return;
        StartCoroutine(MinimizeRoutine());
    }

    private IEnumerator MinimizeRoutine()
    {
        isTransitioning = true;
        isExpanded = false;
        SetMenuPanelRaycastBlocking(false);

        CloseAllUI();

        if (expandedPanel) expandedPanel.SetActive(false);
        if (minimizedPanel) minimizedPanel.SetActive(true);

        if (villageOrigin != null)
        {
            savedDraggedPosition = villageOrigin.position;
            villageOrigin.DOKill();
            villageOrigin.DOMove(originalVillagePos, 0.5f).SetEase(Ease.InOutQuad);
        }

        if (CameraDirector.Instance != null)
            CameraDirector.Instance.SetMinimizedView();

        for (int i = 0; i < bottomIcons.Length; i++)
        {
            if (bottomIcons[i] == null) continue;

            bottomIcons[i].DOKill();
            bottomIcons[i].anchoredPosition = iconOriginalPositions[i] + new Vector2(0, -400f);
        }

        yield return new WaitForSeconds(0.5f);
        isTransitioning = false;
    }

    public void SetExpandedScreen()
    {
        if (isExpanded || isTransitioning) return;
        StartCoroutine(ExpandRoutine());
    }

    private IEnumerator ExpandRoutine()
    {
        isTransitioning = true;
        isExpanded = true;
        SetMenuPanelRaycastBlocking(true);

        if (expandedPanel) expandedPanel.SetActive(true);
        if (minimizedPanel) minimizedPanel.SetActive(false);

        if (ticketNotification) ticketNotification.SetActive(false);
        ticketTimer = 0f;

        if (villageOrigin != null)
        {
            villageOrigin.DOKill();
            villageOrigin.DOMove(savedDraggedPosition, 0.5f).SetEase(Ease.InOutQuad);
        }

        if (CameraDirector.Instance != null)
            CameraDirector.Instance.SetExpandedView();

        AnimateIcons();

        yield return new WaitForSeconds(1.4f);
        isTransitioning = false;
    }

    public bool GetIsExpanded()
    {
        return isExpanded;
    }

    private void CacheMenuPanelRaycastBlocker()
    {
        if (btnMinimize == null || btnMinimize.transform.parent == null) return;

        btnMinimize.transform.parent.TryGetComponent(out menuPanelRaycastBlocker);
    }

    private void SetMenuPanelRaycastBlocking(bool shouldBlock)
    {
        if (menuPanelRaycastBlocker == null) return;

        menuPanelRaycastBlocker.raycastTarget = shouldBlock;
    }

    #endregion

    #region 시스템 및 연출 제어

    private void AnimateIcons()
    {
        if (bottomIcons == null) return;

        for (int i = 0; i < bottomIcons.Length; i++)
        {
            if (bottomIcons[i] == null) continue;

            bottomIcons[i].DOKill();

            float startY = iconOriginalPositions[i].y - 300f;
            bottomIcons[i].anchoredPosition = new Vector2(iconOriginalPositions[i].x, startY);

            bottomIcons[i].DOAnchorPosY(iconOriginalPositions[i].y, 0.6f)
                .SetEase(Ease.OutBounce)
                .SetDelay(0.2f + (i * 0.15f));
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
