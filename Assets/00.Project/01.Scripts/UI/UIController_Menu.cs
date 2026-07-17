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

    [Header("오픈할 UIController_AnimalInvPage 창")]
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
