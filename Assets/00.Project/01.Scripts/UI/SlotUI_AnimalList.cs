using KAY;
using UnityEngine;
using UnityEngine.UI;

public class SlotUI_AnimalList : SlotUIBase
{

    [Header("클릭 범위 버튼")]
    [SerializeField] private Button coverButton;

    private UIController_AnimalPage animalPageController;
    
    private void Awake()
    {
        if (coverButton == null)
            coverButton = GetComponentInChildren<Button>(true);
    }

    public void Initialize(UIController_AnimalPage pageController)
    {
        animalPageController = pageController;

        if (coverButton == null)
        {
            Debug.LogWarning("[SlotUI_AnimalList] coverButton이 연결되지 않았습니다.");
            return;
        }

        coverButton.onClick.RemoveListener(OnClickSlot);
        coverButton.onClick.AddListener(OnClickSlot);
    }

    private void OnClickSlot()
    {
        if (animalPageController == null)
        {
            Debug.LogWarning("[SlotUI_AnimalList] animalPageController가 연결되지 않았습니다.");
            return;
        }

        animalPageController.OpenAnimalPage();
    }

    private void OnDestroy()
    {
        if (coverButton != null)
            coverButton.onClick.RemoveListener(OnClickSlot);
    }
}
