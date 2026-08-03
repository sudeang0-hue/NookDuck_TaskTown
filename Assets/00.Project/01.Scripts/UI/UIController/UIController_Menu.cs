using System.Collections.Generic;
using UI;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 메인 메뉴 버튼 허브. GameMenuType 기준으로 한 번에 하나의 메인 메뉴만 엽니다.
/// LevelUpPopup / None은 배타 대상에서 제외합니다.
/// </summary>
public class UIController_Menu : MonoBehaviour
{
    [Header("메뉴 목록 버튼")]
    [Tooltip("인벤토리 오픈")]
    [SerializeField] private Button inventoryButton;
    [Tooltip("뽑기 패널 오픈")]
    [SerializeField] private Button gachaButton;
    [Tooltip("도감 오픈")]
    [SerializeField] private Button animalDexButton;
    [Tooltip("마을 업그레이드 오픈")]
    [SerializeField] private Button villageButton;
    [Tooltip("옵션 패널 오픈")]
    [SerializeField] private Button optionButton;
    [Tooltip("화면 축소 버튼")]
    [SerializeField] private Button minimizeButton;

    [Header("UI 동기화")]
    [Tooltip("뽑기 패널 오픈 시 가격 텍스트 갱신 호출 대상")]
    private UIController_Gacha gachaUI;
    [Tooltip("도감 패널 오픈 시 처리 호출 대상")]
    private UIController_AnimalDex animalDexUI;

    [Header("탭 전환 Manager")]
    private TabUIManager_Inventory inventoryTabManager;
    private TabUIManager_Town townTabManager;
    private TabUIManager_Dex dexTabManager;
    private TabUIManager_Option optionTabManager;

    [Header("패널 오픈시 초기 위치 고정")]
    private bool usePanelOpenDefaultPosition = true;

    [Header("패널 배타 오픈")]
    [Tooltip("true면 한 번에 하나의 메인 메뉴(GameMenuType)만 엽니다.")]
    private bool useTradeOffSetting = true;

    private readonly List<UIPanelWindow> registeredWindows = new List<UIPanelWindow>();
    private UIPanelWindow gachaPanel;
    private UIPanelWindow optionPanel;

    // -----------------------------------------------------------------------------
    // [ 2026.07.28 - Choi - 튜토리얼 축소·확장 단계 연동 ]
    // 기능: Additive 튜토리얼 Scene이 기존 뽑기 메뉴 버튼을 강조할 수 있게 읽기 전용으로 노출합니다.
    // -----------------------------------------------------------------------------
    public Button GachaButton => gachaButton;

    // -----------------------------------------------------------------------------
    // [ 2026.08.03 - Choi - 튜토리얼 단계별 강조 연동 ]
    // 기능: Additive 튜토리얼 Scene에서 인벤토리/마을 메뉴 버튼을 강조 대상으로
    //       사용할 수 있도록 읽기 전용 참조만 공개합니다.
    // -----------------------------------------------------------------------------
    public Button InventoryButton => inventoryButton;
    public Button VillageButton => villageButton;

    private void Awake()
    {
        UIControllerNullRefrerenceBind();
        CachePanelWindows();

        if (inventoryButton != null)
            inventoryButton.onClick.AddListener(OnInventoryButtonClicked);

        if (gachaButton != null)
            gachaButton.onClick.AddListener(OnGachaButtonClicked);

        if (animalDexButton != null)
            animalDexButton.onClick.AddListener(OnDexButtonClicked);

        if (optionButton != null)
            optionButton.onClick.AddListener(OnOptionButtonClicked);

        if (villageButton != null)
            villageButton.onClick.AddListener(OnVillageButtonClicked);

        if (minimizeButton != null)
            minimizeButton.onClick.AddListener(CloseAllPanels);
    }

    private void OnEnable()
    {
        TargetSelector.OnTargetSelected += OnCameraTargetSelected;
    }

    private void OnDisable()
    {
        TargetSelector.OnTargetSelected -= OnCameraTargetSelected;
    }

