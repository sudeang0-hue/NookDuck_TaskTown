using UI;
using UnityEngine;
using UnityEngine.UI;

public class UIController_Menu : MonoBehaviour
{
    [Header("메뉴 목록 버튼")]
    //[Tooltip("동물 인벤토리 오픈")]
    //[SerializeField] private Button animalInventoryButton;
    //[Tooltip("도구 인벤토리 오픈")]
    //[SerializeField] private Button toolInventoryButton;
    [Tooltip("인벤토리 오픈")]
    [SerializeField] private Button inventoryButton;
    [Tooltip("뽑기 패널 오픈")]
    [SerializeField] private Button gachaButton;
    [Tooltip("동물 도감 오픈")]
    [SerializeField] private Button animalDexButton;
    [Tooltip("마을 업그레이드 오픈")]
    [SerializeField] private Button villageButton;
    [Tooltip("옵션 패널 오픈")]
    [SerializeField] private Button optionButton;
    [Tooltip("화면 축소 버튼")]
    [SerializeField] private Button minimizeButton;

    [Header("오픈할 UI 패널")]
    private UIPanelWindow animalInventoryPanel;
    private UIPanelWindow toolInventoryPanel;
    private UIPanelWindow gachaPanel;
    private UIPanelWindow animalDexPanel;
    //private UIPanelWindow villagePanel;
    private UIPanelWindow optionPanel;

    [Header("UI 동기화")]
    [Tooltip("동물 Inv 패널 오픈/닫기 시 상세 닫기 + SyncAllSlots 호출 대상")]
    [SerializeField] private UIController_AnimalInv animalInvUI;
    [Tooltip("도구 Inv 패널 오픈/닫기 시 상세 닫기 + SyncAllSlots 호출 대상")]
    [SerializeField] private UIController_ToolInv toolInvUI;
    [Tooltip("동물 도감 패널 오픈 시 상세 닫기 등 오픈 처리 호출 대상")]
    [SerializeField] private UIController_AnimalDex animalDexUI;
    [Tooltip("뽑기 패널 오픈 시 가격 텍스트 갱신 호출 대상")]
    [SerializeField] private UIController_Gacha gachaUI;
    //[Tooltip("마을 패널 오픈 시 텍스트 갱신 호출 대상")]
    //[SerializeField] private UIController_VillageUpgrade villageUI;

    [Header("탭 전환 Manager")]
    [SerializeField] private InventoryTabUI_Manager inventoryTabManager;
    [SerializeField] private TownTabUI_Manager townTabManager;

    [Header("패널 오픈시 초기 위치 고정")]
    [SerializeField] private bool usePanelOpenDefaultPosition;

    [Header("패널 배타 오픈")]
    [Tooltip("true면 한 번에 하나의 패널만 열고, false면 기존처럼 독립 토글")]
    [SerializeField] private bool useTradeOffSetting;

    private void Awake()
    {
        UIControllerNullRefrerenceBind();

        //animalInventoryButton.onClick.AddListener(() => TogglePanel(animalInventoryPanel));
        //toolInventoryButton.onClick.AddListener(() => TogglePanel(toolInventoryPanel));
        if(inventoryButton != null)
        { 
            inventoryButton.onClick.AddListener(OnInventoryButtonClicked);
        }

        gachaButton.onClick.AddListener(() => TogglePanel(gachaPanel));
        animalDexButton.onClick.AddListener(() => TogglePanel(animalDexPanel));
        optionButton.onClick.AddListener(() => TogglePanel(optionPanel));

        if (villageButton != null)
        {
            villageButton.onClick.AddListener(OnVillageButtonClicked);
        }

        // GameMasterManager의 축소와 별개로, 같은 Minimize 버튼에 메뉴 패널 닫기를 추가 연결
        if (minimizeButton != null)
        {
            minimizeButton.onClick.AddListener(CloseAllPanels);
        }

        var windows = FindObjectsByType<UIPanelWindow>(
    FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var w in windows)
        {
            //if (animalInventoryPanel == null && w.MenuType == GameMenuType.AnimalInv)
            //    animalInventoryPanel = w;

            //if (toolInventoryPanel == null && w.MenuType == GameMenuType.ToolInv)
            //    toolInventoryPanel = w;

            if (gachaPanel == null && w.MenuType == GameMenuType.Gacha)
                gachaPanel = w;

            if (animalDexPanel == null && w.MenuType == GameMenuType.AnimalDex)
                animalDexPanel = w;

            if (optionPanel == null && w.MenuType == GameMenuType.Option)
                optionPanel = w;
            
            //if (villagePanel == null && w.MenuType == GameMenuType.Villiage)
            //    villagePanel = w;
        }
    }


