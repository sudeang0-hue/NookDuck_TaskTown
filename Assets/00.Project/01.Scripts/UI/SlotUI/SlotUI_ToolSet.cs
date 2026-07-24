

using System;
using TaskTown.KDH;
using Tool.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    public class SlotUI_ToolSet : SlotUIBase, IPointerClickHandler
    {
        [Header("���ŵǴ� UI ����")]
        [SerializeField] private Image toolIconImage;
        [SerializeField] private TMP_Text toolNameText;

        [Header("Ŭ�� ���� ��ư")]
        [SerializeField] private Button coverButton;

        // �ʿ��ϸ� �ܺο��� �ݹ����ε� ���� �� �ְ�
        private Action<SlotData_Tool> onSelected;

        private SlotData_Tool currentSlotData;
        public SlotData_Tool CurrentSlotData => currentSlotData;


        private void Awake()
        {
            //--------------------------26.07.23 KDH ����--------------------------------
            if (coverButton == null)
                coverButton = GetComponentInChildren<Button>(true);

            if (coverButton != null)
                coverButton.onClick.AddListener(HandleSlotClicked);
            //---------------------------------------------------------
        }

        public void Initialize(SlotData_Tool slotData, Action<SlotData_Tool> selectedCallback = null)
        {
            onSelected = selectedCallback;
            currentSlotData = slotData;

            if (slotData == null)
            {
                ClearViewOnly();
                return;
            }

            RefreshView();
        }

       
        public void Refresh(SlotData_Tool slotData)
        {
            currentSlotData = slotData;

            if (slotData == null)
            {
                ClearViewOnly();
                return;
            }

            RefreshView();
        }

        public void SetSelectedCallback(Action<SlotData_Tool> selectedCallback)
        {
            onSelected = selectedCallback;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
                return;

            HandleSlotClicked();
        }

        private void RefreshView()
        {
            if (currentSlotData == null)
                return;

            ToolDataSO data = currentSlotData.ToolData;

            if (data == null)
            {
                Debug.LogWarning("[SlotUI_ToolSet] ToolDataSO�� ��� �ֽ��ϴ�.", this);
                return;
            }

            SetBaseInfo(data.Id, data.DisplayName, data.Icon);
            ApplyIconAndName(data);
        }

        private void ApplyIconAndName(ToolDataSO data)
        {
            if (toolIconImage != null)
            {
                toolIconImage.sprite = data.Icon;
                toolIconImage.enabled = data.Icon != null;
            }

            if (toolNameText != null)
            {
                //toolNameText.text = data.DisplayName;
                toolNameText.text = data.Id;
            }
        }

        /// <summary>
        /// cover_btn(또는 슬롯 클릭)에서만 호출됩니다.
        /// Set Tool은 목록만 열고, 실제 장착 선택은 이 경로에서 onSelected로 전달됩니다.
        /// </summary>
        private void HandleSlotClicked()
        {
            if (currentSlotData == null)
            {
                Debug.LogWarning("[SlotUI_ToolSet] 현재 슬롯에 도구 데이터가 없습니다.", this);
                return;
            }

            // 1) 월드 배치 UI가 있으면 선택 ToolId 동기화
            UIController_ToolPlacement placementUi = FindFirstObjectByType<UIController_ToolPlacement>();

            if (placementUi != null)
                placementUi.SetSelectedTool(currentSlotData.ToolId);

            // 2) 도구 목록 패널 콜백 → TryAssignAnimalToTool
            onSelected?.Invoke(currentSlotData);

            Debug.Log($"[SlotUI_ToolSet] 선택됨: {currentSlotData.ToolId}");
        }

        private void ClearViewOnly()
        {
            currentId = string.Empty;

            if (toolIconImage != null)
            {
                toolIconImage.sprite = null;
                toolIconImage.enabled = false;
            }

            if (toolNameText != null)
                toolNameText.text = string.Empty;

            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }

            if (displayNameText != null)
                displayNameText.text = string.Empty;
        }

        private void OnDestroy()
        {
            if (coverButton != null)
                coverButton.onClick.RemoveListener(HandleSlotClicked);
            
        }

        public override void Clear()
        {
            base.Clear();

            currentSlotData = null;
            onSelected = null;

            ClearViewOnly();
        }
    }
}
