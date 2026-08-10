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
        // Dex_Page 자신에 붙어 있으므로 SerializeField 없이 gameObject를 사용합니다.
        private GameObject ToolDexPagePanel => gameObject;

        [Header("도구 상세 정보")]
        [SerializeField] private Image toolIconImage;
        [SerializeField] private TMP_Text toolNameText;
        [SerializeField] private TMP_Text toolDescriptionText;

        [SerializeField] private Image animaImage;
        [SerializeField] private TMP_Text animalNameText;

        [Header("미해금 반영 사항")]
        [SerializeField] private Sprite unknownToolIcon;
        [SerializeField] private Sprite unknownAnimalIcon;
        [SerializeField] private string unknownToolName = "???";
        [SerializeField] private string unknownAnimalName = "???";
        [SerializeField, TextArea(2, 4)]
        private string unlokedMessage = "이 도구는 아직 발견하지 못 했어요.";

        private ToolDataSO currentToolData;

        public ToolDataSO CurrentToolData => currentToolData;

        public string CurrentToolId => currentToolData != null ? currentToolData.Id : string.Empty;

        /// <summary>
        /// 상세 페이지 패널이 현재 열려 있는지 여부
        /// </summary>
        public bool IsOpen => ToolDexPagePanel != null && ToolDexPagePanel.activeSelf;

        private void Awake()
        {
            ToolDexPagePanel.SetActive(false);
        }

        /// <summary>
        /// 도감 슬롯 클릭 시 선택한 도구의 정보로 상세 페이지를 갱신하고 패널을 엽니다.
        /// </summary>
        public void OpenToolDexPage(ToolDataSO toolData)
        {
            if (toolData == null)
            {
                Debug.LogWarning("[UIController_ToolDexPage] 표시할 도구 데이터가 없습니다.");
                return;
            }

            currentToolData = toolData;

            RefreshToolDexPage();

            ToolDexPagePanel.SetActive(true);
        }

        /// <summary>
        /// 현재 선택된 도구 데이터로 상세 페이지를 갱신합니다.
        /// 해금/미해금에 따라 아이콘·이름·설명을 분기합니다.
        /// </summary>
        private void RefreshToolDexPage()
        {
            if (currentToolData == null)
                return;

            // ----------------08.09.KAY (미해금 도구 상세 페이지 표시)------------------
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

            // 특화 해금 시 실제 동물 아이콘·이름, 아니면 unknown 표시
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

            // 미획득 도구도 animaImage/animalNameText는 unknown으로 표시
            ApplySpecialAnimalInfo();
        }

        /// <summary>
        /// animaImage / animalNameText 갱신.
        /// 기본: unknownAnimalIcon / unknownAnimalName
        /// 특화 해금(HasRevealedSpecialAnimal): 해당 동물 Icon / AnimalDisplayName_
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
        /// 특화 동물 이름이 해금된 도구 슬롯의 특화 동물 데이터를 반환합니다.
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
        /// 상세 페이지 닫기 버튼용.
        /// 선택 데이터도 함께 초기화합니다.
        /// </summary>
        public void CloseToolDexPage()
        {
            currentToolData = null;

            ToolDexPagePanel.SetActive(false);
        }
    }
}