    /// <summary>
    /// UIController / TabManager 자동 할당
    /// </summary>
    private void UIControllerNullRefrerenceBind()
    {
        // TabManager는 같은 GO에 붙는 경우가 많아 parent와 무관하게 먼저 연결합니다.
        if (inventoryTabManager == null)
            inventoryTabManager = GetComponent<TabUIManager_Inventory>();

        if (townTabManager == null)
            townTabManager = GetComponent<TabUIManager_Town>();

        if (dexTabManager == null)
            dexTabManager = GetComponent<TabUIManager_Dex>();

        if (optionTabManager == null)
            optionTabManager = GetComponent<TabUIManager_Option>();

        if (transform.parent == null)
        {
            Debug.LogWarning($"{name}의 부모 오브젝트를 찾을 수 없어 자식 UI 자동 바인딩을 건너뜁니다.");
        }
        else
        {
            Transform parentTransform = transform.parent;

            if (gachaUI == null)
                gachaUI = parentTransform.GetComponentInChildren<UIController_Gacha>(true);

            if (animalDexUI == null)
                animalDexUI = parentTransform.GetComponentInChildren<UIController_AnimalDex>(true);

            if (dexTabManager == null)
                dexTabManager = parentTransform.GetComponentInChildren<TabUIManager_Dex>(true);

            if (optionTabManager == null)
                optionTabManager = parentTransform.GetComponentInChildren<TabUIManager_Option>(true);
        }

        if (inventoryTabManager == null)
            inventoryTabManager = FindFirstObjectByType<TabUIManager_Inventory>(FindObjectsInactive.Include);

        if (townTabManager == null)
            townTabManager = FindFirstObjectByType<TabUIManager_Town>(FindObjectsInactive.Include);

        if (dexTabManager == null)
            dexTabManager = FindFirstObjectByType<TabUIManager_Dex>(FindObjectsInactive.Include);

        if (optionTabManager == null)
            optionTabManager = FindFirstObjectByType<TabUIManager_Option>(FindObjectsInactive.Include);

        if (gachaUI == null)
            gachaUI = FindFirstObjectByType<UIController_Gacha>(FindObjectsInactive.Include);

        if (animalDexUI == null)
            animalDexUI = FindFirstObjectByType<UIController_AnimalDex>(FindObjectsInactive.Include);
    }

