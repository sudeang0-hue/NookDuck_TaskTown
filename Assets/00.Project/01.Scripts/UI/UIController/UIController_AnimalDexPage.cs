using System.Collections.Generic;
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
        [Tooltip("동물 타입 텍스트(오리,개,고양이)")]
        [SerializeField] private TMP_Text animalNameText;
        [Tooltip("동물 실제 이름 텍스트(신사오리,퍼피,케이트)")]
        [SerializeField] private TMP_Text animalDisplayNameText;

        [SerializeField] private Image toolImage;
        [SerializeField] private TMP_Text toolNameText;

        [SerializeField] private TMP_Text animalDescriptionText; // 동물 설명
        [SerializeField] private TMP_Text hintDescriptionText; // 도구에 대한 힌트

        [Header("미해금 반영 사항")]
        [SerializeField] private Sprite unknownAnimalIcon;
        [SerializeField] private Sprite unknownToolIcon;
        [SerializeField] private string unknownAnimalName = "-";
        [SerializeField] private string unknownAnimalDispalName = "???";
        [SerializeField] private string unknownToollName = "???";
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

            if (animalDisplayNameText != null)
                animalDisplayNameText.text = currentAnimalData.AnimalDisplayName_;

            if (animalDescriptionText != null)
                animalDescriptionText.text = currentAnimalData.AnimalDescription;

            // 힌트 텍스트가 추가되면 AnimalDescription 부분을 해당 항목으로 변경합니다.
            if (hintDescriptionText != null)
                hintDescriptionText.text = currentAnimalData.AnimalHintDescription;

            // 특화 해금 시 실제 도구 아이콘·이름, 아니면 unknown 표시
            ApplySpecialToolInfo();
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

            if (animalDisplayNameText != null)
                animalDisplayNameText.text = unknownAnimalDispalName;

            if (animalDescriptionText != null)
            {
                animalDescriptionText.text = unavailableOnDifficulty
                    ? unlokedDifficultyMessage
                    : unlokedMessage;
            }

            string emptytxt = " ";
            string hinttxt = " ";
            // 힌트 텍스트 추가
            if (hintDescriptionText != null)
            {
                hintDescriptionText.text = unavailableOnDifficulty
                    ? hinttxt
                    : emptytxt;
            }

            // 미획득 동물도 toolImage/toolNameText는 unknown으로 표시
            ApplySpecialToolInfo();
        }

        /// <summary>
        /// toolImage / toolNameText 갱신.
        /// 기본: unknownToolIcon / unknownToollName
        /// 특화 해금(HasRevealedSpecialAnimal): 해당 도구 Icon / DisplayName
        /// </summary>
        private void ApplySpecialToolInfo()
        {
            SlotData_Tool revealedToolSlot = FindRevealedSpecialToolSlot(CurrentAnimalId);
            bool hasRevealedTool = revealedToolSlot != null && revealedToolSlot.ToolData != null;

            if (toolImage != null)
            {
                Sprite toolIcon = hasRevealedTool ? revealedToolSlot.ToolData.Icon : null;

                if (toolIcon != null)
                {
                    toolImage.sprite = toolIcon;
                    toolImage.enabled = true;
                }
                else
                {
                    toolImage.sprite = unknownToolIcon;
                    toolImage.enabled = unknownToolIcon != null;
                }
            }

            if (toolNameText != null)
            {
                toolNameText.text = hasRevealedTool
                    ? revealedToolSlot.ToolData.DisplayName
                    : unknownToollName;
            }
        }

        /// <summary>
        /// 특화 동물 이름이 해금된 도구 슬롯을 찾습니다. 없으면 null.
        /// </summary>
        private static SlotData_Tool FindRevealedSpecialToolSlot(string animalId)
        {
            if (string.IsNullOrEmpty(animalId) || InventoryManager_Tool.Instance == null)
                return null;

            IReadOnlyList<SlotData_Tool> toolSlots = InventoryManager_Tool.Instance.ToolSlotsList;
            if (toolSlots == null)
                return null;

            for (int i = 0; i < toolSlots.Count; i++)
            {
                SlotData_Tool toolSlot = toolSlots[i];
                if (toolSlot == null || toolSlot.ToolData == null)
                    continue;

                if (!toolSlot.HasRevealedSpecialAnimal)
                    continue;

                if (toolSlot.ToolData.SpecialAnimalId == animalId)
                    return toolSlot;
            }

            return null;
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
