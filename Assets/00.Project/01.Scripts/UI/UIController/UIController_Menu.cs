using UI;
using UnityEngine;
using UnityEngine.UI;

public class UIController_Menu : MonoBehaviour
{
    [Header("메뉴 목록 버튼")]
    [Tooltip("동물 인벤토리 오픈")]
    [SerializeField] private Button animalInventoryButton;
    [Tooltip("도구 인벤토리 오픈")]
    [SerializeField] private Button toolInventoryButton;
    [Tooltip("뽑기 패널 오픈")]
    [SerializeField] private Button gachaButton;
    [Tooltip("동물 도감 오픈")]
    [SerializeField] private Button animalDexButton;
    [Tooltip("옵션 패널 오픈")]
    [SerializeField] private Button optionButton;
    [Tooltip("화면 축소 버튼")]
    [SerializeField] private Button minimizeButton;

    [Header("오픈할 UI 패널")]
    [SerializeField] private UIPanelWindow animalInventoryPanel;
    [SerializeField] private UIPanelWindow toolInventoryPanel;
    [SerializeField] private UIPanelWindow gachaPanel;
    [SerializeField] private UIPanelWindow animalDexPanel;
    [SerializeField] private UIPanelWindow optionPanel;

    [Header("인벤토리 UI 동기화")]
    [Tooltip("동물 Inv 패널 오픈 시 상세 닫기 + SyncAllSlots 호출 대상")]
    [SerializeField] private UIController_AnimalInv animalInvUI;
    [Tooltip("도구 Inv 패널 오픈 시 SyncAllSlots 호출 대상")]
    [SerializeField] private UIController_ToolInv toolInvUI;
    [Tooltip("동물 도감 패널 오픈 시 상세 닫기 등 오픈 처리 호출 대상")]
    [SerializeField] private UIController_AnimalDex animalDexUI;

    [Header("패널 오픈시 초기 위치 고정")]
    [SerializeField] private bool usePanelOpenDefaultPosition;

    [Header("패널 배타 오픈")]
    [Tooltip("true면 한 번에 하나의 패널만 열고, false면 기존처럼 독립 토글")]
    [SerializeField] private bool useTradeOffSetting;

    private void Awake()
    {
        if (animalInvUI == null)
            animalInvUI = GetComponent<UIController_AnimalInv>();

        if (toolInvUI == null)
            toolInvUI = GetComponent<UIController_ToolInv>();

        if (animalDexUI == null)
            animalDexUI = GetComponent<UIController_AnimalDex>();

        animalInventoryButton.onClick.AddListener(() => TogglePanel(animalInventoryPanel));
        toolInventoryButton.onClick.AddListener(() => TogglePanel(toolInventoryPanel));
        gachaButton.onClick.AddListener(() => TogglePanel(gachaPanel));
        animalDexButton.onClick.AddListener(() => TogglePanel(animalDexPanel));
        optionButton.onClick.AddListener(() => TogglePanel(optionPanel));

        // GameMasterManager의 축소와 별개로, 같은 Minimize 버튼에 메뉴 패널 닫기를 추가 연결
        if (minimizeButton != null)
            minimizeButton.onClick.AddListener(CloseAllPanels);
    }

    /// <summary>
    /// 화면 축소 등에서 열린 메뉴 패널(UIPanelWindow)을 모두 닫습니다.
    /// </summary>
    public void CloseAllPanels()
    {
        CloseAllExcept(null);
    }

    private void TogglePanel(UIPanelWindow panel)
    {
        if (panel == null)
        {
            Debug.LogWarning("[UIController_Menu] 연결되지 않은 패널이 있습니다.");
            return;
        }

        // 배타 모드: 닫힌 패널을 열 때만 다른 패널을 닫음 (같은 버튼 재클릭은 토글 유지)
        if (useTradeOffSetting && !panel.gameObject.activeSelf)
            CloseAllExcept(panel);

        if (usePanelOpenDefaultPosition)
            panel.TogglePanelDefaultPosition();
        else
            panel.TogglePanelSetPosition();

        // Inv 패널이 열린 직후, 비활성 중 누적된 인벤 데이터를 UI에 반영
        // 닫힌 경우(동물 Inv)에는 상세 페이지도 함께 닫음
        if (panel.gameObject.activeSelf)
            SyncInventoryIfNeeded(panel);
        else
            NotifyPanelClosedIfNeeded(panel);
    }

    private void SyncInventoryIfNeeded(UIPanelWindow panel)
    {
        if (panel == animalInventoryPanel)
            animalInvUI?.NotifyPanelOpened();
        else if (panel == toolInventoryPanel)
            toolInvUI?.SyncAllSlots();
        else if (panel == animalDexPanel)
            animalDexUI?.NotifyPanelOpened();
    }

    /// <summary>
    /// 패널이 닫힌 뒤 필요한 UI 정리. Dex는 현재 유지(호출하지 않음).
    /// </summary>
    private void NotifyPanelClosedIfNeeded(UIPanelWindow panel)
    {
        if (panel == animalInventoryPanel)
            animalInvUI?.NotifyPanelClosed();
    }

    private void CloseAllExcept(UIPanelWindow keepOpen)
    {
        CloseIfOther(animalInventoryPanel, keepOpen);
        CloseIfOther(toolInventoryPanel, keepOpen);
        CloseIfOther(gachaPanel, keepOpen);
        CloseIfOther(animalDexPanel, keepOpen);
        CloseIfOther(optionPanel, keepOpen);
    }

    private void CloseIfOther(UIPanelWindow panel, UIPanelWindow keepOpen)
    {
        if (panel == null || panel == keepOpen)
            return;

        if (!panel.gameObject.activeSelf)
            return;

        panel.ClosePanel();
        NotifyPanelClosedIfNeeded(panel);
    }
}
