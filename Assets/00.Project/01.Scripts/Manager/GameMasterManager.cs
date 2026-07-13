// NB
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Unity.AI.Navigation;
using UnityEngine.AI;
using System.Collections.Generic;

public class GameMasterManager : MonoBehaviour
{
    [Header("3D 오브젝트 및 카메라 설정")]
    public Transform villageOrigin;
    private Camera mainCamera;

    private Vector3 camOriginalPos;
    private float camOriginalSize;

    //마을의 최초 위치 및 유저가 드래그한 위치를 기억할 변수
    private Vector3 originalVillagePos;
    private Vector3 savedDraggedPosition;

    [Header("축소 화면용 카메라 타겟 셋팅")]
    public Transform miniVillagePos;

    [Header("메인 패널 참조")]
    public GameObject expandedPanel;
    public GameObject minimizedPanel;
    public GameObject menuPanel;
    public CanvasGroup menuCanvasGroup;

    [Header("독립 하단 팝업 패널들 (RectTransform)")]
    public RectTransform panelDex;
    public RectTransform panelGacha;
    public RectTransform panelManage;

    private Vector2 posDexOpen;
    private Vector2 posGachaOpen;
    private Vector2 posManageOpen;

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

    private bool isExpanded = true;
    private bool isMenuOpen = false;
    private bool isMenuAnimating = false;

    void Start()
    {
        // 메인 카메라 캐싱 및 초기 값 저장
        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            camOriginalPos = mainCamera.transform.position;
            camOriginalSize = mainCamera.orthographicSize;
        }

        //마을의 최초 원본 위치 저장 및 드래그 위치 초기화
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

        // 팝업 오프셋 설정
        if (panelDex != null) { posDexOpen = panelDex.anchoredPosition; panelDex.anchoredPosition = new Vector2(posDexOpen.x - 2000f, posDexOpen.y); }
        if (panelGacha != null) { posGachaOpen = panelGacha.anchoredPosition; panelGacha.anchoredPosition = new Vector2(posGachaOpen.x, posGachaOpen.y - 1200f); }
        if (panelManage != null) { posManageOpen = panelManage.anchoredPosition; panelManage.anchoredPosition = new Vector2(posManageOpen.x + 2000f, posManageOpen.y); }

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

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }
    }

    // [축소 화면 상태로 전환]
    public void SetMinimizedScreen()
    {
        isExpanded = false;
        CloseAllPopups();

        expandedPanel.SetActive(false);
        minimizedPanel.SetActive(true);

        if (villageOrigin != null)
        {
            // 1. 현재 유저가 드래그해 놓은 마지막 위치를 저장
            savedDraggedPosition = villageOrigin.position;

            // 2. 마을을 카메라 연출 규격에 맞는 '최초 위치(원점)'로 부드럽게 복귀
            villageOrigin.DOMove(originalVillagePos, 0.5f).SetEase(Ease.InOutQuad);
        }

        if (mainCamera != null && miniVillagePos != null)
        {
            //1.카메라 배율 설정
            float targetSize = camOriginalSize / 0.3f;
            mainCamera.DOOrthoSize(targetSize, 0.5f).SetEase(Ease.InOutQuad);

            //마을이 최초 위치(originalVillagePos)로 가므로 카메라 타겟 계산도 안정적으로 고정
            Vector3 targetCamPos = miniVillagePos.position + (camOriginalPos - originalVillagePos);
            mainCamera.transform.DOMove(targetCamPos, 0.5f).SetEase(Ease.InOutQuad);
        }

        for (int i = 0; i < bottomIcons.Length; i++)
        {
            if (bottomIcons[i] == null) continue;
            bottomIcons[i].anchoredPosition = iconOriginalPositions[i] + new Vector2(0, -400f);
        }
    }

    //축소화면일때 드래그 잠금 기능
    public bool GetIsExpanded()
    {
        return isExpanded;
    }

    // [확장 화면 상태로 복귀]
    public void SetExpandedScreen()
    {
        isExpanded = true;

        expandedPanel.SetActive(true);
        minimizedPanel.SetActive(false);
        if (ticketNotification) ticketNotification.SetActive(false);
        ticketTimer = 0f;

        if (villageOrigin != null)
        {
            // 다시 확대될 때는 유저가 원래 드래그해서 배치해 두었던 위치로 마을을 돌려놓기
            villageOrigin.DOMove(savedDraggedPosition, 0.5f).SetEase(Ease.InOutQuad);
        }

        if (mainCamera != null)
        {
            mainCamera.DOOrthoSize(camOriginalSize, 0.5f).SetEase(Ease.InOutQuad);
            mainCamera.transform.DOMove(camOriginalPos, 0.5f).SetEase(Ease.InOutQuad);
        }

        AnimateIcons();
    }


    #region UI 및 시스템 팝업 로직 (기존 유지)
    public void OpenDex() { CloseAllPopups(); if (panelDex != null) panelDex.DOAnchorPosX(posDexOpen.x, 0.4f).SetEase(Ease.OutQuad); }
    public void CloseDex() { if (panelDex != null) panelDex.DOAnchorPosX(posDexOpen.x - 2000f, 0.4f).SetEase(Ease.InQuad); }
    public void OpenGacha() { CloseAllPopups(); if (panelGacha != null) panelGacha.DOAnchorPosY(posGachaOpen.y, 0.4f).SetEase(Ease.OutQuad); }
    public void CloseGacha() { if (panelGacha != null) panelGacha.DOAnchorPosY(posGachaOpen.y - 1200f, 0.4f).SetEase(Ease.InQuad); }
    public void OpenManage() { CloseAllPopups(); if (panelManage != null) panelManage.DOAnchorPosX(posManageOpen.x, 0.4f).SetEase(Ease.OutQuad); }
    public void CloseManage() { if (panelManage != null) panelManage.DOAnchorPosX(posManageOpen.x + 2000f, 0.4f).SetEase(Ease.InQuad); }

    private void CloseAllPopups()
    {
        if (panelDex != null) panelDex.DOAnchorPosX(posDexOpen.x - 2000f, 0.2f);
        if (panelGacha != null) panelGacha.DOAnchorPosY(posGachaOpen.y - 1200f, 0.2f);
        if (panelManage != null) panelManage.DOAnchorPosX(posManageOpen.x + 2000f, 0.2f);
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
