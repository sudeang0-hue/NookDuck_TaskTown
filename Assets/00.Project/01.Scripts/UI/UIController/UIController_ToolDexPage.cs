using Animal.Data;
using TaskTown.KDH;
using TMPro;
using Tool.Data;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UIController_ToolDexPage : MonoBehaviour
    {
        // Dex_Page �ڽſ� �پ� �����Ƿ� SerializeField ���� gameObject�� ����մϴ�.
        private GameObject ToolDexPagePanel => gameObject;

        [Header("���� �� ����")]
        [SerializeField] private Image toolIconImage;
        [SerializeField] private TMP_Text toolNameText;
        [SerializeField] private TMP_Text toolDescriptionText;

        [SerializeField] private Image animaImage;
        [SerializeField] private TMP_Text animalNameText;

        [Header("���ر� �ݿ� ����")]
        [SerializeField] private Sprite unknownToolIcon;
        [SerializeField] private Sprite unknownAnimalIcon;
        [SerializeField] private string unknownToolName = "???";
        [SerializeField] private string unknownAnimalName = "???";
        [SerializeField, TextArea(2, 4)]
        private string unlokedMessage = "�� ������ ���� �߰����� �� �߾��.";

        private ToolDataSO currentToolData;

        public ToolDataSO CurrentToolData => currentToolData;

        public string CurrentToolId => currentToolData != null ? currentToolData.Id : string.Empty;

        /// <summary>
        /// �� ������ �г��� ���� ���� �ִ��� ����
        /// </summary>
        public bool IsOpen => ToolDexPagePanel != null && ToolDexPagePanel.activeSelf;

        private void Awake()
        {
            ToolDexPagePanel.SetActive(false);
        }

        /// <summary>
        /// ���� ���� Ŭ�� �� ������ ������ ������ �� �������� �����ϰ� �г��� ���ϴ�.
        /// </summary>
        public void OpenToolDexPage(ToolDataSO toolData)
        {
            if (toolData == null)
            {
                Debug.LogWarning("[UIController_ToolDexPage] ǥ���� ���� �����Ͱ� �����ϴ�.");
                return;
            }

            currentToolData = toolData;

            RefreshToolDexPage();

            ToolDexPagePanel.SetActive(true);
        }

        /// <summary>
        /// ���� ���õ� ���� �����ͷ� �� �������� �����մϴ�.
        /// �ر�/���رݿ� ���� �����ܡ��̸��������� �б��մϴ�.
        /// </summary>
        private void RefreshToolDexPage()
        {
            if (currentToolData == null)
                return;

            // ----------------08.09.KAY (���ر� ���� �� ������ ǥ��)------------------
            bool isUnlocked = IsToolDiscovered(currentToolData.Id);

            if (isUnlocked)
            {
                ShowUnlockedToolView();
                return;
            }

            ShowLockedToolView();
            // ---------------------------------------------------------
        }

        private void ShowUnlockedToolView()
        {
            if (toolIconImage != null)
            {
                toolIconImage.sprite = currentToolData.Icon;
                toolIconImage.enabled = currentToolData.Icon != null;
            }

            if (toolNameText != null)
                toolNameText.text = currentToolData.DisplayName;

            if (toolDescriptionText != null)
                toolDescriptionText.text = currentToolData.ToolDescription;

            // Ưȭ �ر� �� ���� ���� �����ܡ��̸�, �ƴϸ� unknown ǥ��
            ApplySpecialAnimalInfo();
        }

        private void ShowLockedToolView()
        {
            if (toolIconImage != null)
            {
                toolIconImage.sprite = unknownToolIcon;
                toolIconImage.enabled = unknownToolIcon != null;
            }

            if (toolNameText != null)
                toolNameText.text = unknownToolName;

            if (toolDescriptionText != null)
                toolDescriptionText.text = unlokedMessage;

            // ��ȹ�� ������ animaImage/animalNameText�� unknown���� ǥ��
            ApplySpecialAnimalInfo();
        }

        /// <summary>
        /// animaImage / animalNameText ����.
        /// �⺻: unknownAnimalIcon / unknownAnimalName
        /// Ưȭ �ر�(HasRevealedSpecialAnimal): �ش� ���� Icon / AnimalDisplayName_
        /// </summary>
        private void ApplySpecialAnimalInfo()
        {
            bool hasRevealedAnimal = TryGetRevealedSpecialAnimal(CurrentToolId, out AnimalDataSO animalData)
                && animalData != null;

            if (animaImage != null)
            {
                Sprite animalIcon = hasRevealedAnimal ? animalData.Icon : null;

                if (animalIcon != null)
                {
                    animaImage.sprite = animalIcon;
                    animaImage.enabled = true;
                }
                else
                {
                    animaImage.sprite = unknownAnimalIcon;
                    animaImage.enabled = unknownAnimalIcon != null;
                }
            }

            if (animalNameText != null)
            {
                animalNameText.text = hasRevealedAnimal
                    ? animalData.AnimalDisplayName_
                    : unknownAnimalName;
            }
        }

        /// <summary>
        /// Ưȭ ���� �̸��� �رݵ� ���� ������ Ưȭ ���� �����͸� ��ȯ�մϴ�.
        /// </summary>
        private static bool TryGetRevealedSpecialAnimal(string toolId, out AnimalDataSO animalData)
        {
            animalData = null;

            if (string.IsNullOrEmpty(toolId) || InventoryManager_Tool.Instance == null)
                return false;

            if (!InventoryManager_Tool.Instance.TryGetToolSlot(toolId, out SlotData_Tool toolSlot))
                return false;

            if (toolSlot == null || !toolSlot.HasRevealedSpecialAnimal || toolSlot.ToolData == null)
                return false;

            string specialAnimalId = toolSlot.ToolData.SpecialAnimalId;
            if (string.IsNullOrEmpty(specialAnimalId))
                return false;

            if (InventoryManager_Animal.Instance == null)
                return false;

            animalData = InventoryManager_Animal.Instance.GetAnimalData(specialAnimalId);
            return animalData != null;
        }

        private static bool IsToolDiscovered(string toolId)
        {
            if (string.IsNullOrEmpty(toolId) || DexRecordManager.Instance == null)
                return false;

            return DexRecordManager.Instance.IsDiscovered(toolId);
        }

        /// <summary>
        /// �� ������ �ݱ� ��ư��.
        /// ���� �����͵� �Բ� �ʱ�ȭ�մϴ�.
        /// </summary>
        public void CloseToolDexPage()
        {
            currentToolData = null;

            ToolDexPagePanel.SetActive(false);
        }
    }
}
