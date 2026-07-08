using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class GameMasterManager : MonoBehaviour
{
    [Header("3D 오브젝트 셋팅")]
    public Transform villageOrigin;
    public Transform miniVillagePos;
    private Vector3 originalVillagePos;
    private Vector3 originalVillageScale;

    [Header("메인 패널 참조")]
    public GameObject expandedPanel;
    public GameObject minimizedPanel;
    public GameObject menuPanel;
    public CanvasGroup menuCanvasGroup;

    [Header("독립 하단 팝업 패널들 (RectTransform)")]
    public RectTransform panelDex;
    public RectTransform panelGacha;
    public RectTransform panelManage;

    // 각 패널이 에디터에 배치된 '정상 열린 위치'를 기억할 변수들
    private Vector2 posDexOpen;
    private Vector2 posGachaOpen;
    private Vector2 posManageOpen;

    [Header("전환 및 시스템 버튼들")]
    public Button btnMinimize;
    public Button btnMaximize;
    public Button btnQuit;
    public Button btnCloseMenu;

    [Header("하단 메인 아이콘들 (통통 튀는 등장용)")]
    public RectTransform[] bottomIcons;
    private Vector2[] iconOriginalPositions;

    [Header("티켓 알림 설정")]
    public GameObject ticketNotification;
    private float ticketTimer = 0f;
    private const float TICKET_COOLDOWN = 1800f;

    private bool isExpanded = true;
    private bool isMenuOpen = false;
    private bool isMenuAnimating = false;

    void Start()
    {
        // 1. 3D 마을 초기 위치 저장
        if (villageOrigin != null)
        {
            originalVillagePos = villageOrigin.position;
            originalVillageScale = villageOrigin.localScale;
        }

        // 2. 하단 메인 버튼들의 에디터 배치 위치 기억
        iconOriginalPositions = new Vector2[bottomIcons.Length];
        for (int i = 0; i < bottomIcons.Length; i++)
        {
            if (bottomIcons[i] == null) continue;
            iconOriginalPositions[i] = bottomIcons[i].anchoredPosition;
        }

        // 3.각 패널의 숨김 위치를 좌/우/하단으로 각각 다르게 설정
        if (panelDex != null)
        {
            posDexOpen = panelDex.anchoredPosition;
            // 도감: 왼쪽 밖(-2000f)으로 숨김
            panelDex.anchoredPosition = new Vector2(posDexOpen.x - 2000f, posDexOpen.y);
        }
        if (panelGacha != null)
        {
            posGachaOpen = panelGacha.anchoredPosition;
            // 뽑기: 아래쪽(-1200f)으로 숨김
            panelGacha.anchoredPosition = new Vector2(posGachaOpen.x, posGachaOpen.y - 1200f);
        }
        if (panelManage != null)
        {
            posManageOpen = panelManage.anchoredPosition;
            // 동물관리: 오른쪽 밖(+2000f)으로 숨김
            panelManage.anchoredPosition = new Vector2(posManageOpen.x + 2000f, posManageOpen.y);
        }

        // 4. 시스템 기본 버튼 이벤트 연결
        if (btnMinimize) btnMinimize.onClick.AddListener(SetMinimizedScreen);
        if (btnMaximize) btnMaximize.onClick.AddListener(SetExpandedScreen);
        if (btnQuit) btnQuit.onClick.AddListener(QuitGame);
        if (btnCloseMenu) btnCloseMenu.onClick.AddListener(ToggleMenu);

        // 5. 초기 화면 세팅
        expandedPanel.SetActive(true);
        minimizedPanel.SetActive(false);
        menuPanel.SetActive(false);
        menuCanvasGroup.alpha = 0f;

        // 6. 하단 아이콘 통통 등장
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

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }
    }
    // [1] 도감 팝업 (왼쪽 ↔ 중앙)
    public void OpenDex()
    {
        CloseAllPopups();
        // DOAnchorPosX를 사용하여 X축으로 부드럽게 들어옴
        if (panelDex != null) panelDex.DOAnchorPosX(posDexOpen.x, 0.4f).SetEase(Ease.OutQuad);
    }
    public void CloseDex()
    {
        if (panelDex != null) panelDex.DOAnchorPosX(posDexOpen.x - 2000f, 0.4f).SetEase(Ease.InQuad);
    }

    // [2] 뽑기 팝업 (기존 유지: 아래 ↔ 위)
    public void OpenGacha()
    {
        CloseAllPopups();
        if (panelGacha != null) panelGacha.DOAnchorPosY(posGachaOpen.y, 0.4f).SetEase(Ease.OutQuad);
    }
    public void CloseGacha()
    {
        if (panelGacha != null) panelGacha.DOAnchorPosY(posGachaOpen.y - 1200f, 0.4f).SetEase(Ease.InQuad);
    }

    // [3] 동물관리 팝업 (오른쪽 ↔ 중앙)
    public void OpenManage()
    {
        CloseAllPopups();
        // DOAnchorPosX를 사용하여 X축으로 부드럽게 들어옴
        if (panelManage != null) panelManage.DOAnchorPosX(posManageOpen.x, 0.4f).SetEase(Ease.OutQuad);
    }
    public void CloseManage()
    {
        if (panelManage != null) panelManage.DOAnchorPosX(posManageOpen.x + 2000f, 0.4f).SetEase(Ease.InQuad);
    }

    // 모든 팝업을 각자의 숨김 위치로 빠르게 치우는 함수
    private void CloseAllPopups()
    {
        if (panelDex != null) panelDex.DOAnchorPosX(posDexOpen.x - 2000f, 0.2f);
        if (panelGacha != null) panelGacha.DOAnchorPosY(posGachaOpen.y - 1200f, 0.2f);
        if (panelManage != null) panelManage.DOAnchorPosX(posManageOpen.x + 2000f, 0.2f);
    }

    // 기존의 화면 전환 및 등장 연출 (유지)
    public void SetMinimizedScreen()
    {
        isExpanded = false;
        CloseAllPopups();

        expandedPanel.SetActive(false);
        minimizedPanel.SetActive(true);

        if (villageOrigin != null && miniVillagePos != null)
        {
            villageOrigin.DOMove(miniVillagePos.position, 0.5f).SetEase(Ease.InOutQuad);
            villageOrigin.DOScale(originalVillageScale * 0.3f, 0.5f).SetEase(Ease.InOutQuad);
        }

        for (int i = 0; i < bottomIcons.Length; i++)
        {
            if (bottomIcons[i] == null) continue;
            bottomIcons[i].anchoredPosition = iconOriginalPositions[i] + new Vector2(0, -400f);
        }
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
            villageOrigin.DOMove(originalVillagePos, 0.5f).SetEase(Ease.InOutQuad);
            villageOrigin.DOScale(originalVillageScale, 0.5f).SetEase(Ease.InOutQuad);
        }

        AnimateIcons();
    }

    private void AnimateIcons()
    {
        for (int i = 0; i < bottomIcons.Length; i++)
        {
            if (bottomIcons[i] == null) continue;
            float startY = iconOriginalPositions[i].y - 300f;
            bottomIcons[i].anchoredPosition = new Vector2(iconOriginalPositions[i].x, startY);
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
            menuCanvasGroup.DOFade(0f, 0.25f).SetUpdate(true).OnComplete(() => {
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
}