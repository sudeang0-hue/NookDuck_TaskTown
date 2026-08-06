using System;
using System.Collections.Generic;
using Manager;
using TaskTown.KDH;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// 마을 동물 배치 UI.
    /// Edit 모드에서 드래프트로 배치/제거하고, Confirm_AnimalSet_btn으로 확정합니다.
    /// 목록은 이미 배치(드래프트 포함)된 animalId를 제외합니다.
    /// </summary>
    public class VillageAnimalSetUI_Manager : MonoBehaviour
    {
        private const int DefaultMaxCapacity = 20;
        private const int DefaultUnlockedAtLevel1 = 5;

        [Header("UI 참조")]
        [SerializeField] private UIPanelWindow villageAnimalSetPanel;
        [SerializeField] private UIController_VillageAnimalSet uiController;

        [Tooltip("Edit_AnimalSet_btn. 미연결 시 UIController 버튼 사용")]
        [SerializeField] private Button editAnimalSetButton;

        [Tooltip("Confirm_AnimalSet_btn. Edit 모드에서만 활성. 미연결 시 UIController 버튼 사용")]
        [SerializeField] private Button confirmAnimalSetButton;

        [Tooltip("SetAnimalList_Panel GameObject (폴백)")]
        [SerializeField] private GameObject setAnimalListPanel;

        [Tooltip("SetAnimalList_Panel의 UIController_VillageAnimalSetList")]
        [SerializeField] private UIController_VillageAnimalSetList animalSetList;

        [Header("인벤토리 (미연결 시 Instance 사용)")]
        private InventoryManager_Animal animalInventory;

        [Header("배치 버퍼 (확정본, 인덱스 = 슬롯 번호)")]
        [SerializeField] private int maxCapacity = DefaultMaxCapacity;

        [UnityEngine.Serialization.FormerlySerializedAs("villageAnimalId")]
        [SerializeField] private List<string> villageAnimalIds = new List<string>();

        [Header("Unlocked 규칙 (테스트)")]
        [SerializeField] private int unlockedAtLevel1 = DefaultUnlockedAtLevel1;
        //[SerializeField] private int[] unlockedByLevel = new int[0];                                        기존코드
        [SerializeField] private int[] unlockedByLevel = new int[10] { 5, 6, 7, 9, 10, 12, 13, 14, 16, 20 };  // 26.07.28 KDH 수정
        // 기존 VillageAnimalSet_Canvas_tab프리펩의 인스펙터에 unlockedByLevel을 위 배열의 숫자를 그대로 집어넣으면 잘 작동함
        [SerializeField] private int testTownLevel = 1;
        [SerializeField] private VillageUpgradeUI_Manager villageUpgradeUIManager;

        
        private bool openPanelOnPlay = false;
        private bool hidePanelOnAwake = true;

        /// <summary>Edit 중 작업용 드래프트. Confirm 시 확정본으로 복사됩니다.</summary>
        private List<string> editDraftIds;

        private bool isPanelOpen;
        private bool isEditMode;
        private bool isSubscribed;
        private int pendingSlotIndex = -1;

        public event Action<int> OnRequestOpenAnimalSetList;
        // 확정본(villageAnimalIds) 배치가 Confirm으로 변경되었을 때
        public event Action OnVillagePlacementChanged;

        public bool IsPanelOpen => isPanelOpen;
        public bool IsEditMode => isEditMode;
        public int MaxCapacity => Mathf.Max(0, maxCapacity);
        public int PendingSlotIndex => pendingSlotIndex;
        public IReadOnlyList<string> PlacedAnimalIds => GetActivePlacementIds();

        private void Awake()
        {
            ResolveInventory();
            EnsurePlacementBuffer();
            ResolveEditButton();
            ResolveConfirmButton();
            HideSetAnimalListPanel();
            SetConfirmButtonActive(false);

            if (hidePanelOnAwake && !openPanelOnPlay)
                HidePanel();
        }

        private void Start()
        {
            ResolveInventory();
            TryResolveVillageUpgradeManager();
            RestoreConfirmedPlacementFromVillageSystem();
            EnsurePlacementBuffer();
            ResolveEditButton();
            ResolveConfirmButton();
            BindEditButton();
            BindConfirmButton();
            SubscribeEvents();

            if (openPanelOnPlay)
                OpenPanel();
            else
                RefreshUI();
        }

        private void OnEnable()
        {
            BindEditButton();
            BindConfirmButton();
            SubscribeEvents();
            SubscribePanelEvents();
        }

        private void OnDisable()
        {
            // 컴포넌트 비활성 시에도 Edit 잔존 방지 (패널만 닫힌 경우는 PanelClosed에서 처리)
            DiscardEditOnPanelLeave();
            UnbindEditButton();
            UnbindConfirmButton();
            UnsubscribeEvents();
            UnsubscribePanelEvents();
        }

        private void OnDestroy()
        {
            UnbindEditButton();
            UnbindConfirmButton();
            UnsubscribeEvents();
            UnsubscribePanelEvents();
        }

        public int GetUnlockedSlotCount()
        {
            //------------------26.08.05 KAY 추가 (마을 레벨 설정 연동)---------------------------------
            // 배치 상한 진실 소스는 VillageSystemManager. 로컬 배열은 폴백으로만 사용.
            if (VillageSystemManager.Instance != null)
                return Mathf.Clamp(VillageSystemManager.Instance.GetUnlockedPlacementSlotCount(), 0, MaxCapacity);
            //-----------------------------------------------------------------------------

            return GetUnlockedSlotCount(GetCurrentTownLevel());
        }

        public int GetUnlockedSlotCount(int townLevel)
        {
            int level = Mathf.Max(1, townLevel);
            int unlocked;

            //if (unlockedByLevel != null && unlockedByLevel.Length > level)   기존코드
            if (unlockedByLevel != null && unlockedByLevel.Length > level - 1) // 26.07.28 KDH 수정
                //unlocked = unlockedByLevel[level];                          기존코드
                unlocked = unlockedByLevel[level - 1];                      // 26.07.28 KDH 수정
            else
                //unlocked = Mathf.Max(0, unlockedAtLevel1) + (level - 1);  기존코드
                unlocked = unlockedAtLevel1;                              // 26.07.28 KDH 수정

            return Mathf.Clamp(unlocked, 0, MaxCapacity);
        }

        public bool CanUseSlot(int index)
        {
            return index >= 0 && index < GetUnlockedSlotCount();
        }

        /// <summary>
        /// 현재 활성 배치(Edit 중이면 드래프트)에 이미 있는지 판별합니다. 중복 배치 가드.
        /// </summary>
        public bool IsPlaced(string animalId)
        {
            if (string.IsNullOrEmpty(animalId))
                return false;

            IReadOnlyList<string> ids = GetActivePlacementIds();
            for (int i = 0; i < ids.Count; i++)
            {
                if (ids[i] == animalId)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 확정본(villageAnimalIds)에만 있는지 판별합니다. Edit 드래프트는 무시합니다.
        /// Animal Inv 슬롯의 setVillage 아이콘용.
        /// </summary>
        public bool IsConfirmedPlaced(string animalId)
        {
            if (string.IsNullOrEmpty(animalId) || villageAnimalIds == null)
                return false;

            for (int i = 0; i < villageAnimalIds.Count; i++)
            {
                if (villageAnimalIds[i] == animalId)
                    return true;
            }

            return false;
        }

        public bool IsSlotEmpty(int index)
        {
            if (!CanUseSlot(index))
                return true;

            IReadOnlyList<string> ids = GetActivePlacementIds();
            if (index < 0 || index >= ids.Count)
                return true;

            return string.IsNullOrEmpty(ids[index]);
        }

        public int GetCurrentTownLevel()
        {
            if (VillageSystemManager.Instance != null)
                return VillageSystemManager.Instance.TownLevel;

            if (villageUpgradeUIManager != null)
                return Mathf.Max(1, villageUpgradeUIManager.UiTownLevel);

            return Mathf.Max(1, testTownLevel);
        }

        public void OpenPanel()
        {
            ShowPanel();
            isPanelOpen = true;
            // 패널 재오픈 시 Edit 잔존 없이 확정본으로 표시
            DiscardEditOnPanelLeave();
            RefreshUI();
        }

        public void ClosePanel()
        {
            DiscardEditOnPanelLeave();
            HideSetAnimalListPanel();
            HidePanel();
            isPanelOpen = false;
        }

        public void TogglePanel()
        {
            if (isPanelOpen)
                ClosePanel();
            else
                OpenPanel();
        }

        public void ToggleEditMode()
        {
            if (isEditMode)
                CancelEditMode();
            else
                EnterEditMode();
        }

        public void EnterEditMode()
        {
            Debug.Log(
                "[VillageAnimalSetUI.EnterEditMode] Edit 단계 시작 — 드래프트 생성",
                this);

            isEditMode = true;
            BeginEditDraft();
            SetConfirmButtonActive(true);
            ApplyEditModeToSlots();
            RefreshUI();
        }

        /// <summary>
        /// Edit 취소: 드래프트를 버리고 확정본으로 되돌립니다.
        /// </summary>
        public void CancelEditMode()
        {
            if (!isEditMode)
            {
                SetConfirmButtonActive(false);
                return;
            }

            Debug.Log(
                "[VillageAnimalSetUI.CancelEditMode] Edit 취소 — 드래프트 폐기, 확정본으로 복귀",
                this);

            isEditMode = false;
            pendingSlotIndex = -1;
            editDraftIds = null;
            HideSetAnimalListPanel();
            SetConfirmButtonActive(false);
            ApplyEditModeToSlots();
            RefreshUI();
        }

        /// <summary>
        /// Confirm 없이 패널을 떠날 때: 수정 중 드래프트를 버리고 Edit/Confirm 이전 상태로 되돌립니다.
        /// </summary>
        private void DiscardEditOnPanelLeave()
        {
            if (!isEditMode && editDraftIds == null)
            {
                SetConfirmButtonActive(false);
                return;
            }

            Debug.Log(
                "[VillageAnimalSetUI.DiscardEditOnPanelLeave] 패널 이탈 — Confirm 없이 Edit 초기화",
                this);

            CancelEditMode();
        }

        private void HandlePanelOpened()
        {
            isPanelOpen = true;
            DiscardEditOnPanelLeave();
            RefreshUI();
        }

        private void HandlePanelClosed()
        {
            // 탭/메뉴가 UIPanelWindow.ClosePanel만 호출해도 Edit이 남지 않도록 처리
            DiscardEditOnPanelLeave();
            HideSetAnimalListPanel();
            isPanelOpen = false;
        }

        private void SubscribePanelEvents()
        {
            if (villageAnimalSetPanel == null)
                return;

            villageAnimalSetPanel.OnPanelOpened -= HandlePanelOpened;
            villageAnimalSetPanel.OnPanelOpened += HandlePanelOpened;
            villageAnimalSetPanel.OnPanelClosed -= HandlePanelClosed;
            villageAnimalSetPanel.OnPanelClosed += HandlePanelClosed;
        }

        private void UnsubscribePanelEvents()
        {
            if (villageAnimalSetPanel == null)
                return;

            villageAnimalSetPanel.OnPanelOpened -= HandlePanelOpened;
            villageAnimalSetPanel.OnPanelClosed -= HandlePanelClosed;
        }

        /// <summary>
        /// Confirm_AnimalSet_btn: 현재 드래프트(배치/Remove 반영본)를 확정하고 UI를 Refresh합니다.
        /// </summary>
        public void OnClickConfirmAnimalSet()
        {
            if (!isEditMode)
                return;

            Debug.Log(
                "[VillageAnimalSetUI.OnClickConfirmAnimalSet] Confirm 단계 시작 — 드래프트 확정 시도",
                this);

            // Remove로 비운 슬롯 포함, 현재 드래프트를 확정본에 그대로 반영
            bool committed = CommitEditDraft();
            if (!committed)
            {
                Debug.LogWarning(
                    "[VillageAnimalSetUI.OnClickConfirmAnimalSet] 확정할 드래프트가 없습니다. Edit 상태를 종료합니다.",
                    this);
            }

            isEditMode = false;
            pendingSlotIndex = -1;
            editDraftIds = null;
            HideSetAnimalListPanel();
            SetConfirmButtonActive(false);
            ApplyEditModeToSlots();

            if (committed)
                SynchronizeConfirmedPlacementToVillageSystem();

            // 확정본 기준으로 슬롯 UI를 다시 그림 (비운 슬롯은 empty 유지)
            RefreshUI();

            if (committed)
            {
                OnVillagePlacementChanged?.Invoke();
                SaveManager.Instance?.SaveGame();
            }

            ClosePanel();
            Debug.Log("[VillageAnimalSetUI.OnClickConfirmAnimalSet] Confirm 완료 — RefreshUI 반영, 창 닫기", this);
        }

        public void OnClickSetAnimal(int slotIndex)
        {
            if (!isEditMode || !CanUseSlot(slotIndex) || !IsSlotEmpty(slotIndex))
                return;

            Debug.Log(
                $"[VillageAnimalSetUI.OnClickSetAnimal] 배치 준비 — 슬롯 {slotIndex} 목록 오픈",
                this);

            pendingSlotIndex = slotIndex;
            OnRequestOpenAnimalSetList?.Invoke(slotIndex);

            if (animalSetList != null)
            {
                // 이미 배치된(드래프트 포함) ID는 목록에서 제외
                animalSetList.Open(slotIndex, OnAnimalSelectedFromList, IsPlaced);
                return;
            }

            if (setAnimalListPanel != null)
            {
                setAnimalListPanel.SetActive(true);
                setAnimalListPanel.transform.SetAsLastSibling();
            }
            else
            {
                Debug.Log(
                    "[VillageAnimalSetUI.OnClickSetAnimal] animalSetList / setAnimalListPanel이 없습니다.",
                    this);
            }
        }

        /// <summary>
        /// 목록에서 동물 선택 → 드래프트 슬롯에 반영 (확정은 Confirm).
        /// </summary>
        private void OnAnimalSelectedFromList(int villageSlotIndex, string animalId)
        {
            Debug.Log(
                $"[VillageAnimalSetUI.OnAnimalSelectedFromList] 목록 선택 — slot={villageSlotIndex}, animalId={animalId}",
                this);

            if (!TryPlaceAt(villageSlotIndex, animalId))
                return;

            animalSetList?.Close();
        }

        public void OnClickRemoveAnimal(int slotIndex)
        {
            Debug.Log(
                $"[VillageAnimalSetUI.OnClickRemoveAnimal] Remove 버튼 — 슬롯 {slotIndex}",
                this);
            TryRemoveAt(slotIndex);
        }

        /// <summary>
        /// Edit 중 드래프트에 배치합니다. 중복 animalId는 거부합니다.
        /// </summary>
        public bool TryPlaceAt(int slotIndex, string animalId)
        {
            if (!isEditMode)
            {
                Debug.LogWarning(
                    "[VillageAnimalSetUI.TryPlaceAt] Edit 모드에서만 배치할 수 있습니다.",
                    this);
                return false;
            }

            EnsureEditDraft();

            if (!CanUseSlot(slotIndex))
                return false;

            if (string.IsNullOrEmpty(animalId))
                return false;

            if (!string.IsNullOrEmpty(editDraftIds[slotIndex]))
                return false;

            if (IsPlaced(animalId))
            {
                Debug.LogWarning(
                    $"[VillageAnimalSetUI.TryPlaceAt] 이미 배치된 동물입니다(중복 가드): {animalId}",
                    this);
                return false;
            }

            ResolveInventory();
            if (animalInventory != null && !animalInventory.TryGetAnimalSlot(animalId, out _))
            {
                Debug.LogWarning(
                    $"[VillageAnimalSetUI.TryPlaceAt] 인벤에 없는 동물은 배치할 수 없습니다: {animalId}",
                    this);
                return false;
            }

            editDraftIds[slotIndex] = animalId;
            pendingSlotIndex = -1;
            HideSetAnimalListPanel();
            RefreshUI();

            Debug.Log(
                $"[VillageAnimalSetUI.TryPlaceAt] 배치 단계 완료 — slot={slotIndex}, animalId={animalId}",
                this);
            return true;
        }

        /// <summary>
        /// Edit 중 배치 취소. 드래프트 슬롯을 비우고 UI를 Refresh합니다.
        /// </summary>
        public bool TryRemoveAt(int slotIndex)
        {
            if (!isEditMode)
            {
                Debug.LogWarning(
                    "[VillageAnimalSetUI.TryRemoveAt] Edit 모드에서만 배치를 취소할 수 있습니다.",
                    this);
                return false;
            }

            EnsureEditDraft();

            if (!CanUseSlot(slotIndex))
                return false;

            string removedId = editDraftIds[slotIndex];
            // 이미 비어 있어도 UI 동기화를 위해 Refresh 수행
            editDraftIds[slotIndex] = string.Empty;

            if (pendingSlotIndex == slotIndex)
                pendingSlotIndex = -1;

            RefreshUI();

            Debug.Log(
                $"[VillageAnimalSetUI.TryRemoveAt] Remove 단계 완료 — slot={slotIndex}, removedId={(string.IsNullOrEmpty(removedId) ? "(empty)" : removedId)}",
                this);
            return true;
        }

        [ContextMenu("Refresh UI")]
        public void RefreshUI()
        {
            if (uiController == null)
            {
                Debug.LogWarning("[VillageAnimalSetUI_Manager] uiController가 없습니다.", this);
                return;
            }

            EnsurePlacementBuffer();
            // Edit 중에는 기존 드래프트를 유지한 채 길이만 맞춘다. (확정본으로 덮어쓰지 않음)
            if (isEditMode)
                EnsureEditDraft();

            SanitizeActivePlacement();

            int unlocked = GetUnlockedSlotCount();
            IReadOnlyList<string> activeIds = GetActivePlacementIds();
            uiController.BindSlotCallbacks(OnClickSetAnimal, OnClickRemoveAnimal);
            uiController.RefreshSlots(activeIds, unlocked, isEditMode);
            uiController.SetConfirmButtonActive(isEditMode);

            // set: 확정본만 카운트 (Edit 드래프트는 Confirm 전까지 미반영)
            // max: unlockedCount (레벨업 시 OnVillageUpgradeStateChanged → RefreshUI)
            uiController.RefreshCountTexts(CountPlacedIds(villageAnimalIds), unlocked);
        }

        private static int CountPlacedIds(IReadOnlyList<string> ids)
        {
            if (ids == null)
                return 0;

            int count = 0;
            for (int i = 0; i < ids.Count; i++)
            {
                if (!string.IsNullOrEmpty(ids[i]))
                    count++;
            }

            return count;
        }

        private IReadOnlyList<string> GetActivePlacementIds()
        {
            if (isEditMode && editDraftIds != null)
                return editDraftIds;

            return villageAnimalIds;
        }

        private void BeginEditDraft()
        {
            EnsurePlacementBuffer();
            editDraftIds = new List<string>(MaxCapacity);
            for (int i = 0; i < MaxCapacity; i++)
            {
                string id = i < villageAnimalIds.Count ? villageAnimalIds[i] : string.Empty;
                editDraftIds.Add(id ?? string.Empty);
            }
        }

        /// <summary>
        /// 드래프트가 없으면 확정본에서 생성. 이미 있으면 Remove/Place 결과를 절대 덮어쓰지 않고 길이만 보정.
        /// </summary>
        private void EnsureEditDraft()
        {
            if (editDraftIds == null)
            {
                BeginEditDraft();
                return;
            }

            while (editDraftIds.Count < MaxCapacity)
                editDraftIds.Add(string.Empty);

            if (editDraftIds.Count > MaxCapacity)
                editDraftIds.RemoveRange(MaxCapacity, editDraftIds.Count - MaxCapacity);
        }

        /// <summary>
        /// 드래프트를 확정본에 원소 단위로 복사합니다. (SerializeField 리스트 참조 유지)
        /// </summary>
        private bool CommitEditDraft()
        {
            if (editDraftIds == null)
                return false;

            EnsurePlacementBuffer();

            // 확정 직전 Sanitize만 수행. BeginEditDraft/확정본 재로드 금지.
            SanitizeList(editDraftIds);

            int capacity = MaxCapacity;
            int placedCount = 0;
            for (int i = 0; i < capacity; i++)
            {
                string id = i < editDraftIds.Count ? editDraftIds[i] : string.Empty;
                villageAnimalIds[i] = id ?? string.Empty;
                if (!string.IsNullOrEmpty(villageAnimalIds[i]))
                    placedCount++;
            }

            Debug.Log(
                $"[VillageAnimalSetUI.CommitEditDraft] 확정 복사 완료 — placedCount={placedCount}/{capacity}",
                this);
            return true;
        }

        private void SanitizeActivePlacement()
        {
            if (isEditMode && editDraftIds != null)
                SanitizeList(editDraftIds);
            else
                SanitizeList(villageAnimalIds);
        }

        private void SanitizeList(List<string> ids)
        {
            if (ids == null)
                return;

            int unlocked = GetUnlockedSlotCount();
            var seen = new HashSet<string>();

            for (int i = 0; i < ids.Count; i++)
            {
                if (ids[i] == null)
                    ids[i] = string.Empty;

                if (i >= unlocked)
                    continue;

                string id = ids[i];
                if (string.IsNullOrEmpty(id))
                    continue;

                if (!seen.Add(id))
                    ids[i] = string.Empty;
            }
        }

        private void ApplyEditModeToSlots()
        {
            if (uiController == null)
                return;

            uiController.SetEditMode(isEditMode);
            uiController.SetConfirmButtonActive(isEditMode);
            if (!isEditMode)
                pendingSlotIndex = -1;
        }

        private void OnVillageUpgradeStateChanged()
        {
            RefreshUI();
        }

        private void EnsurePlacementBuffer()
        {
            if (villageAnimalIds == null)
                villageAnimalIds = new List<string>();

            int capacity = MaxCapacity;
            while (villageAnimalIds.Count < capacity)
                villageAnimalIds.Add(string.Empty);

            if (villageAnimalIds.Count > capacity)
                villageAnimalIds.RemoveRange(capacity, villageAnimalIds.Count - capacity);
        }

        // -----------------------------------------------------------------------------
        // [ 2026.08.06 - Choi - 마을 동물 배치 저장 연동 ]
        // 기능: 시스템 확정본을 UI 슬롯에 복원하고, Confirm 결과를 시스템과 즉시 동기화합니다.
        // -----------------------------------------------------------------------------
        private void RestoreConfirmedPlacementFromVillageSystem()
        {
            VillageSystemManager villageSystem = VillageSystemManager.Instance;
            if (villageSystem == null)
                return;

            EnsurePlacementBuffer();

            IReadOnlyList<string> restoredIds = villageSystem.PlacedAnimalIds;
            for (int i = 0; i < MaxCapacity; i++)
            {
                string animalId = i < restoredIds.Count
                    ? restoredIds[i] ?? string.Empty
                    : string.Empty;

                // 동물 인벤토리 복원이 끝난 뒤 존재하지 않는 ID는 빈 슬롯으로 보정합니다.
                if (!string.IsNullOrEmpty(animalId) &&
                    animalInventory != null &&
                    !animalInventory.TryGetAnimalSlot(animalId, out _))
                {
                    animalId = string.Empty;
                }

                villageAnimalIds[i] = animalId;
            }

            SanitizeList(villageAnimalIds);

            // 잘못된 ID, null, 용량 초과를 보정했다면 시스템 확정본도 같은 값으로 맞춥니다.
            if (!PlacementListsEqual(villageSystem.PlacedAnimalIds, villageAnimalIds))
                villageSystem.SetPlacedAnimalIds(villageAnimalIds);
        }

        private void SynchronizeConfirmedPlacementToVillageSystem()
        {
            VillageSystemManager villageSystem = VillageSystemManager.Instance;
            if (villageSystem == null)
            {
                Debug.LogWarning(
                    "[VillageAnimalSetUI_Manager] VillageSystemManager가 없어 배치 확정본을 저장 시스템에 전달하지 못했습니다.",
                    this);
                return;
            }

            villageSystem.SetPlacedAnimalIds(villageAnimalIds);
        }

        private static bool PlacementListsEqual(
            IReadOnlyList<string> left,
            IReadOnlyList<string> right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (left == null || right == null || left.Count != right.Count)
                return false;

            for (int i = 0; i < left.Count; i++)
            {
                if (!string.Equals(left[i] ?? string.Empty, right[i] ?? string.Empty, StringComparison.Ordinal))
                    return false;
            }

            return true;
        }

        private void ResolveEditButton()
        {
            if (editAnimalSetButton != null)
                return;

            if (uiController != null)
                editAnimalSetButton = uiController.EditSetAnimalButton;
        }

        private void ResolveConfirmButton()
        {
            if (confirmAnimalSetButton != null)
                return;

            if (uiController != null)
                confirmAnimalSetButton = uiController.ConfirmSetAnimalButton;
        }

        private void BindEditButton()
        {
            ResolveEditButton();
            if (editAnimalSetButton == null)
                return;

            editAnimalSetButton.onClick.RemoveListener(ToggleEditMode);
            editAnimalSetButton.onClick.AddListener(ToggleEditMode);
        }

        private void UnbindEditButton()
        {
            if (editAnimalSetButton == null)
                return;

            editAnimalSetButton.onClick.RemoveListener(ToggleEditMode);
        }

        private void BindConfirmButton()
        {
            ResolveConfirmButton();
            if (confirmAnimalSetButton == null)
                return;

            confirmAnimalSetButton.onClick.RemoveListener(OnClickConfirmAnimalSet);
            confirmAnimalSetButton.onClick.AddListener(OnClickConfirmAnimalSet);
        }

        private void UnbindConfirmButton()
        {
            if (confirmAnimalSetButton == null)
                return;

            confirmAnimalSetButton.onClick.RemoveListener(OnClickConfirmAnimalSet);
        }

        private void SetConfirmButtonActive(bool active)
        {
            ResolveConfirmButton();
            if (confirmAnimalSetButton == null)
                return;

            if (confirmAnimalSetButton.gameObject.activeSelf != active)
                confirmAnimalSetButton.gameObject.SetActive(active);

            confirmAnimalSetButton.interactable = active;
        }

        private void SubscribeEvents()
        {
            if (isSubscribed)
                return;

            TryResolveVillageUpgradeManager();

            if (villageUpgradeUIManager != null)
            {
                villageUpgradeUIManager.OnVillageUpgradeStateChanged -= OnVillageUpgradeStateChanged;
                villageUpgradeUIManager.OnVillageUpgradeStateChanged += OnVillageUpgradeStateChanged;
            }

            isSubscribed = villageUpgradeUIManager != null;
        }

        private void UnsubscribeEvents()
        {
            if (!isSubscribed)
                return;

            if (villageUpgradeUIManager != null)
                villageUpgradeUIManager.OnVillageUpgradeStateChanged -= OnVillageUpgradeStateChanged;

            isSubscribed = false;
        }

        private void ResolveInventory()
        {
            if (animalInventory == null)
                animalInventory = InventoryManager_Animal.Instance;
        }

        private void TryResolveVillageUpgradeManager()
        {
            if (villageUpgradeUIManager != null)
                return;

            villageUpgradeUIManager = FindFirstObjectByType<VillageUpgradeUI_Manager>();
        }

        private void ShowPanel()
        {
            if (villageAnimalSetPanel != null)
                villageAnimalSetPanel.OpenPanelDefaultPosition();
        }

        private void HidePanel()
        {
            if (villageAnimalSetPanel != null)
                villageAnimalSetPanel.ClosePanel();
        }

        private void HideSetAnimalListPanel()
        {
            if (animalSetList != null)
                animalSetList.Close();
            else if (setAnimalListPanel != null)
                setAnimalListPanel.SetActive(false);
        }

    }
}
