using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class AnimalInvPage_Manager : MonoBehaviour
    {
        [Header("해당 동물의 설정 상호작용 버튼")]
        [SerializeField] private Button levelupButton;         // 레벨업 버튼
        [SerializeField] private Button toolSetButton;         // 도구 배치 버튼
        [SerializeField] private Button villageSetButton;      // 마을 배치 버튼

        [SerializeField] private GameObject toolSettingPage;

        private void Awake()
        {
            if (levelupButton != null)
                levelupButton.onClick.AddListener(OnClickLevelUp);

            if (toolSetButton != null)
                toolSetButton.onClick.AddListener(OnClickToolSet);

            if (villageSetButton != null)
                villageSetButton.onClick.AddListener(OnClickVillageSet);
        }

        private void OnDisable()
        {
            // 상세 페이지가 닫힐 때 ToolSetting 패널도 함께 닫습니다.
            CloseToolSettingPage();
        }

        private void OnDestroy()
        {
            if (levelupButton != null)
                levelupButton.onClick.RemoveListener(OnClickLevelUp);

            if (toolSetButton != null)
                toolSetButton.onClick.RemoveListener(OnClickToolSet);

            if (villageSetButton != null)
                villageSetButton.onClick.RemoveListener(OnClickVillageSet);
        }

        private void OnClickLevelUp()
        {
            Debug.Log("[AnimalInvPage_Manager] 레벨업 시도");
        }

        private void OnClickToolSet()
        {
            Debug.Log("[AnimalInvPage_Manager] 도구 세팅하기");

            if (toolSettingPage == null)
            {
                Debug.LogWarning("[AnimalInvPage_Manager] toolSettingPage 가 연결되지 않았습니다.", this);
                return;
            }

            if (toolSettingPage.TryGetComponent(out UIController_ToolSetList toolSetList))
                toolSetList.Open();
            else
                toolSettingPage.SetActive(true);
        }

        private void OnClickVillageSet()
        {
            Debug.Log("[AnimalInvPage_Manager] 마을에 배치하기");
        }

        /// <summary>
        /// ToolSettingList_root 패널을 닫습니다.
        /// </summary>
        public void CloseToolSettingPage()
        {
            if (toolSettingPage == null)
                return;

            if (toolSettingPage.TryGetComponent(out UIController_ToolSetList toolSetList))
                toolSetList.Close();
            else
                toolSettingPage.SetActive(false);
        }
    }
}
