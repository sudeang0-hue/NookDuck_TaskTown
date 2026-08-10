using Animal.Data;
using TaskTown.Gacha;
using TaskTown.KDH;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UIController_AnimalDexPage : MonoBehaviour
    {
        // Dex_Page 자신에 붙어 있으므로 SerializeField 없이 gameObject를 사용합니다.
        private GameObject AnimalDexPagePanel => gameObject;

        [Header("동물 상세 정보")]
        [SerializeField] private Image animalIconImage;
        [SerializeField] private TMP_Text animalNameText;
        [SerializeField] private TMP_Text animalDescriptionText;

        [Header("미해금 반영 사항")]
        [SerializeField] private Sprite unknownAnimalIcon;
        [SerializeField] private string unknownAnimalName = "???";
        [SerializeField, TextArea(2, 4)]
        private string unlokedMessage = "이 주민은 아직 마을에 방문하지 않았어요.";
        [SerializeField, TextArea(2, 4)]
        private string unlokedDifficultyMessage = "이 주민은 현재 만날 수 없어요.";

        private AnimalDataSO currentAnimalData;

        public AnimalDataSO CurrentAnimalData => currentAnimalData;

        public string CurrentAnimalId => currentAnimalData != null ? currentAnimalData.Id : string.Empty;

        /// <summary>
        /// 상세 페이지 패널이 현재 열려 있는지 여부
        /// </summary>
        public bool IsOpen => AnimalDexPagePanel != null && AnimalDexPagePanel.activeSelf;

        private void Awake()
        {
            AnimalDexPagePanel.SetActive(false);
        }

        /// <summary>
        /// 도감 슬롯 클릭 시 선택한 동물의 정보로 상세 페이지를 갱신하고 패널을 엽니다.
        /// </summary>
        public void OpenAnimalDexPage(AnimalDataSO animalData)
        {
            if (animalData == null)
            {
                Debug.LogWarning("[UIController_AnimalDexPage] 표시할 동물 데이터가 없습니다.");
                return;
            }

            currentAnimalData = animalData;

            RefreshAnimalDexPage();

            AnimalDexPagePanel.SetActive(true);
        }

        /// <summary>
        /// 현재 선택된 동물 데이터로 상세 페이지를 갱신합니다.
        /// 해금/미해금/현재 난이도 획득 불가에 따라 아이콘·이름·설명을 분기합니다.
        /// </summary>
        private void RefreshAnimalDexPage()
        {
            if (currentAnimalData == null)
                return;

            // ----------------08.09.KAY (미해금/난이도별 상세 페이지 표시)------------------
            bool isUnlocked = IsAnimalDiscovered(currentAnimalData.Id);

            if (isUnlocked)
            {
                ShowUnlockedAnimalView();
                return;
            }

            ShowLockedAnimalView(IsUnavailableOnCurrentDifficulty(currentAnimalData));
            // ---------------------------------------------------------
        }

        private void ShowUnlockedAnimalView()
        {
            if (animalIconImage != null)
            {
                animalIconImage.sprite = currentAnimalData.Icon;
                animalIconImage.enabled = currentAnimalData.Icon != null;
            }

            if (animalNameText != null)
                animalNameText.text = currentAnimalData.DisplayName;

            if (animalDescriptionText != null)
                animalDescriptionText.text = currentAnimalData.AnimalDescription;
        }

        private void ShowLockedAnimalView(bool unavailableOnDifficulty)
        {
            if (animalIconImage != null)
            {
                animalIconImage.sprite = unknownAnimalIcon;
                animalIconImage.enabled = unknownAnimalIcon != null;
            }

            if (animalNameText != null)
                animalNameText.text = unknownAnimalName;

            if (animalDescriptionText != null)
            {
                animalDescriptionText.text = unavailableOnDifficulty
                    ? unlokedDifficultyMessage
                    : unlokedMessage;
            }
        }

        private static bool IsAnimalDiscovered(string animalId)
        {
            if (string.IsNullOrEmpty(animalId) || DexRecordManager.Instance == null)
                return false;

            return DexRecordManager.Instance.IsDiscovered(animalId);
        }

        /// <summary>
        /// 난이도 계층 기준 획득 불가 여부.
        /// Normal → Hard/VeryHard 불가, Hard → VeryHard 불가, VeryHard → 모두 가능.
        /// </summary>
        private static bool IsUnavailableOnCurrentDifficulty(AnimalDataSO animalData)
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
        /// 상세 페이지 닫기 버튼용.
        /// 선택 데이터도 함께 초기화합니다.
        /// </summary>
        public void CloseAnimalDexPage()
        {
            currentAnimalData = null;

            AnimalDexPagePanel.SetActive(false);
        }
    }
}
