using Animal.Data;
using TaskTown.Gacha;
using TaskTown.KDH;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{

    public class SlotUI_AnimalDex : SlotUIBase
    {

        [Header("????? ???? ???? ????")]
        [SerializeField] private Sprite unknownAnimalIcon;
        [SerializeField] private string unknownAnimalName = "???";
        [Header("???? ????????? ???? ?? ???? ????")]
        [SerializeField] private TMP_Text unknownAnimalName_Dif;

        [Header("??? ???? ???? ????")]
        [SerializeField] private Image animalIconImage;
        [SerializeField] private TMP_Text animalNameText;

        [Header("??? ???? ???")]
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
        /// ???? ????? ???? ??????? ?? ?????? Controller?? ????
        /// </summary>
        public void Initialize(AnimalDataSO data, UIController_AnimalDexPage pageController, bool unlocked)
        {
            if (data == null)
            {
                Debug.LogWarning("[SlotUI_AnimalDex] ?????? AnimalDataSO?? ???????.");
                return;
            }

            if (coverButton == null)
            {
                Debug.LogWarning("[SlotUI_AnimalDex] coverButton?? ??????? ???????.");
                return;
            }

            animalData = data;
            animalPageController = pageController;
            isUnlocked = unlocked;

            RefreshView();
        }

        /// <summary>
        /// ???? ?????? ??? ???¸? ??????? ????? ???????? ??? ???????.
        /// </summary>
        public void SetUnlocked(bool unlocked)
        {
            isUnlocked = unlocked;
            RefreshView();
        }

        /// <summary>
        /// ???? ???? ??????? ???? ????? ????
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

            // ????? + ???? ????????? ???? ?? ???? ??????? Dif ???
            RefreshDifficultyUnavailableMark();

            if (coverButton != null)
            {
                // ----------------08.09.KAY (미해금 슬롯도 상세 페이지 오픈)------------------
                // 미해금이어도 상세 페이지를 열 수 있도록 클릭을 항상 허용합니다.
                coverButton.interactable = true;
                // ---------------------------------------------------------
            }

        }

        /// <summary>
        /// ????? ??????? UIController_AnimalInvPage
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
        /// ????? ???¿??? ??????? UIController_AnimalInvPage
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
        /// 획득한 적 없고, 현재 난이도보다 높은 RequiredDifficulty 동물이면
        /// unknownAnimalName_Dif를 활성화합니다.
        /// </summary>
        private void RefreshDifficultyUnavailableMark()
        {
            if (unknownAnimalName_Dif == null)
                return;

            bool showDif = !isUnlocked && IsUnavailableOnCurrentDifficulty();
            unknownAnimalName_Dif.gameObject.SetActive(showDif);

        }

        /// <summary>
        /// 난이도 계층 기준 획득 불가 여부.
        /// Normal → Hard/VeryHard Dif ON
        /// Hard → VeryHard Dif ON
        /// VeryHard → 모든 Dif OFF (하위 난이도 설정도 획득 가능)
        /// </summary>
        private bool IsUnavailableOnCurrentDifficulty()
        {
            if (animalData == null || !animalData.IsDifficultyExclusive)
                return false;

            DifficultyType currentDifficulty = GetCurrentDifficulty();
            return animalData.RequiredDifficulty > currentDifficulty;
        }

        private static DifficultyType GetCurrentDifficulty()
        {
            if (RealProductionTicker.Instance != null)
                return RealProductionTicker.Instance.CurrentDifficulty;

            int stored = PlayerPrefs.GetInt(
                RealProductionTicker.DifficultyPrefsKey,
                (int)DifficultyType.Normal);

            if (System.Enum.IsDefined(typeof(DifficultyType), stored))
                return (DifficultyType)stored;

            return DifficultyType.Normal;
        }


        /// <summary>
        /// ???? ??? ?? ???? ?? ???????? ???????.
        /// ???? ?????? ??? ???? ?????? Close, ???? Open????.
        /// </summary>
        private void HandleSlotClicked()
        {
            if (animalData == null)
            {
                Debug.LogWarning("[SlotUI_AnimalDex] ???? ????? ???? ??????? ???????.", this);
                return;
            }

            if (animalPageController == null)
            {
                Debug.LogWarning("[SlotUI_AnimalDex] UIController_AnimalDexPage ?? ??????? ???????.", this);
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
