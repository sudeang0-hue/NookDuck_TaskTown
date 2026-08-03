using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// 마을 동물 배치 슬롯 1칸 표시.
    /// 생성 직후 Set/Remove는 비활성. Edit 모드에서만 활성화됩니다.
    /// SetAnimal_btn: SetAnimalList_Panel 오픈 (배치 완료는 목록 슬롯 클릭에서 처리).
    /// </summary>
    public class SlotUI_VillageAnimal : SlotUIBase
    {
        [Header("마을 배치 슬롯")]
        [FormerlySerializedAs("AnimalIconImage")]
        [SerializeField] private Image animalIconImage;
        [SerializeField] private TMP_Text animalNameText;

        [Header("빈/배치 시각 (Image)")]
        [Tooltip("빈 슬롯일 때 animal_icon에 적용할 Image 설정 소스")]
        [SerializeField] private Image emptyVisual;

        [Tooltip("동물이 배치되었을 때 표시할 Image")]
        [SerializeField] private Image filledVisual;

        [Header("편집 버튼 (Edit 모드에서만 활성)")]
        [Tooltip("SetAnimal_Remove_btn")]
        [FormerlySerializedAs("removeAnimal")]
        [SerializeField] private Button removeAnimalButton;

        [Tooltip("SetAnimal_btn — SetAnimalList_Panel 오픈용")]
        [FormerlySerializedAs("setAnimalListOpenButton")]
        [SerializeField] private Button setAnimalButton;

        private int slotIndex = -1;
        private bool isEditMode;
        private Action<int> onSetAnimalClicked;
        private Action<int> onRemoveAnimalClicked;

        // emptyVisual에 동물 아이콘이 덮어씌워지지 않도록 초기 스프라이트 보관
        private Sprite defaultEmptySprite;
        private Color defaultEmptyColor = Color.white;
        private bool hasCachedEmptyVisual;

        public int SlotIndex => slotIndex;
        public bool IsEmpty => string.IsNullOrEmpty(currentId);

        // -----------------------------------------------------------------------------
        // [ 2026.08.03 - Choi - 튜토리얼 단계별 강조 연동 ]
        // 기능: Edit 모드에서 표시되는 빈 슬롯의 '+' 버튼을 튜토리얼 손가락 강조
        //       대상으로 사용할 수 있도록 읽기 전용 참조만 공개합니다.
        // -----------------------------------------------------------------------------
        public Button SetAnimalButton => setAnimalButton;

        private void Awake()
        {
            CacheEmptyVisualDefaults();
            BindButtonListeners();
            // 생성 직후: 두 버튼 모두 비활성
            ForceButtonsInactive();
        }

        private void OnDestroy()
        {
            UnbindButtonListeners();
        }

        /// <summary>
        /// 생성/갱신 시 한 번에 초기화합니다.
        /// </summary>
        public void Initialize(
            int index,
            string animalId,
            Sprite icon,
            string displayName,
            bool editMode,
            Action<int> setAnimalClicked,
            Action<int> removeAnimalClicked)
        {
            BindButtonListeners();

            onSetAnimalClicked = setAnimalClicked;
            onRemoveAnimalClicked = removeAnimalClicked;
            isEditMode = editMode;

            if (string.IsNullOrEmpty(animalId))
                BindEmpty(index);
            else
                Bind(index, animalId, icon, displayName);

            SetEditMode(editMode);
        }

        public void SetCallbacks(Action<int> setAnimalClicked, Action<int> removeAnimalClicked)
        {
            onSetAnimalClicked = setAnimalClicked;
            onRemoveAnimalClicked = removeAnimalClicked;
        }

        public void SetEditMode(bool editing)
        {
            isEditMode = editing;
            RefreshEditButtons();
        }

        public void Bind(int index, string animalId, Sprite icon, string displayName = null)
        {
            slotIndex = index;

            if (string.IsNullOrEmpty(animalId))
            {
                BindEmpty(index);
                return;
            }

            // iconImage가 emptyVisual과 동일하므로, 동물 아이콘을 base에 넣지 않음
            // (Remove 시 empty가 동물 이미지로 남는 현상 방지)
            currentId = animalId;

            TMP_Text targetName = animalNameText != null ? animalNameText : displayNameText;
            if (targetName != null)
                targetName.text = displayName ?? string.Empty;

            ApplyFilledVisual(icon);
            SetEmptyState(false);
            RefreshEditButtons();
        }

        public void BindEmpty(int index)
        {
            slotIndex = index;
            Clear();
            SetEmptyState(true);
            RefreshEditButtons();
        }

        public override void Clear()
        {
            currentId = string.Empty;

            if (displayNameText != null)
                displayNameText.text = string.Empty;

            if (animalNameText != null)
                animalNameText.text = string.Empty;

            // 배치 아이콘(fill)을 완전히 비움
            ClearFilledVisual();
            RestoreEmptyVisualDefaults();
        }

        private void CacheEmptyVisualDefaults()
        {
            if (hasCachedEmptyVisual || emptyVisual == null)
                return;

            defaultEmptySprite = emptyVisual.sprite;
            defaultEmptyColor = emptyVisual.color;
            hasCachedEmptyVisual = true;
        }

        private void RestoreEmptyVisualDefaults()
        {
            CacheEmptyVisualDefaults();
            if (emptyVisual == null || !hasCachedEmptyVisual)
                return;

            emptyVisual.sprite = defaultEmptySprite;
            emptyVisual.color = defaultEmptyColor;
        }

        private void ClearFilledVisual()
        {
            Image fill = GetFilledImage();
            if (fill == null)
                return;

            fill.sprite = null;
            fill.enabled = false;
        }

        private Image GetFilledImage()
        {
            if (animalIconImage != null)
                return animalIconImage;

            if (filledVisual != null)
                return filledVisual;

            // iconImage가 empty와 같으면 fill로 쓰지 않음
            if (iconImage != null && iconImage != emptyVisual)
                return iconImage;

            return null;
        }

        private void ApplyFilledVisual(Sprite icon)
        {
            Image fill = GetFilledImage();
            if (fill == null)
                return;

            if (icon != null)
            {
                fill.sprite = icon;
                fill.color = Color.white;
                fill.enabled = true;
            }
            else
            {
                fill.sprite = null;
                fill.enabled = false;
            }
        }

        /// <summary>
        /// 빈 슬롯: emptyVisual만 표시. 배치 슬롯: filledVisual만 표시.
        /// fill에 empty 스프라이트를 복사해 다시 켜지 않음.
        /// </summary>
        private void SetEmptyState(bool isEmpty)
        {
            if (isEmpty)
                RestoreEmptyVisualDefaults();

            if (emptyVisual != null)
                emptyVisual.enabled = isEmpty;

            Image fill = GetFilledImage();
            if (fill != null)
            {
                if (isEmpty)
                {
                    fill.sprite = null;
                    fill.enabled = false;
                }
                else
                {
                    fill.enabled = fill.sprite != null;
                }
            }

            // filledVisual이 animalIconImage와 다르면 동기화
            if (filledVisual != null && filledVisual != fill)
            {
                if (isEmpty)
                {
                    filledVisual.sprite = null;
                    filledVisual.enabled = false;
                }
                else
                {
                    filledVisual.enabled = filledVisual.sprite != null;
                }
            }
        }

        /// <summary>
        /// Edit 모드 OFF: Set/Remove 모두 비활성.
        /// Edit 모드 ON: 빈 슬롯=Set, 배치 슬롯=Remove 활성.
        /// </summary>
        private void RefreshEditButtons()
        {
            if (!isEditMode)
            {
                ForceButtonsInactive();
                return;
            }

            bool showSet = IsEmpty;
            bool showRemove = !IsEmpty;

            SetButtonActive(setAnimalButton, showSet);
            SetButtonActive(removeAnimalButton, showRemove);
        }

        private void ForceButtonsInactive()
        {
            SetButtonActive(setAnimalButton, false);
            SetButtonActive(removeAnimalButton, false);
        }

        private static void SetButtonActive(Button button, bool active)
        {
            if (button == null)
                return;

            if (button.gameObject.activeSelf != active)
                button.gameObject.SetActive(active);

            button.interactable = active;
        }

        private void BindButtonListeners()
        {
            if (setAnimalButton != null)
            {
                setAnimalButton.onClick.RemoveListener(HandleSetAnimalClicked);
                setAnimalButton.onClick.AddListener(HandleSetAnimalClicked);
            }

            if (removeAnimalButton != null)
            {
                removeAnimalButton.onClick.RemoveListener(HandleRemoveAnimalClicked);
                removeAnimalButton.onClick.AddListener(HandleRemoveAnimalClicked);
            }
        }

        private void UnbindButtonListeners()
        {
            if (setAnimalButton != null)
                setAnimalButton.onClick.RemoveListener(HandleSetAnimalClicked);

            if (removeAnimalButton != null)
                removeAnimalButton.onClick.RemoveListener(HandleRemoveAnimalClicked);
        }

        private void HandleSetAnimalClicked()
        {
            // Edit 모드 + 빈 슬롯에서만: 목록 패널 오픈 요청
            if (!isEditMode || !IsEmpty)
                return;

            onSetAnimalClicked?.Invoke(slotIndex);
        }

        private void HandleRemoveAnimalClicked()
        {
            if (!isEditMode || IsEmpty)
                return;

            onRemoveAnimalClicked?.Invoke(slotIndex);
        }
    }
}
