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
        [Header("???? ????????? ??? ??? ???")]
        //[SerializeField] private TMP_Text unknownAnimalName_Dif;

        [Header("??? ???? ???? ????")]
        [SerializeField] private Image animalIconImage;
        [SerializeField] private TMP_Text animalNameText;

        [Header("??? ???? ???")]
        [SerializeField] private Button coverButton;

        private AnimalDataSO animalData;
        private UIController_AnimalDexPage animalPageController;
        private bool isUnlocked;
        private bool useProfileImage;

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
        public void Initialize(AnimalDataSO data, UIController_AnimalDexPage pageController, bool unlocked, bool useProfileImage = false)
        {
            if (data == null)
            {
                Debug.LogWarning("[SlotUI_AnimalDex] 전달된 AnimalDataSO가 비어 있습니다.");
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
            this.useProfileImage = useProfileImage;

            RefreshView();
        }

        /// <summary>
        /// ???? ?????? ??? ?????? ??????? ????? ???????? ??? ???????.
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

            // ????? + ???? ????????? ??? ????? ?? Dif ???
            RefreshDifficultyUnavailableMark();

            if (coverButton != null)
            {
                // ----------------08.09.KAY (????? ????? ?? ?????? ????)------------------
                // ???????? ?? ???????? ?? ?? ????? ????? ??? ???????.
                coverButton.interactable = true;
                // ---------------------------------------------------------
            }

        }

        /// <summary>
        /// ??? ???? ???? ???
        /// </summary>
        private void ShowUnlockedView()
        {
            if (animalIconImage != null)
            {
                // useProfileImageToslot == false: ???? Icon ??? / true: AnimalDataSO.ProfileImage ???
                Sprite displaySprite = useProfileImage ? animalData.ProfileImage : animalData.Icon;
                animalIconImage.sprite = displaySprite;
                animalIconImage.enabled = displaySprite != null;
            }

            if (animalNameText != null)
            {
                animalNameText.text = animalData.AnimalDisplayName_;
            }
        }

        /// <summary>
        /// ????? ???? ???? ???
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
        /// ????? ?? ????, ???? ????????? ???? RequiredDifficulty ???????
        /// unknownAnimalName_Dif?? ????????.
        /// </summary>
        private void RefreshDifficultyUnavailableMark()
        {
            //if (unknownAnimalName_Dif == null)
            //    return;

            // -------------- 08.10 ?????? -------------------------------
            //bool showDif = !isUnlocked && IsUnavailableOnCurrentDifficulty();
            //unknownAnimalName_Dif.gameObject.SetActive(showDif);

        }

        /// <summary>
        /// ????? ???? ???? ??? ??? ????.
        /// Normal ?? Hard/VeryHard Dif ON
        /// Hard ?? VeryHard Dif ON
        /// VeryHard ?? ??? Dif OFF (???? ????? ?????? ??? ????)
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
