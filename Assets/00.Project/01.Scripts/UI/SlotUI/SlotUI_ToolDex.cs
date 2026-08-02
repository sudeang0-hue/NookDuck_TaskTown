
using TMPro;
using Tool.Data;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class SlotUI_ToolDex : SlotUIBase
    {

        [Header("미해금 상태 도감 슬롯")]
        [SerializeField] private Sprite unknownToolIcon;
        [SerializeField] private string unknownToolName = "???";

        [Header("해금 상태 도구 정보")]
        [SerializeField] private Image ToolIconImage;
        [SerializeField] private TMP_Text ToolNameText;

        [Header("클릭 범위 버튼")]
        [SerializeField] private Button coverButton;

        private ToolDataSO toolData;
        private UIController_ToolDexPage toolPageController;
        private bool isUnlocked;

        public ToolDataSO ToolData => toolData;
        public string ToolId => toolData != null ? toolData.Id : string.Empty;


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
        /// 도감 슬롯에 동물 데이터와 상세 페이지 Controller를 연결
        /// </summary>
        public void Initialize(ToolDataSO data, UIController_ToolDexPage pageController, bool unlocked)
        {
            if (data == null)
            {
                Debug.LogWarning("[SlotUI_AnimalDex] 연결할 AnimalDataSO가 없습니다.");
                return;
            }

            if (coverButton == null)
            {
                Debug.LogWarning("[SlotUI_AnimalDex] coverButton이 연결되지 않았습니다.");
                return;
            }

            toolData = data;
            toolPageController = pageController;
            isUnlocked = unlocked;

            RefreshView();
        }

        /// <summary>
        /// 도감 슬롯의 잠금 상태를 변경하고 이름과 아이콘을 다시 표시합니다.
        /// </summary>
        public void SetUnlocked(bool unlocked)
        {
            isUnlocked = unlocked;
            RefreshView();
        }

        /// <summary>
        /// 현재 동물 데이터로 슬롯 비주얼 갱신
        /// </summary>
        private void RefreshView()
        {
            if (toolData == null)
                return;

            if (isUnlocked)
            {
                ShowUnlockedView();
            }
            else
            {
                ShowLockedView();
            }

            if (coverButton != null)
            {
                coverButton.interactable = isUnlocked;
            }

        }

        /// <summary>
        /// 해금되면 보여주는 UIController_AnimalInvPage
        /// </summary>
        private void ShowUnlockedView()
        {
            if (ToolIconImage != null)
            {
                ToolIconImage.sprite = toolData.Icon;
                ToolIconImage.enabled = toolData.Icon != null;
            }

            if (ToolNameText != null)
            {
                ToolNameText.text = toolData.DisplayName;
            }
        }

        /// <summary>
        /// 미해금 상태에서 보여주는 UIController_AnimalInvPage
        /// </summary>
        private void ShowLockedView()
        {
            if (ToolIconImage != null)
            {
                ToolIconImage.sprite = unknownToolIcon;
                ToolIconImage.enabled = unknownToolIcon != null;
            }

            if (ToolNameText != null)
            {
                ToolNameText.text = unknownToolName;
            }
        }


        /// <summary>
        /// 슬롯 클릭 시 도감 상세 페이지를 토글합니다.
        /// 같은 동물이 이미 열려 있으면 Close, 아니면 Open합니다.
        /// </summary>
        private void HandleSlotClicked()
        {
            if (toolData == null)
            {
                Debug.LogWarning("[SlotUI_AnimalDex] 현재 슬롯에 동물 데이터가 없습니다.", this);
                return;
            }

            if (toolPageController == null)
            {
                Debug.LogWarning("[SlotUI_AnimalDex] UIController_AnimalDexPage 가 연결되지 않았습니다.", this);
                return;
            }

            if (toolPageController.IsOpen &&
                toolPageController.CurrentToolId == toolData.Id)
            {
                toolPageController.CloseToolDexPage();
                return;
            }

            toolPageController.OpenToolDexPage(toolData);
        }


    }
}