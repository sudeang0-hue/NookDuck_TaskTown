using KAY;
using UnityEngine;
using UnityEngine.UI;

public class UIController_Menu : MonoBehaviour
{
    [Header("메뉴 목록 버튼")]
    [SerializeField] private Button animalInventoryButton;
    [SerializeField] private Button toolInventoryButton;
    [SerializeField] private Button gachaButton;
    [SerializeField] private Button animalDexButton;
    [SerializeField] private Button optionButton;

    [Header("오픈할 UI 창")]
    [SerializeField] private UIPanelWindow animalInventoryPanel;
    [SerializeField] private UIPanelWindow toolInventoryPanel;
    [SerializeField] private UIPanelWindow gachaPanel;
    [SerializeField] private UIPanelWindow animalDexPanel;
    [SerializeField] private UIPanelWindow optionPanel;

    [Header("패널 오픈시 초기 위치 고정")]
    [SerializeField] private bool usePanelOpenDefaultPosition;

    private void Awake()
    {
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
            Debug.LogWarning("[UIController_Menu] 연결되지 않은 패널이 있습니다.");
            return;
        }

        if(usePanelOpenDefaultPosition == true)
        {
            panel.TogglePanelDefaultPosition();
        }
        else
        {
            panel.TogglePanelSetPosition();
        }

    }
}
