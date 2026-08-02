using Animal.Data;
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
        /// </summary>
        private void RefreshAnimalDexPage()
        {
            if (currentAnimalData == null)
                return;

            if (animalIconImage != null)
            {
                animalIconImage.sprite = currentAnimalData.Icon;
                animalIconImage.enabled = currentAnimalData.Icon != null;
            }

            if (animalNameText != null)
            {
                animalNameText.text = currentAnimalData.DisplayName;
            }

            if (animalDescriptionText != null)
            {
                animalDescriptionText.text = currentAnimalData.AnimalDescription;
            }
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
