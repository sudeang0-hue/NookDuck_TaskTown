using System;
using System.Collections.Generic;
using Animal.Data;
using TaskTown.KDH;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// 마을 동물 배치 슬롯 목록 생성/갱신을 담당합니다.
    /// Edit 모드에서만 각 슬롯의 SetAnimal_btn / SetAnimal_Remove_btn이 활성화됩니다.
    /// </summary>
    public class UIController_VillageAnimalSet : MonoBehaviour
    {
        [Tooltip("스크롤뷰에 동물 슬롯이 추가될 root")]
        [FormerlySerializedAs("villageAnimalSetContant")]
        [SerializeField] private Transform villageAnimalSetContent;

        [SerializeField] private SlotUI_VillageAnimal slotPrefab;

        [Header("VillageAnimalSet_root 하위 버튼")]
        [SerializeField] private Button editSetAnimalButton;
        [SerializeField] private Button confirmSetAnimalButton;

        [Header("아이콘 조회 (미연결 시 Instance 사용)")]
        private InventoryManager_Animal animalInventory;

        [SerializeField] private TMP_Text setAnimalText;
        [SerializeField] private TMP_Text maxAnimalText;

        [Header("인스펙터 확인용")]
        [SerializeField] private List<SlotUI_VillageAnimal> activeSlots = new List<SlotUI_VillageAnimal>();

        private bool isEditMode;
        private Action<int> onSetAnimalClicked;
        private Action<int> onRemoveAnimalClicked;

        public Button EditSetAnimalButton => editSetAnimalButton;
        public Button ConfirmSetAnimalButton => confirmSetAnimalButton;

        // -----------------------------------------------------------------------------
        // [ 2026.08.03 - Choi - 튜토리얼 단계별 강조 연동 ]
        // 기능: 지정한 마을 배치 슬롯의 '+' 버튼을 튜토리얼 강조 대상으로 조회합니다.
        //       슬롯 목록 자체는 외부에 노출하지 않습니다.
        // -----------------------------------------------------------------------------
        public Button GetSetAnimalButtonAt(int index)
        {
            if (index < 0 || index >= activeSlots.Count)
                return null;

            SlotUI_VillageAnimal slot = activeSlots[index];
            return slot != null ? slot.SetAnimalButton : null;
        }

        private void Awake()
        {
            ResolveInventory();
            SetConfirmButtonActive(false);
        }

        /// <summary>
        /// unlockedCount만큼 슬롯을 Initialize로 생성/갱신합니다.
        /// editMode=false면 Set/Remove 버튼은 비활성 상태로 유지됩니다.
        /// </summary>
        public void RefreshSlots(IReadOnlyList<string> placedAnimalIds, int unlockedCount, bool editMode)
        {
            ResolveInventory();
            isEditMode = editMode;

            if (villageAnimalSetContent == null)
            {
                Debug.LogWarning("[UIController_VillageAnimalSet] villageAnimalSetContent가 없습니다.", this);
                return;
            }

            if (slotPrefab == null)
            {
                Debug.LogWarning("[UIController_VillageAnimalSet] slotPrefab이 없습니다.", this);
                return;
            }

            if (unlockedCount < 0)
                unlockedCount = 0;

            EnsureSlotCount(unlockedCount);

            for (int i = 0; i < unlockedCount; i++)
            {
                SlotUI_VillageAnimal slot = activeSlots[i];
                if (slot == null)
                    continue;

                string animalId = GetPlacedIdAt(placedAnimalIds, i);
                Sprite icon = null;
                string displayName = null;

                if (!string.IsNullOrEmpty(animalId))
                    TryResolveAnimalVisual(animalId, out icon, out displayName);

                slot.Initialize(
                    i,
                    animalId,
                    icon,
                    displayName,
                    isEditMode,
                    onSetAnimalClicked,
                    onRemoveAnimalClicked);
            }

            // 최대 배치 수는 unlockedCount와 동일 (레벨업 시 Refresh로 갱신)
            RefreshMaxAnimalText(unlockedCount);
        }

        /// <summary>
        /// 확정 배치 수(set)와 최대 배치 수(max/unlocked) 텍스트를 갱신합니다.
        /// setAnimalText는 Confirm(확정본) 기준으로 넘깁니다.
        /// </summary>
        public void RefreshCountTexts(int setCount, int maxCount)
        {
            RefreshSetAnimalText(setCount);
            RefreshMaxAnimalText(maxCount);
        }

        /// <summary>현재 마을에 확정 배치된 동물 수. Confirm 후 갱신.</summary>
        public void RefreshSetAnimalText(int setCount)
        {
            if (setAnimalText == null)
                return;

            setAnimalText.text = Mathf.Max(0, setCount).ToString();
        }

        /// <summary>현재 마을 레벨 기준 배치 가능 최대 수(= unlockedCount). 레벨업 시 갱신.</summary>
        public void RefreshMaxAnimalText(int maxCount)
        {
            if (maxAnimalText == null)
                return;

            maxAnimalText.text = "/ " + Mathf.Max(0, maxCount).ToString();
        }

        public void BindSlotCallbacks(Action<int> setAnimalClicked, Action<int> removeAnimalClicked)
        {
            onSetAnimalClicked = setAnimalClicked;
            onRemoveAnimalClicked = removeAnimalClicked;

            for (int i = 0; i < activeSlots.Count; i++)
            {
                if (activeSlots[i] == null)
                    continue;

                activeSlots[i].SetCallbacks(onSetAnimalClicked, onRemoveAnimalClicked);
            }
        }

        public void SetEditMode(bool editMode)
        {
            isEditMode = editMode;

            for (int i = 0; i < activeSlots.Count; i++)
            {
                if (activeSlots[i] != null && activeSlots[i].gameObject.activeSelf)
                    activeSlots[i].SetEditMode(isEditMode);
            }

            SetConfirmButtonActive(isEditMode);

        }

        public void SetConfirmButtonActive(bool active)
        {
            if (confirmSetAnimalButton == null)
                return;

            if (confirmSetAnimalButton.gameObject.activeSelf != active)
                confirmSetAnimalButton.gameObject.SetActive(active);

            confirmSetAnimalButton.interactable = active;
        }

        public void ClearAllSlots()
        {
            for (int i = 0; i < activeSlots.Count; i++)
            {
                if (activeSlots[i] != null)
                    activeSlots[i].BindEmpty(i);
            }
        }

        private void EnsureSlotCount(int unlockedCount)
        {
            while (activeSlots.Count < unlockedCount)
            {
                int index = activeSlots.Count;
                SlotUI_VillageAnimal created = Instantiate(slotPrefab, villageAnimalSetContent);
                created.name = $"VillageAnimalSlot_{index}";
                // Awake에서 버튼 비활성. Initialize는 RefreshSlots에서 호출.
                activeSlots.Add(created);
            }

            for (int i = 0; i < activeSlots.Count; i++)
            {
                if (activeSlots[i] == null)
                    continue;

                bool visible = i < unlockedCount;
                if (activeSlots[i].gameObject.activeSelf != visible)
                    activeSlots[i].gameObject.SetActive(visible);
            }
        }

        private static string GetPlacedIdAt(IReadOnlyList<string> placedAnimalIds, int index)
        {
            if (placedAnimalIds == null || index < 0 || index >= placedAnimalIds.Count)
                return string.Empty;

            return placedAnimalIds[index] ?? string.Empty;
        }

        private void TryResolveAnimalVisual(string animalId, out Sprite icon, out string displayName)
        {
            icon = null;
            displayName = animalId;

            if (animalInventory == null)
                return;

            AnimalDataSO data = animalInventory.GetAnimalData(animalId);
            if (data == null)
            {
                Debug.LogWarning(
                    $"[UIController_VillageAnimalSet] AnimalDataSO를 찾지 못했습니다: {animalId}",
                    this);
                return;
            }

            icon = data.Icon;
            displayName = data.DisplayName;
        }

        private void ResolveInventory()
        {
            if (animalInventory == null)
                animalInventory = InventoryManager_Animal.Instance;
        }
    }
}
