

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
        [Header("갱신되는 UI 정보")]
        [SerializeField] private Image toolIconImage;
        [SerializeField] private TMP_Text toolNameText;

        private SlotData_Tool currentSlotData;
        private Action<SlotData_Tool> onSelected;

        public SlotData_Tool CurrentSlotData => currentSlotData;

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
                Debug.LogWarning("[SlotUI_ToolSet] ToolDataSO ? ?? ????.", this);
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
                toolNameText.text = data.DisplayName;
        }

        private void HandleSlotClicked()
        {
            Debug.Log("[SlotUI_ToolSet] 도구 선택 완료.");
            onSelected?.Invoke(currentSlotData);
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

        public override void Clear()
        {
            base.Clear();

            currentSlotData = null;
            onSelected = null;

            ClearViewOnly();
        }
    }
}
