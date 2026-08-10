using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// 동물 인벤 페이지의 상호작용 버튼 중,
    /// Set Tool 버튼을 "도구 장착 목록 표시 담당"으로 연결합니다.
    /// 실제 도구 장착은 tool_set_slot의 cover_btn 클릭 시
    /// UIController_ToolSetList 및 SlotUI_ToolSet 경로에서 처리됩니다.
    /// </summary>
    public class AnimalInvPage_Manager : MonoBehaviour
    {
        [Header("해당 페이지 내 상호작용 버튼")]
        [SerializeField] private Button levelupButton;         // 레벨업 버튼
        [SerializeField] private Button toolSetButton;         // Set Tool - 도구 장착 목록
        [SerializeField] private Button toolChangeButton;      // 도구 변경 버튼 (다른 Manager 이관 될 예정)
        [SerializeField] private Button toolSetOffButton;      // 도구 해제 버튼 (다른 Manager 이관 될 예정)

        [Header("도구 목록 / 툴 세팅페이지")]
        [SerializeField] private GameObject toolSettingPage;
        [Tooltip("비어있으면 같은 GameObject에서 UIController_AnimalInvPage를 찾습니다.")]
        [SerializeField] private UIController_AnimalInvPage animalInvPage;

        private void Awake()
        {
            ResolveAnimalInvPage();

            if (levelupButton != null)
                levelupButton.onClick.AddListener(OnClickLevelUp);

            if (toolSetButton != null)
                toolSetButton.onClick.AddListener(OnClickToolSet);
        }

        private void OnDisable()
        {
            // 이 페이지가 닫힐 때 ToolSetting 패널도 함께 닫습니다.
            CloseToolSettingPage();
        }

        private void OnDestroy()
        {
            if (levelupButton != null)
                levelupButton.onClick.RemoveListener(OnClickLevelUp);

            if (toolSetButton != null)
                toolSetButton.onClick.RemoveListener(OnClickToolSet);
        }

        private void ResolveAnimalInvPage()
        {
            if (animalInvPage == null)
                animalInvPage = GetComponent<UIController_AnimalInvPage>();
        }

        private void OnClickLevelUp()
        {
            Debug.Log("[AnimalInvPage_Manager] 레벨업 시도");
        }

        /// <summary>
        /// Set Tool: 도구 장착 목록만 엽니다.
        /// 실제는 cover_btn 선택 시 UIController_ToolSetList에서 처리합니다.
        /// </summary>
        private void OnClickToolSet()
        {
            ResolveAnimalInvPage();

            if (toolSettingPage == null)
            {
                Debug.LogWarning("[AnimalInvPage_Manager] toolSettingPage 가 할당되지 않았습니다.", this);
                return;
            }

            if (animalInvPage == null)
            {
                Debug.LogWarning("[AnimalInvPage_Manager] UIController_AnimalInvPage 를 찾지 못했습니다.", this);
                return;
            }

            string animalId = animalInvPage.CurrentAnimalId;

            if (string.IsNullOrEmpty(animalId))
            {
                Debug.LogWarning("[AnimalInvPage_Manager] 현재 동물 ID가 비어 있어 도구 목록을 열 수 없습니다.", this);
                return;
            }

            if (toolSettingPage.TryGetComponent(out UIController_ToolSetList toolSetList))
            {
                // 목록만 엽니다. pendingAnimalId로 보관하고, cover_btn 클릭 시 장착한다.
                toolSetList.Open(animalId, animalInvPage.RefreshAnimalInvPage);
            }
            else
            {
                toolSettingPage.SetActive(true);
            }
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
