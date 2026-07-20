using UI;
using UnityEngine;
using UnityEngine.UI;

public class UIController_Menu : MonoBehaviour
{
    [Header("??? ??? ???")]
    [Tooltip("???? ?????? ????")]
    [SerializeField] private Button animalInventoryButton;
    [Tooltip("???? ?????? ????")]
    [SerializeField] private Button toolInventoryButton;
    [Tooltip("??? ???? ????")]
    [SerializeField] private Button gachaButton;
    [Tooltip("???? ???? ????")]
    [SerializeField] private Button animalDexButton;
    [Tooltip("??? ???? ????")]
    [SerializeField] private Button optionButton;
    [Tooltip("??? ??? ???")]
    [SerializeField] private Button minimizeButton;

    [Header("?????? UI ????")]
    [SerializeField] private UIPanelWindow animalInventoryPanel;
    [SerializeField] private UIPanelWindow toolInventoryPanel;
    [SerializeField] private UIPanelWindow gachaPanel;
    [SerializeField] private UIPanelWindow animalDexPanel;
    [SerializeField] private UIPanelWindow optionPanel;

    [Header("Inventory UI Sync")]
    [Tooltip("???? Inv ???? ???? ?? ?? ??? + SyncAllSlots ??? ???")]
    [SerializeField] private UIController_AnimalInv animalInvUI;
    [Tooltip("???? Inv ???? ???? ?? SyncAllSlots ??? ???")]
    [SerializeField] private UIController_ToolInv toolInvUI;
    [Tooltip("???? ???? ???? ?? ?? ??? ??? ???")]
    [SerializeField] private UIController_AnimalDex animalDexUI;

    [Header("???? ?????? ??? ??? ????")]
    [SerializeField] private bool usePanelOpenDefaultPosition;

    [Header("???? ??? ????")]
    [Tooltip("true?? ?? ???? ????? ????? ????, false?? ??????? ???? ???")]
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
    }

    private void TogglePanel(UIPanelWindow panel)
    {
        if (panel == null)
        {
            Debug.LogWarning("[UIController_Menu] ??????? ???? ?????? ??????.");
            return;
        }

        // ??? ???: ???? ?????? ?? ???? ??? ?????? ???? (???? ??? ??????? ??? ????)
        if (useTradeOffSetting && !panel.gameObject.activeSelf)
            CloseAllExcept(panel);

        if (usePanelOpenDefaultPosition)
            panel.TogglePanelDefaultPosition();
        else
            panel.TogglePanelSetPosition();

        // Inv ?????? ???? ????, ????? ?? ?????? ???? ??????? UI?? ???
        if (panel.gameObject.activeSelf)
            SyncInventoryIfNeeded(panel);
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

        if (panel.gameObject.activeSelf)
            panel.ClosePanel();
    }
}
