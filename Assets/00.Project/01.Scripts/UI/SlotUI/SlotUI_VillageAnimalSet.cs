using Animal.Data;
using System;
using TaskTown.KDH;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    public class SlotUI_VillageAnimalSet : SlotUIBase, IPointerClickHandler
    {
        [Header("현재 보유중인&&배치되지 않은 동물 아이콘")]
        [SerializeField] private Image animalIconImage;

        [Header("UI 클릭 범위 버튼")]
        [SerializeField] private Button coverButton;

        // 동물이 선택되었다는 이벤트 발생
        private Action<string> onSelected;

        private SlotData_Animal currentSlotData;
        public SlotData_Animal CurrentSlotData => currentSlotData;



        private void Awake()
        {
            if (coverButton == null)
                coverButton = GetComponentInChildren<Button>(true);

            if (coverButton != null)
                coverButton.onClick.AddListener(HandleSlotClicked);
        }


        public void Initialize(SlotData_Animal slotData, Action<string> selectedCallback = null)
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

        public void Refresh(SlotData_Animal slotData)
        {
            currentSlotData = slotData;

            if (slotData == null)
            {
                ClearViewOnly();
                return;
            }

            RefreshView();
        }


        public void SetSelectedCallback(Action<string> selectedCallback)
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

            AnimalDataSO data = currentSlotData.AnimalData;

            if (data == null)
            {
                Debug.LogWarning("[SlotUI_VillageAnimalSet] AnimalDataSO 데이터가 없습니다.", this);
                return;
            }

            SetBaseInfo(data.Id, data.DisplayName, data.Icon);
            ApplyIconAndName(data);
        }

        private void ApplyIconAndName(AnimalDataSO data)
        {
            if (animalIconImage != null)
            {
                animalIconImage.sprite = data.Icon;
                animalIconImage.enabled = data.Icon != null;
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
                Debug.LogWarning("[SlotUI_VillageAnimalSet] 현재 슬롯에 동물 데이터가 없습니다.", this);
                return;
            }


            Debug.Log($"[SlotUI_VillageAnimalSet] 선택됨: {currentSlotData.AnimalId}");
        }

        private void ClearViewOnly()
        {
            currentId = string.Empty;

            if (animalIconImage != null)
            {
                animalIconImage.sprite = null;
                animalIconImage.enabled = false;
            }

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