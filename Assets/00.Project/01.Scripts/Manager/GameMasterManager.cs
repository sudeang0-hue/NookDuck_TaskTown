// NB
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

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

    [Header("독립 팝업 패널들 (RectTransform)")]
    public RectTransform panelDex;       // 1. 도감 (좌 -> 우 등장)
    public RectTransform panelGacha;     // 2. 가챠 (아래 -> 위 등장)
    public RectTransform panelManage;    // 3. 동물 인벤토리/관리 (우 -> 좌 등장)
    public RectTransform panelOption;    // 4. 옵션/설정 (위 -> 아래 등장)
    public RectTransform panelToolInv;   // 5. 도구 인벤토리 (우 -> 좌 등장)

    // 열렸을 때의 오리지널 기준 좌표 저장 변수들
    private Vector2 posDexOpen;
    private Vector2 posGachaOpen;
    private Vector2 posManageOpen;
    private Vector2 posOptionOpen;
    private Vector2 posToolInvOpen;

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
    private const float TICKET_COOLDOWN = 1800f;

    //상태 관리 플래그
    private bool isExpanded = true;
    private bool isMenuOpen = false;
    private bool isMenuAnimating = false;
    private bool isTransitioning = false; //화면 전환 연타 방지용 가드 플래그

    void Awake()
    {
        // 씬 시작 시 오리지널 좌표를 기억하고, 우선은 패널들을 비활성화(-2000f 등 화면 바깥 배치 후 꺼두기)
        CacheAndHidePanels();
    }

    void Start()
    {
        if (villageOrigin != null)
        {
            originalVillagePos = villageOrigin.position;
            savedDraggedPosition = originalVillagePos;

            // 카메라 디렉터에게 원본 위치만 전달
            if (CameraDirector.Instance != null)
            {
                CameraDirector.Instance.SetupVillageOrigin(originalVillagePos);
            }
        }

        iconOriginalPositions = new Vector2[bottomIcons.Length];
        for (int i = 0; i < bottomIcons.Length; i++)
        {
            if (bottomIcons[i] == null) continue;
            iconOriginalPositions[i] = bottomIcons[i].anchoredPosition;
        }

        if (btnMinimize) btnMinimize.onClick.AddListener(SetMinimizedScreen);
        if (btnMaximize) btnMaximize.onClick.AddListener(SetExpandedScreen);
        if (btnQuit) btnQuit.onClick.AddListener(QuitGame);
        if (btnCloseMenu) btnCloseMenu.onClick.AddListener(ToggleMenu);

        expandedPanel.SetActive(true);
        minimizedPanel.SetActive(false);
        menuPanel.SetActive(false);
        menuCanvasGroup.alpha = 0f;

        AnimateIcons();
    }

    void Update()
    {
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
        // 카메라 포커스 시작 이벤트 처리기 등록
        CameraDirector.OnCameraFocusStarted += CloseAllUI;
    }

    private void OnDisable()
    {
        // 메모리 누수 방지를 위한 구독 해제
        CameraDirector.OnCameraFocusStarted -= CloseAllUI;
    }

    // 동물이 클릭되어 팔로우 캠이 동작할 때 호출되는 모든 UI 수거 핸들러
    public void CloseAllUI()
    {
        // 1. 모든 독립 팝업 판넬 닫기 (DOTween 닫기 연출 진행)
        CloseAllPopups();

        // 2. 전체 메뉴 판넬이 열려있다면 즉시 페이드 아웃 처리
        if (isMenuOpen && menuPanel != null)
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

    // 패널들의 원래 위치를 저장하고, 시작하자마자 구석으로 치운 뒤 비활성화
    private void CacheAndHidePanels()
    {
        if (panelDex != null)
        {
            posDexOpen = panelDex.anchoredPosition;
            panelDex.anchoredPosition = new Vector2(posDexOpen.x - 2000f, posDexOpen.y);
            panelDex.gameObject.SetActive(false);
        }
        if (panelGacha != null)
        {
            posGachaOpen = panelGacha.anchoredPosition;
            panelGacha.anchoredPosition = new Vector2(posGachaOpen.x, posGachaOpen.y - 1200f);
            panelGacha.gameObject.SetActive(false);
        }
        if (panelManage != null)
        {
            posManageOpen = panelManage.anchoredPosition;
            panelManage.anchoredPosition = new Vector2(posManageOpen.x + 2000f, posManageOpen.y);
            panelManage.gameObject.SetActive(false);
        }
        if (panelOption != null)
        {
            posOptionOpen = panelOption.anchoredPosition;
            panelOption.anchoredPosition = new Vector2(posOptionOpen.x, posOptionOpen.y + 1200f);
            panelOption.gameObject.SetActive(false);
        }
        if (panelToolInv != null)
        {
            posToolInvOpen = panelToolInv.anchoredPosition;
            panelToolInv.anchoredPosition = new Vector2(posToolInvOpen.x + 2000f, posToolInvOpen.y);
            panelToolInv.gameObject.SetActive(false);
        }
    }

    // 화면 축소 진입점 (연타 및 중복 실행 방지 가드 적용)
    public void SetMinimizedScreen()
    {
        if (!isExpanded || isTransitioning) return;
        StartCoroutine(MinimizeRoutine());
    }

    // 축소 연출 코루틴 (트윈 충돌 방지 및 안전한 상태 전환)
    private IEnumerator MinimizeRoutine()
    {
        isTransitioning = true;
        isExpanded = false;
        CloseAllPopups();

        expandedPanel.SetActive(false);
        minimizedPanel.SetActive(true);

        if (villageOrigin != null)
        {
            savedDraggedPosition = villageOrigin.position;
            villageOrigin.DOKill();
            villageOrigin.DOMove(originalVillagePos, 0.5f).SetEase(Ease.InOutQuad);
        }

        // 카메라 축소 연출은 디렉터가 알아서 처리
        if (CameraDirector.Instance != null)
            CameraDirector.Instance.SetMinimizedView();

        // 아이콘들을 내리기 전 기존 트윈을 반드시 강제 종료
        for (int i = 0; i < bottomIcons.Length; i++)
        {
            if (bottomIcons[i] == null) continue;

            bottomIcons[i].DOKill(); // 잔류 버그 방지
            bottomIcons[i].anchoredPosition = iconOriginalPositions[i] + new Vector2(0, -400f);
        }

        // 연출 시간 동안 입력 락 유지
        yield return new WaitForSeconds(0.5f);
        isTransitioning = false;
    }

    // 화면 확장 진입점 (연타 및 중복 실행 방지 가드 적용)
    public void SetExpandedScreen()
    {
        if (isExpanded || isTransitioning) return;
        StartCoroutine(ExpandRoutine());
    }

    // 확장 연출 코루틴 (애니메이션 완료 대기 포함)
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

        // 카메라 복귀 연출도 디렉터가 알아서 처리
        if (CameraDirector.Instance != null)
            CameraDirector.Instance.SetExpandedView();

        // 아이콘 등장 연출 실행
        AnimateIcons();

        // 모든 아이콘 바운스 애니메이션이 끝날 때까지 대기 (약 1.4초: 딜레이 최대치 + 재생 시간) 후 락 해제
        yield return new WaitForSeconds(1.4f);
        isTransitioning = false;
    }
    
    public bool GetIsExpanded()
    {
        return isExpanded;
    }

    #region UI 개별 제어 세트 (독립 제어 및 SetActive 통합 설계)

    // 1. 도감 (Dex) 제어
    public void OpenDex()
    {
        if (panelDex == null) return;
        panelDex.gameObject.SetActive(true); // 두트윈 실행 전 활성화!
        panelDex.DOKill();
        panelDex.DOAnchorPosX(posDexOpen.x, 0.4f).SetEase(Ease.OutQuad);
    }
    public void CloseDex()
    {
        if (panelDex == null) return;
        panelDex.DOKill();
        panelDex.DOAnchorPosX(posDexOpen.x - 2000f, 0.4f).SetEase(Ease.InQuad)
                 .OnComplete(() => panelDex.gameObject.SetActive(false)); // 닫히는 연출 끝나면 완전히 비활성화!
    }

    // 2. 가챠 (Gacha) 제어
    public void OpenGacha()
    {
        if (panelGacha == null) return;
        panelGacha.gameObject.SetActive(true);
        panelGacha.DOKill();
        panelGacha.DOAnchorPosY(posGachaOpen.y, 0.4f).SetEase(Ease.OutQuad);
    }
    public void CloseGacha()
    {
        if (panelGacha == null) return;
        panelGacha.DOKill();
        panelGacha.DOAnchorPosY(posGachaOpen.y - 1200f, 0.4f).SetEase(Ease.InQuad)
                 .OnComplete(() => panelGacha.gameObject.SetActive(false));
    }

    // 3. 동물 관리 (Manage) 제어
    public void OpenManage()
    {
        if (panelManage == null) return;
        panelManage.gameObject.SetActive(true);
        panelManage.DOKill();
        panelManage.DOAnchorPosX(posManageOpen.x, 0.4f).SetEase(Ease.OutQuad);
    }
    public void CloseManage()
    {
        if (panelManage == null) return;
        panelManage.DOKill();
        panelManage.DOAnchorPosX(posManageOpen.x + 2000f, 0.4f).SetEase(Ease.InQuad)
                 .OnComplete(() => panelManage.gameObject.SetActive(false));
    }

    // 4. 옵션 (Option) 제어 
    public void OpenOption()
    {
        if (panelOption == null) return;
        panelOption.gameObject.SetActive(true);
        panelOption.DOKill();
        panelOption.DOAnchorPosY(posOptionOpen.y, 0.4f).SetEase(Ease.OutQuad);
    }
    public void CloseOption()
    {
        if (panelOption == null) return;
        panelOption.DOKill();
        panelOption.DOAnchorPosY(posOptionOpen.y + 1200f, 0.4f).SetEase(Ease.InQuad)
                 .OnComplete(() => panelOption.gameObject.SetActive(false));
    }

    // 5. 도구 인벤토리 (Tool_Inv) 제어 
    public void OpenToolInv()
    {
        if (panelToolInv == null) return;
        panelToolInv.gameObject.SetActive(true);
        panelToolInv.DOKill();
        panelToolInv.DOAnchorPosX(posToolInvOpen.x, 0.4f).SetEase(Ease.OutQuad);
    }
    public void CloseToolInv()
    {
        if (panelToolInv == null) return;
        panelToolInv.DOKill();
        panelToolInv.DOAnchorPosX(posToolInvOpen.x + 2000f, 0.4f).SetEase(Ease.InQuad)
                 .OnComplete(() => panelToolInv.gameObject.SetActive(false));
    }

    // 화면 축소 시 모든 UI를 쓸어 담는 전체 닫기 기능
    public void CloseAllPopups()
    {
        if (panelDex != null) { panelDex.DOKill(); panelDex.DOAnchorPosX(posDexOpen.x - 2000f, 0.2f).OnComplete(() => panelDex.gameObject.SetActive(false)); }
        if (panelGacha != null) { panelGacha.DOKill(); panelGacha.DOAnchorPosY(posGachaOpen.y - 1200f, 0.2f).OnComplete(() => panelGacha.gameObject.SetActive(false)); }
        if (panelManage != null) { panelManage.DOKill(); panelManage.DOAnchorPosX(posManageOpen.x + 2000f, 0.2f).OnComplete(() => panelManage.gameObject.SetActive(false)); }
        if (panelOption != null) { panelOption.DOKill(); panelOption.DOAnchorPosY(posOptionOpen.y + 1200f, 0.2f).OnComplete(() => panelOption.gameObject.SetActive(false)); }
        if (panelToolInv != null) { panelToolInv.DOKill(); panelToolInv.DOAnchorPosX(posToolInvOpen.x + 2000f, 0.2f).OnComplete(() => panelToolInv.gameObject.SetActive(false)); }
    }

    private void AnimateIcons()
    {
        for (int i = 0; i < bottomIcons.Length; i++)
        {
            if (bottomIcons[i] == null) continue;

            //개별 아이콘 트윈 시작 직전 무조건 DOKill 호출하여 꼬임 방지
            bottomIcons[i].DOKill();

            float startY = iconOriginalPositions[i].y - 300f;
            bottomIcons[i].anchoredPosition = new Vector2(iconOriginalPositions[i].x, startY);

            // 순차적 바운스 연출
            bottomIcons[i].DOAnchorPosY(iconOriginalPositions[i].y, 0.6f)
                .SetEase(Ease.OutBounce)
                .SetDelay(0.2f + (i * 0.15f));
        }
    }

    private void ToggleMenu()
    {
        if (isMenuAnimating || menuPanel == null) return;
        isMenuOpen = !isMenuOpen;
        isMenuAnimating = true;

        if (isMenuOpen)
        {
            menuPanel.SetActive(true);
            menuCanvasGroup.DOFade(1f, 0.25f).SetUpdate(true).OnComplete(() => isMenuAnimating = false);
        }
        else
        {
            menuCanvasGroup.DOFade(0f, 0.25f).SetUpdate(true).OnComplete(() => { menuPanel.SetActive(false); isMenuAnimating = false; });
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