    private void OnEnable()
    {
        TargetSelector.OnTargetSelected += OnCameraTargetSelected;
    }

    private void OnDisable()
    {
        TargetSelector.OnTargetSelected -= OnCameraTargetSelected;
    }


    private void UIControllerNullRefrerenceBind()
    {
        if (animalInvUI == null)
            animalInvUI = GetComponent<UIController_AnimalInv>();

        if (toolInvUI == null)
            toolInvUI = GetComponent<UIController_ToolInv>();

        if (animalDexUI == null)
            animalDexUI = GetComponent<UIController_AnimalDex>();

        if (gachaUI == null)
            gachaUI = GetComponent<UIController_Gacha>();

        if (inventoryTabManager == null)
            inventoryTabManager = GetComponent<InventoryTabUI_Manager>();

        if (townTabManager == null)
            townTabManager = GetComponent<TownTabUI_Manager>();

    }


    /// <summary>
    /// 카메라 타겟 선택(시점 전환) 시 열린 메뉴 패널을 닫습니다.
    /// 빈 공간 클릭(null)은 무시합니다.
    /// </summary>
    private void OnCameraTargetSelected(Transform target)
    {
        if (target == null)
            return;

        CloseAllPanels();
    }
   

    /// <summary>
    /// 화면 축소 등에서 열린 메뉴 패널(UIPanelWindow)을 모두 닫습니다.
    /// </summary>
    public void CloseAllPanels()
    {
        CloseAllExcept(null);
        inventoryTabManager?.CloseAllTabs();
        townTabManager?.CloseAllTabs();
    }

    private void TogglePanel(UIPanelWindow panel)
    {
        if (panel == null)
        {
            Debug.LogWarning("[UIController_Menu] 연결되지 않은 패널이 있습니다.");
            return;
        }

        bool willOpen = !panel.gameObject.activeSelf;

        // 다른 메뉴 패널을 열면 마을 관련 패널을 모두 닫음
        if (willOpen)
            townTabManager?.CloseAllTabs();

        if (willOpen)
            inventoryTabManager?.CloseAllTabs();

        // 배타 모드: 닫힌 패널을 열 때만 다른 패널을 닫음 (같은 버튼 재클릭은 토글 유지)
        if (useTradeOffSetting && willOpen)
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
            toolInvUI?.NotifyPanelOpened();
        else if (panel == animalDexPanel)
            animalDexUI?.NotifyPanelOpened();
        else if (panel == gachaPanel)
            gachaUI?.NotifyPanelOpened();
    }

    /// <summary>
    /// 패널이 닫힌 뒤 필요한 UI 정리. Dex는 현재 유지(호출하지 않음).
    /// Animal / Tool Inv는 상세 페이지(Animal_Inv_Page / Tool_Inv_Page)도 함께 닫습니다.
    /// </summary>
    private void NotifyPanelClosedIfNeeded(UIPanelWindow panel)
    {
        if (panel == animalInventoryPanel)
            animalInvUI?.NotifyPanelClosed();
        else if (panel == toolInventoryPanel)
            toolInvUI?.NotifyPanelClosed();
    }

    private void CloseAllExcept(UIPanelWindow keepOpen)
    {
        //CloseIfOther(animalInventoryPanel, keepOpen);
        //CloseIfOther(toolInventoryPanel, keepOpen);
        CloseIfOther(gachaPanel, keepOpen);
        CloseIfOther(animalDexPanel, keepOpen);
        //CloseIfOther(villagePanel, keepOpen);
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


    private void OnVillageButtonClicked()
    {
        if (townTabManager == null)
        {
            Debug.LogWarning("[UIController_Menu] TownTabManager가 연결되지 않았습니다.");

            return;
        }

        // 닫힌 상태에서 새로 열 때만 다른 메뉴를 닫음 (같은 버튼 재클릭은 토글 유지)
        bool willOpen = !townTabManager.IsAnyTabOpen;
        if (willOpen)
        {
            inventoryTabManager?.CloseAllTabs();

            if (useTradeOffSetting)
                CloseAllExcept(null);
        }

        townTabManager.ToggleTownTabs();
    }

    private void OnInventoryButtonClicked()
    {
        if (inventoryTabManager == null)
        {
            Debug.LogWarning("[UIController_Menu] InventoryTabManager 연결되지 않았습니다.");

            return;
        }

        // 닫힌 상태에서 새로 열 때만 다른 메뉴를 닫음 (같은 버튼 재클릭은 토글 유지)
        bool willOpen = !inventoryTabManager.IsAnyTabOpen;
        if (willOpen)
        {
            townTabManager?.CloseAllTabs();

            if (useTradeOffSetting)
                CloseAllExcept(null);
        }

        inventoryTabManager.ToggleInventoryTabs();
    }


}
