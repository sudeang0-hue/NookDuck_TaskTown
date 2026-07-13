using Animal.Data;
using KAY;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{

    public class SlotUI_AnimalList : SlotUIBase
    {

        [Header("미해금 상태 도감 슬롯 UI")]
        [SerializeField] private Sprite unknownAnimalIcon;
        [SerializeField] private string unknownAnimalName = "???";

        [Header("해금 상태 동물 정보 UI")]
        [SerializeField] private Image animalIconImage;
        [SerializeField] private TMP_Text animalNameText;

        [Header("클릭 범위 버튼")]
        [SerializeField] private Button coverButton;

        private AnimalDataSO animalData;
        private UIController_AnimalPage animalPageController;
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
        public void Initialize(AnimalDataSO data, UIController_AnimalPage pageController, bool unlocked)
        {
            if (data == null)
            {
                Debug.LogWarning("[SlotUI_AnimalList] 연결할 AnimalDataSO가 없습니다.");
                return;
            }

            if (coverButton == null)
            {
                Debug.LogWarning("[SlotUI_AnimalList] coverButton이 연결되지 않았습니다.");
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
        /// 해금되면 보여주는 UI
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
        /// 미해금 상태에서 보여주는 UI
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
        /// 슬롯 클릭 시 자신의 동물 데이터를 상세 페이지에 전달
        /// </summary>
        private void HandleSlotClicked()
        {
            if (animalData == null)
            {
                Debug.LogWarning("[SlotUI_Animalist] 현재 슬롯에 동물 데이터가 없습니다.");
                return;
            }

            if (animalPageController == null)
            {
                Debug.LogWarning("[SlotUI_AnimalList] UIController_AnimalPage 가 연결되지 않았습니다.");
                return;
            }

            animalPageController.OpenAnimalPage(animalData);
        }

    }
}
