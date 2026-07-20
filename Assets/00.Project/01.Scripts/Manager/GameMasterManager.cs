// NB
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class GameMasterManager : MonoBehaviour
{
    [Header("3D 오브젝트 및 카메라 설정")]
    public Transform villageOrigin;
    private Camera mainCamera;

    private Vector3 camOriginalPos;
    private float camOriginalSize;
    private Vector3 originalVillagePos;
    private Vector3 savedDraggedPosition;

    [Header("축소 화면용 카메라 타겟 셋팅")]
    public Transform miniVillagePos;

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
    private float ticketTimer = 0f;
    private const float TICKET_COOLDOWN = 1800f;

    [Header("팔로우 카메라 설정")]
    public float zoomInSize = 2.5f;          // 줌인되었을 때 카메라 Ortho Size
    public float followSpeed = 5f;            // 동물을 따라가는 카메라 이동 속도
    public Vector3 followOffset = new Vector3(0, 0, 0); // 줌인 시 카메라 위치 오프셋
    public Button btnUnfocus;                // 동물 감상 모드 해제 버튼 (UI)

    private Transform targetAnimal = null;   // 현재 추적 중인 동물
    private bool isFollowing = false;        // 팔로우 상태 여부

    private bool isExpanded = true;
    private bool isMenuOpen = false;
    private bool isMenuAnimating = false;

    void Awake()
    {
        // 씬 시작 시 오리지널 좌표를 기억하고, 우선은 패널들을 비활성화(-2000f 등 화면 바깥 배치 후 꺼두기)
        CacheAndHidePanels();
    }

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            camOriginalPos = mainCamera.transform.position;
            camOriginalSize = mainCamera.orthographicSize;
        }

        if (villageOrigin != null)
        {
            originalVillagePos = villageOrigin.position;
            savedDraggedPosition = originalVillagePos;
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

        if (btnUnfocus != null)
        {
            btnUnfocus.onClick.AddListener(UnfocusAnimal);
            btnUnfocus.gameObject.SetActive(false); // 시작할 때는 끄기
        }

        expandedPanel.SetActive(true);
        minimizedPanel.SetActive(false);
        menuPanel.SetActive(false);
        menuCanvasGroup.alpha = 0f;

        AnimateIcons();
    }

    void LateUpdate()
    {
        // 동물을 추적 중이고 타겟이 존재할 때
        if (isFollowing && targetAnimal != null)
        {
            // 동물의 위치에 offset을 더하되, 카메라의 원래 Z값은 유지
            Vector3 targetPos = new Vector3(targetAnimal.position.x, targetAnimal.position.y, camOriginalPos.z) + followOffset;

            // Vector3.Lerp를 이용해 부드럽게 동물을 따라감
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetPos, Time.deltaTime * followSpeed);
        }
    }
    /// <summary>
    /// 동물을 클릭했을 때 호출되는 카메라 줌인 및 팔로우 시작 메서드
    /// </summary>
    public void FocusOnAnimal(Transform animalTransform)
    {
        if (!isExpanded || mainCamera == null) return; // 화면이 꽉 찬 상태(Expanded)일 때만 작동

        targetAnimal = animalTransform;

        // 기존 카메라 연출(Tweener) 중단
        mainCamera.DOKill();

        // 1. 카메라 줌인 (OrthoSize 감소)
        mainCamera.DOOrthoSize(zoomInSize, 0.6f).SetEase(Ease.OutCubic);

        // 2. 동물의 위치로 카메라 이동 후 팔로우 활성화
        Vector3 targetPos = new Vector3(targetAnimal.position.x, targetAnimal.position.y, camOriginalPos.z) + followOffset;
        mainCamera.transform.DOMove(targetPos, 0.6f).SetEase(Ease.OutCubic).OnComplete(() =>
        {
            isFollowing = true; // 이동 연출이 끝난 후 LateUpdate에서 실시간 추적 시작
        });

        // 감상 모드 종료 버튼 활성화
        if (btnUnfocus != null)
        {
            btnUnfocus.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 팔로우 모드를 종료하고 원래 화면 구도로 복귀
    /// </summary>
    public void UnfocusAnimal()
    {
        isFollowing = false;
        targetAnimal = null;

        if (mainCamera == null) return;

        mainCamera.DOKill();

        // 원래 카메라 위치와 크기로 복원 연출
        mainCamera.DOOrthoSize(camOriginalSize, 0.5f).SetEase(Ease.InOutQuad);
        mainCamera.transform.DOMove(camOriginalPos, 0.5f).SetEase(Ease.InOutQuad);

        // 해제 버튼 비활성화
        if (btnUnfocus != null)
        {
            btnUnfocus.gameObject.SetActive(false);
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

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }
    }

    public void SetMinimizedScreen()
    {
        UnfocusAnimal();
        isExpanded = false;
        CloseAllPopups(); // 축소 상태가 될 때는 예외적으로 모든 팝업 닫기

        expandedPanel.SetActive(false);
        minimizedPanel.SetActive(true);

        if (villageOrigin != null)
        {
            savedDraggedPosition = villageOrigin.position;
            villageOrigin.DOMove(originalVillagePos, 0.5f).SetEase(Ease.InOutQuad);
        }

        if (mainCamera != null && miniVillagePos != null)
        {
            float targetSize = camOriginalSize / 0.3f;
            mainCamera.DOOrthoSize(targetSize, 0.5f).SetEase(Ease.InOutQuad);

            Vector3 targetCamPos = miniVillagePos.position + (camOriginalPos - originalVillagePos);
            mainCamera.transform.DOMove(targetCamPos, 0.5f).SetEase(Ease.InOutQuad);
        }

        for (int i = 0; i < bottomIcons.Length; i++)
        {
            if (bottomIcons[i] == null) continue;
            bottomIcons[i].anchoredPosition = iconOriginalPositions[i] + new Vector2(0, -400f);
        }
    }

    public bool GetIsExpanded()
    {
        return isExpanded;
    }

    public void SetExpandedScreen()
    {
        isExpanded = true;

        expandedPanel.SetActive(true);
        minimizedPanel.SetActive(false);
        if (ticketNotification) ticketNotification.SetActive(false);
        ticketTimer = 0f;

        if (villageOrigin != null)
        {
            villageOrigin.DOMove(savedDraggedPosition, 0.5f).SetEase(Ease.InOutQuad);
        }

        if (mainCamera != null)
        {
            mainCamera.DOOrthoSize(camOriginalSize, 0.5f).SetEase(Ease.InOutQuad);
            mainCamera.transform.DOMove(camOriginalPos, 0.5f).SetEase(Ease.InOutQuad);
        }

        AnimateIcons();
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
            float startY = iconOriginalPositions[i].y - 300f;
            bottomIcons[i].anchoredPosition = new Vector2(iconOriginalPositions[i].x, startY);
            bottomIcons[i].DOAnchorPosY(iconOriginalPositions[i].y, 0.6f).SetEase(Ease.OutBounce).SetDelay(0.2f + (i * 0.15f));
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