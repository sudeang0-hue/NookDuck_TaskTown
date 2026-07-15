//NB - 아영님의 UIController_Menu 에 기능 추가 및 충돌 해결 통합본
using UnityEngine;
using UnityEngine.UI;

public class UIController_Menu_Test : MonoBehaviour
{
    [Header("GameMasterManager 직접 참조")]
    [SerializeField] private GameMasterManager gameMaster; // 실제 SetActive 및 DOTween 연출을 전담하는 매니저

    [Header("메뉴 목록 버튼")]
    [SerializeField] private Button animalInventoryButton;
    [SerializeField] private Button toolInventoryButton;
    [SerializeField] private Button gachaButton;
    [SerializeField] private Button animalDexButton;
    [SerializeField] private Button optionButton;

    // 각 창들이 현재 열렸는지 닫혔는지 독립적으로 기억하는 스위치 플래그들
    private bool isAnimalInvOpen = false;
    private bool isToolInvOpen = false;
    private bool isGachaOpen = false;
    private bool isDexOpen = false;
    private bool isOptionOpen = false;

    private void Awake()
    {
        if (gameMaster == null)
        {
            // 씬에 존재하는 GameMasterManager를 자동으로 찾아 안전장치 설정
            gameMaster = FindFirstObjectByType<GameMasterManager>();
        }

        animalInventoryButton.onClick.AddListener(ToggleAnimalInventory);
        toolInventoryButton.onClick.AddListener(ToggleToolInventory);
        gachaButton.onClick.AddListener(ToggleGacha);
        animalDexButton.onClick.AddListener(ToggleDex);
        optionButton.onClick.AddListener(ToggleOption);
    }

    // 1. 동물 관리 인벤토리 토글 (독립 작동)
    private void ToggleAnimalInventory()
    {
        if (gameMaster == null) return;

        isAnimalInvOpen = !isAnimalInvOpen;
        if (isAnimalInvOpen)
            gameMaster.OpenManage();
        else
            gameMaster.CloseManage();
    }

    // 2. 도구 인벤토리 토글 (독립 작동)
    private void ToggleToolInventory()
    {
        if (gameMaster == null) return;

        isToolInvOpen = !isToolInvOpen;
        if (isToolInvOpen)
            gameMaster.OpenToolInv();
        else
            gameMaster.CloseToolInv();
    }

    // 3. 가챠 토글 (독립 작동)
    private void ToggleGacha()
    {
        if (gameMaster == null) return;

        isGachaOpen = !isGachaOpen;
        if (isGachaOpen)
            gameMaster.OpenGacha();
        else
            gameMaster.CloseGacha();
    }

    // 4. 도감 토글 (독립 작동)
    private void ToggleDex()
    {
        if (gameMaster == null) return;

        isDexOpen = !isDexOpen;
        if (isDexOpen)
            gameMaster.OpenDex();
        else
            gameMaster.CloseDex();
    }

    // 5. 옵션 토글 (독립 작동)
    private void ToggleOption()
    {
        if (gameMaster == null) return;

        isOptionOpen = !isOptionOpen;
        if (isOptionOpen)
            gameMaster.OpenOption();
        else
            gameMaster.CloseOption();
    }
}