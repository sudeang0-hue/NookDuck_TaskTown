using System;
using Animal.Data;
using TaskTown.KDH;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// SetAnimalList_Panel용 작은 목록 슬롯.
    /// 클릭 시 배치할 동물 선택 콜백을 호출합니다.
    /// 이름 텍스트는 사용하지 않으며, 아이콘만 표시합니다.
    /// </summary>
    public class SlotUI_VillageAnimalSetList : SlotUIBase, IPointerClickHandler
    {
        [Header("표시")]
        [SerializeField] private Image animalIconImage;

        [Header("클릭")]
        [SerializeField] private Button coverButton;

        [Header("선택 표시 (선택)")]
        [Tooltip("선택됐을 때 켤 오브젝트. 없으면 무시합니다.")]
        [SerializeField] private GameObject selectedVisual;

        private SlotData_Animal currentSlotData;
        private Action<SlotData_Animal> onSelected;
        private bool isSelected;

        public SlotData_Animal CurrentSlotData => currentSlotData;
        public bool IsSelected => isSelected;

        private void Awake()
        {
            if (coverButton == null)
                coverButton = GetComponentInChildren<Button>(true);

            if (coverButton != null)
            {
                coverButton.onClick.RemoveListener(HandleSlotClicked);
                coverButton.onClick.AddListener(HandleSlotClicked);
            }

            SetSelected(false);
        }

        private void OnDestroy()
        {
            if (coverButton != null)
                coverButton.onClick.RemoveListener(HandleSlotClicked);
        }

        public void Initialize(SlotData_Animal slotData, Action<SlotData_Animal> selectedCallback = null)
        {
            onSelected = selectedCallback;
            currentSlotData = slotData;
            isSelected = false;

            if (slotData == null)
            {
                ClearViewOnly();
                return;
            }

            RefreshView();
            SetSelected(false);
        }

        public void SetSelectedCallback(Action<SlotData_Animal> selectedCallback)
        {
            onSelected = selectedCallback;
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;

            if (selectedVisual != null)
                selectedVisual.SetActive(selected);
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
                Debug.LogWarning("[SlotUI_VillageAnimalSetList] AnimalDataSO가 없습니다.", this);
                return;
            }

            // displayName은 부모 displayNameText용. 이 슬롯에서는 텍스트를 쓰지 않으므로 빈 문자열 전달.
            SetBaseInfo(data.Id, string.Empty, data.Icon);
            ApplyIcon(data);
        }

        private void ApplyIcon(AnimalDataSO data)
        {
            Image targetIcon = animalIconImage != null ? animalIconImage : iconImage;
            if (targetIcon != null)
            {
                targetIcon.sprite = data.Icon;
                targetIcon.enabled = data.Icon != null;
            }
        }

        private void HandleSlotClicked()
        {
            if (currentSlotData == null)
            {
                Debug.LogWarning("[SlotUI_VillageAnimalSetList] 슬롯 데이터가 없습니다.", this);
                return;
            }

            onSelected?.Invoke(currentSlotData);
        }

        private void ClearViewOnly()
        {
            currentId = string.Empty;

            Image targetIcon = animalIconImage != null ? animalIconImage : iconImage;
            if (targetIcon != null)
            {
                targetIcon.sprite = null;
                targetIcon.enabled = false;
            }

            SetSelected(false);
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
