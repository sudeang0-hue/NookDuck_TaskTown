//NB
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Unity.AI.Navigation;
using UnityEngine.AI;
using System.Collections.Generic; 

public class GameMasterManager : MonoBehaviour
{
    [Header("3D 오브젝트 셋팅")]
    public Transform villageOrigin;
    public Transform miniVillagePos;
    public NavMeshSurface navMeshSurface;
    private Vector3 originalVillagePos;
    private Vector3 originalVillageScale;

    private Dictionary<NavMeshAgent, Vector3> recordedLocalPositions = new Dictionary<NavMeshAgent, Vector3>();

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
        if (villageOrigin != null)
        {
            originalVillagePos = villageOrigin.position;
            originalVillageScale = villageOrigin.localScale;
        }

        iconOriginalPositions = new Vector2[bottomIcons.Length];
        for (int i = 0; i < bottomIcons.Length; i++)
        {
            if (bottomIcons[i] == null) continue;
            iconOriginalPositions[i] = bottomIcons[i].anchoredPosition;
        }

        if (panelDex != null)
        {
            posDexOpen = panelDex.anchoredPosition;
            panelDex.anchoredPosition = new Vector2(posDexOpen.x - 2000f, posDexOpen.y);
        }
        if (panelGacha != null)
        {
            posGachaOpen = panelGacha.anchoredPosition;
            panelGacha.anchoredPosition = new Vector2(posGachaOpen.x, posGachaOpen.y - 1200f);
        }
        if (panelManage != null)
        {
            posManageOpen = panelManage.anchoredPosition;
            panelManage.anchoredPosition = new Vector2(posManageOpen.x + 2000f, posManageOpen.y);
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

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }
    }

    public void OpenDex()
    {
        CloseAllPopups();
        if (panelDex != null) panelDex.DOAnchorPosX(posDexOpen.x, 0.4f).SetEase(Ease.OutQuad);
    }
    public void CloseDex()
    {
        if (panelDex != null) panelDex.DOAnchorPosX(posDexOpen.x - 2000f, 0.4f).SetEase(Ease.InQuad);
    }

    public void OpenGacha()
    {
        CloseAllPopups();
        if (panelGacha != null) panelGacha.DOAnchorPosY(posGachaOpen.y, 0.4f).SetEase(Ease.OutQuad);
    }
    public void CloseGacha()
    {
        if (panelGacha != null) panelGacha.DOAnchorPosY(posGachaOpen.y - 1200f, 0.4f).SetEase(Ease.InQuad);
    }

    public void OpenManage()
    {
        CloseAllPopups();
        if (panelManage != null) panelManage.DOAnchorPosX(posManageOpen.x, 0.4f).SetEase(Ease.OutQuad);
    }
    public void CloseManage()
    {
        if (panelManage != null) panelManage.DOAnchorPosX(posManageOpen.x + 2000f, 0.4f).SetEase(Ease.InQuad);
    }

    private void CloseAllPopups()
    {
        if (panelDex != null) panelDex.DOAnchorPosX(posDexOpen.x - 2000f, 0.2f);
        if (panelGacha != null) panelGacha.DOAnchorPosY(posGachaOpen.y - 1200f, 0.2f);
        if (panelManage != null) panelManage.DOAnchorPosX(posManageOpen.x + 2000f, 0.2f);
    }

    public void SetMinimizedScreen()
    {
        isExpanded = false;
        CloseAllPopups();

        expandedPanel.SetActive(false);
        minimizedPanel.SetActive(true);

        // 이동 전에 현재 마을 기준의 상대 좌표를 싹 백업
        PrepareAgentsForTransition();

        if (villageOrigin != null && miniVillagePos != null)
        {
            villageOrigin.DOMove(miniVillagePos.position, 0.5f).SetEase(Ease.InOutQuad);
            villageOrigin.DOScale(originalVillageScale * 0.3f, 0.5f).SetEase(Ease.InOutQuad)
                .OnComplete(() => StartCoroutine(RebakeAndEnableAgentsRoutine(0.3f)));
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

        // 이동 전에 현재 마을 기준의 상대 좌표를 싹 백업
        PrepareAgentsForTransition();

        if (villageOrigin != null)
        {
            villageOrigin.DOMove(originalVillagePos, 0.5f).SetEase(Ease.InOutQuad);
            villageOrigin.DOScale(originalVillageScale, 0.5f).SetEase(Ease.InOutQuad)
                .OnComplete(() => StartCoroutine(RebakeAndEnableAgentsRoutine(1.0f)));
        }

        AnimateIcons();
    }

    //전환 전 주민들의 상대적 위치 백업
    private void PrepareAgentsForTransition()
    {
        recordedLocalPositions.Clear();
        if (villageOrigin == null) return;

        NavMeshAgent[] agents = Object.FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None);
        foreach (var agent in agents)
        {
            if (agent == null) continue;
            agent.enabled = false;

            // 이 시점의 마을을 기준으로 주민이 몇 미터 거리에 서 있는지 로컬 위치를 기억
            Vector3 localPos = villageOrigin.InverseTransformPoint(agent.transform.position);
            recordedLocalPositions[agent] = localPos;
        }
    }

    private System.Collections.IEnumerator RebakeAndEnableAgentsRoutine(float scale)
    {
        if (navMeshSurface != null)
        {
            navMeshSurface.BuildNavMesh();
        }

        // 유니티 엔진이 새 길을 완벽히 인식하도록 1프레임 대기
        yield return null;

        // 딕셔너리에 저장해둔 백업 데이터를 기반으로 주민들을 정밀 복구
        foreach (var pair in recordedLocalPositions)
        {
            NavMeshAgent agent = pair.Key;
            Vector3 storedLocalPos = pair.Value;

            if (agent == null) continue;

            agent.radius = 0.5f * scale;
            agent.height = 2.0f * scale;
            agent.speed = 1.5f * scale;
            agent.acceleration = 8f * scale;

            Vector3 expectedWorldPos = villageOrigin.TransformPoint(storedLocalPos);

            // 허공에서 새 마을 바닥 근처로 강제 순간이동
            agent.transform.position = expectedWorldPos;

            NavMeshHit hit;
            float searchRadius = 10f * scale;

            // 예상 위치 근처에서 바닥을 스캔하여 완벽히 밀착
            if (NavMesh.SamplePosition(expectedWorldPos, out hit, searchRadius, NavMesh.AllAreas))
            {
                agent.transform.position = hit.position;
                agent.enabled = true;
                agent.Warp(hit.position);
                agent.ResetPath();
            }
            else
            {
                Debug.LogWarning($"[NavMesh] {agent.name} 바닥을 여전히 못 찾음! 위치: {expectedWorldPos}");
            }
        }
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