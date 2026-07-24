using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// ���� �� �������� ��ȣ�ۿ� ��ư ��,
    /// Set Tool ��ư�� "���� ���� ���� ��� ����"�� ����մϴ�.
    /// ���� ���� ������ tool_set_slot�� cover_btn Ŭ�� ��
    /// UIController_ToolSetList �� SlotUI_ToolSet ��ο��� ó���˴ϴ�.
    /// </summary>
    public class AnimalInvPage_Manager : MonoBehaviour
    {
        [Header("�ش� ������ ���� ��ȣ�ۿ� ��ư")]
        [SerializeField] private Button levelupButton;         // ������ ��ư
        [SerializeField] private Button toolSetButton;         // Set Tool - ��� ���� ����
        [SerializeField] private Button toolChangeButton;      // ���� ���� ��ư (���� Manager �̰� �� ���)
        [SerializeField] private Button toolSetOffButton;      // ���� ���� ��ư (���� Manager �̰� �� ���)

        [Header("���� ��� / �� ������")]
        [SerializeField] private GameObject toolSettingPage;
        [Tooltip("����θ� ���� GameObject���� UIController_AnimalInvPage�� ã���ϴ�.")]
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
            // �� �������� ���� �� ToolSetting �гε� �Բ� �ݽ��ϴ�.
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
            Debug.Log("[AnimalInvPage_Manager] ������ �õ�");
        }

        /// <summary>
        /// Set Tool: ���� ���� ���� ��ϸ� ���ϴ�.
        /// ������ cover_btn ���� �� UIController_ToolSetList���� �����մϴ�.
        /// </summary>
        private void OnClickToolSet()
        {
            ResolveAnimalInvPage();

            if (toolSettingPage == null)
            {
                Debug.LogWarning("[AnimalInvPage_Manager] toolSettingPage �� ������� �ʾҽ��ϴ�.", this);
                return;
            }

            if (animalInvPage == null)
            {
                Debug.LogWarning("[AnimalInvPage_Manager] UIController_AnimalInvPage �� ã�� ���߽��ϴ�.", this);
                return;
            }

            string animalId = animalInvPage.CurrentAnimalId;

            if (string.IsNullOrEmpty(animalId))
            {
                Debug.LogWarning("[AnimalInvPage_Manager] ���� ���� ID�� ��� �־� ���� ����� �� �� �����ϴ�.", this);
                return;
            }

            if (toolSettingPage.TryGetComponent(out UIController_ToolSetList toolSetList))
            {
                // ��ϸ� ����. pendingAnimalId�� ���� �ΰ�, cover_btn Ŭ�� �� �����Ѵ�.
                toolSetList.Open(animalId, animalInvPage.RefreshAnimalInvPage);
            }
            else
            {
                toolSettingPage.SetActive(true);
            }
        }

        /// <summary>
        /// ToolSettingList_root �г��� �ݽ��ϴ�.
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
