using Animal.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{

    public class SlotUI_AnimalDex : SlotUIBase
    {

        [Header("미해금 상태 도감 슬롯")]
        [SerializeField] private Sprite unknownAnimalIcon;
        [SerializeField] private string unknownAnimalName = "???";

        [Header("해금 상태 동물 정보")]
        [SerializeField] private Image animalIconImage;
        [SerializeField] private TMP_Text animalNameText;

        [Header("클릭 범위 버튼")]
        [SerializeField] private Button coverButton;

        private AnimalDataSO animalData;
        private UIController_AnimalDexPage animalPageController;
        private bool isUnlocked;

        public AnimalDataSO AnimalData => animalData;
        public string AnimalId => animalData != null ? animalData.Id : string.Empty;


        private void Awake()
        {
            if (coverButton == null)
                coverButton = GetComponentInChildren<Button>(true);
        }

        private void OnEnable()
        {
            if (coverButton != null)
            {
                coverButton.onClick.AddListener(HandleSlotClicked);
            }
        }


        private void OnDisable()
        {
            if (coverButton != null)
            {
                coverButton.onClick.RemoveListener(HandleSlotClicked);
            }
        }

        /// <summary>
        /// 도감 슬롯에 동물 데이터와 상세 페이지 Controller를 연결
        /// </summary>
        public void Initialize(AnimalDataSO data, UIController_AnimalDexPage pageController, bool unlocked)
        {
            if (data == null)
            {
                Debug.LogWarning("[SlotUI_AnimalDex] 연결할 AnimalDataSO가 없습니다.");
                return;
            }

            if (coverButton == null)
            {
                Debug.LogWarning("[SlotUI_AnimalDex] coverButton이 연결되지 않았습니다.");
                return;
            }

            animalData = data;
            animalPageController = pageController;
            isUnlocked = unlocked;

            RefreshView();
        }

        /// <summary>
        /// 도감 슬롯의 잠금 상태를 변경하고 이름과 아이콘을 다시 표시합니다.
        /// </summary>
        public void SetUnlocked(bool unlocked)
        {
            isUnlocked = unlocked;
            RefreshView();
        }

        /// <summary>
        /// 현재 동물 데이터로 슬롯 비주얼 갱신
        /// </summary>
        private void RefreshView()
        {
            if (animalData == null)
                return;

            if(isUnlocked)
            {
                ShowUnlockedView();
            }
            else
            {
                ShowLockedView();
            }

            if (coverButton != null)
            {
                coverButton.interactable = isUnlocked;
            }

        }

        /// <summary>
        /// 해금되면 보여주는 UIController_AnimalInvPage
        /// </summary>
        private void ShowUnlockedView()
        {
            if (animalIconImage != null)
            {
                animalIconImage.sprite = animalData.Icon;
                animalIconImage.enabled = animalData.Icon != null;
            }

            if (animalNameText != null)
            {
                animalNameText.text = animalData.DisplayName;
            }
        }

        /// <summary>
        /// 미해금 상태에서 보여주는 UIController_AnimalInvPage
        /// </summary>
        private void ShowLockedView()
        {
            if (animalIconImage != null)
            {
                animalIconImage.sprite = unknownAnimalIcon;
                animalIconImage.enabled = unknownAnimalIcon != null;
            }

            if (animalNameText != null)
            {
                animalNameText.text = unknownAnimalName;
            }
        }


        /// <summary>
        /// 슬롯 클릭 시 도감 상세 페이지를 토글합니다.
        /// 같은 동물이 이미 열려 있으면 Close, 아니면 Open합니다.
        /// </summary>
        private void HandleSlotClicked()
        {
            if (animalData == null)
            {
                Debug.LogWarning("[SlotUI_AnimalDex] 현재 슬롯에 동물 데이터가 없습니다.", this);
                return;
            }

            if (animalPageController == null)
            {
                Debug.LogWarning("[SlotUI_AnimalDex] UIController_AnimalDexPage 가 연결되지 않았습니다.", this);
                return;
            }

            if (animalPageController.IsOpen &&
                animalPageController.CurrentAnimalId == animalData.Id)
            {
                animalPageController.CloseAnimalDexPage();
                return;
            }

            animalPageController.OpenAnimalDexPage(animalData);
        }

    }
}
