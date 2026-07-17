using Animal.Data;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace UI
{
    public class UIController_AnimalDexPage : MonoBehaviour
    {
        [Header("동물 상세 페이지 패널")]
        [FormerlySerializedAs("animalListPagePanel")]
        [SerializeField] private GameObject animalDexPagePanel;

        [Header("동물 상세 정보")]
        [SerializeField] private Image animalIconImage;
        [SerializeField] private TMP_Text animalNameText;
        [SerializeField] private TMP_Text animalDescriptionText;

        private AnimalDataSO currentAnimalData;

        private void Awake()
        {
            if (animalDexPagePanel != null)
            {
                animalDexPagePanel.SetActive(false);
            }
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

            if (animalDexPagePanel == null)
            {
                Debug.LogWarning("[UIController_AnimalDexPage] animalDexPagePanel이 연결되지 않았습니다.");
                return;
            }

            currentAnimalData = animalData;

            RefreshAnimalDexPage();

            animalDexPagePanel.SetActive(true);
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
        /// 상세 페이지 닫기 버튼용
        /// </summary>
        public void CloseAnimalDexPage()
        {
            if (animalDexPagePanel == null)
                return;

            animalDexPagePanel.SetActive(false);
        }
    }
}