    /// <summary>
    /// 씬의 UIPanelWindow를 캐시하고 Gacha/Option 대표 패널을 연결합니다.
    /// </summary>
    private void CachePanelWindows()
    {
        registeredWindows.Clear();
        gachaPanel = null;
        optionPanel = null;

        UIPanelWindow[] windows = FindObjectsByType<UIPanelWindow>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < windows.Length; i++)
        {
            UIPanelWindow window = windows[i];
            if (window == null)
                continue;

            registeredWindows.Add(window);

            if (gachaPanel == null && window.MenuType == GameMenuType.Gacha)
                gachaPanel = window;

            if (optionPanel == null && window.MenuType == GameMenuType.Option)
                optionPanel = window;
        }
    }

    /// <summary>
    /// 배타 대상 메인 메뉴인지 판별합니다.
    /// </summary>
    public static bool IsExclusiveMainMenu(GameMenuType menuType)
    {
        return menuType == GameMenuType.Inventory
            || menuType == GameMenuType.Gacha
            || menuType == GameMenuType.Dex
            || menuType == GameMenuType.Village
            || menuType == GameMenuType.Option;
    }

    /// <summary>
    /// keepType을 제외한 메인 메뉴를 모두 닫습니다.
    /// 1) TabManager CloseAllTabs 2) GameMenuType 기준 UIPanelWindow Close
    /// </summary>
    public void CloseOtherExclusiveMenus(GameMenuType keepType)
    {
        if (!useTradeOffSetting)
            return;

        if (keepType != GameMenuType.Inventory)
            inventoryTabManager?.CloseAllTabs();

        if (keepType != GameMenuType.Village)
            townTabManager?.CloseAllTabs();

        if (keepType != GameMenuType.Dex)
            dexTabManager?.CloseAllTabs();

        if (keepType != GameMenuType.Option)
            optionTabManager?.CloseAllTabs();

        for (int i = 0; i < registeredWindows.Count; i++)
        {
            UIPanelWindow window = registeredWindows[i];
            if (window == null)
                continue;

            if (!IsExclusiveMainMenu(window.MenuType))
                continue;

            if (window.MenuType == keepType)
                continue;

            if (!window.gameObject.activeSelf)
                continue;

            window.ClosePanel();
        }
    }

    private void OnCameraTargetSelected(Transform target)
    {
        if (target == null)
            return;

        CloseAllPanels();
    }

    /// <summary>
    /// 화면 축소 등에서 열린 메인 메뉴 패널을 모두 닫습니다.
    /// </summary>
    public void CloseAllPanels()
    {
        inventoryTabManager?.CloseAllTabs();
        townTabManager?.CloseAllTabs();
        dexTabManager?.CloseAllTabs();
        optionTabManager?.CloseAllTabs();

        for (int i = 0; i < registeredWindows.Count; i++)
        {
            UIPanelWindow window = registeredWindows[i];
            if (window == null)
                continue;

            if (!IsExclusiveMainMenu(window.MenuType))
                continue;

            if (!window.gameObject.activeSelf)
                continue;

            window.ClosePanel();
        }
    }

    private void OnInventoryButtonClicked()
    {
        if (inventoryTabManager == null)
        {
            Debug.LogWarning("[UIController_Menu] InventoryTabManager가 연결되지 않았습니다.");
            return;
        }

        bool willOpen = !inventoryTabManager.IsAnyTabOpen;
        if (willOpen)
            CloseOtherExclusiveMenus(GameMenuType.Inventory);

        inventoryTabManager.ToggleInventoryTabs();
    }

    private void OnVillageButtonClicked()
    {
        if (townTabManager == null)
        {
            Debug.LogWarning("[UIController_Menu] TownTabManager가 연결되지 않았습니다.");
            return;
        }

        bool willOpen = !townTabManager.IsAnyTabOpen;
        if (willOpen)
            CloseOtherExclusiveMenus(GameMenuType.Village);

        townTabManager.ToggleTownTabs();
    }

    private void OnDexButtonClicked()
    {
        if (dexTabManager != null)
        {
            bool willOpen = !dexTabManager.IsAnyTabOpen;
            if (willOpen)
                CloseOtherExclusiveMenus(GameMenuType.Dex);

            dexTabManager.ToggleDexTabs();

            if (dexTabManager.IsAnyTabOpen)
                animalDexUI?.NotifyPanelOpened();
            return;
        }

        ToggleSinglePanelMenu(GameMenuType.Dex, () => animalDexUI?.NotifyPanelOpened());
    }

    private void OnGachaButtonClicked()
    {
        ToggleSinglePanelMenu(GameMenuType.Gacha, () => gachaUI?.NotifyPanelOpened());
    }

    private void OnOptionButtonClicked()
    {
        if (optionTabManager == null)
        {
            Debug.LogWarning("[UIController_Menu] OptionTabManager가 연결되지 않았습니다.");
            return;
        }

        bool willOpen = !optionTabManager.IsAnyTabOpen;
        if (willOpen)
            CloseOtherExclusiveMenus(GameMenuType.Option);

        optionTabManager.ToggleOptionTabs();
    }

    /// <summary>
    /// Gacha처럼 단일 UIPanelWindow 메뉴를 토글합니다.
    /// </summary>
    private void ToggleSinglePanelMenu(GameMenuType menuType, System.Action onOpened = null)
    {
        UIPanelWindow panel = FindPrimaryPanel(menuType);
        if (panel == null)
        {
            Debug.LogWarning($"[UIController_Menu] GameMenuType.{menuType} 패널을 찾지 못했습니다.");
            return;
        }

        bool willOpen = !panel.gameObject.activeSelf;
        if (willOpen)
            CloseOtherExclusiveMenus(menuType);

        if (usePanelOpenDefaultPosition)
            panel.TogglePanelDefaultPosition();
        else
            panel.TogglePanelSetPosition();

        if (panel.gameObject.activeSelf)
            onOpened?.Invoke();
    }

    private UIPanelWindow FindPrimaryPanel(GameMenuType menuType)
    {
        if (menuType == GameMenuType.Gacha && gachaPanel != null)
            return gachaPanel;

        if (menuType == GameMenuType.Option && optionPanel != null)
            return optionPanel;

        UIPanelWindow active = null;
        UIPanelWindow first = null;

        for (int i = 0; i < registeredWindows.Count; i++)
        {
            UIPanelWindow window = registeredWindows[i];
            if (window == null || window.MenuType != menuType)
                continue;

            if (first == null)
                first = window;

            if (window.gameObject.activeSelf)
            {
                active = window;
                break;
            }
        }

        return active != null ? active : first;
    }
}
