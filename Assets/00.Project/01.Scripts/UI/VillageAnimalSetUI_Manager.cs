using System;
using System.Collections;
using System.Collections.Generic;
using Manager;
using TaskTown.KDH;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// 마을 동물 배치 UI 제어 및 서브 패널 슬라이딩 연출을 담당하는 매니저 클래스
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

        // =========================================================================
        // [ 서브 패널 슬라이드 연출 설정 ]
        // =========================================================================
        [Header("서브 패널 슬라이드 연출 설정")]
        [Tooltip("스르륵 나타날 SetAnimalList_root의 RectTransform")]
        [SerializeField] private RectTransform subPanelRect;

        [Tooltip("메인 UI 뒤에 숨어있을 때의 로컬 좌표")]
        [SerializeField] private Vector2 hiddenAnchoredPos = Vector2.zero;

        [Tooltip("오른쪽(또는 지정 위치)으로 튀어나왔을 때의 로컬 좌표")]
        [SerializeField] private Vector2 visibleAnchoredPos = new Vector2(450f, 0f);

        [Tooltip("슬라이드 애니메이션 재생 시간 (초)")]
        [SerializeField] private float slideDuration = 0.25f;

        private Coroutine slideCoroutine;
        // =========================================================================

        [Header("인벤토리 (미연결 시 Instance 사용)")]
        private InventoryManager_Animal animalInventory;

        [Header("배치 버퍼 (확정본, 인덱스 = 슬롯 번호)")]
        [SerializeField] private int maxCapacity = DefaultMaxCapacity;

        [UnityEngine.Serialization.FormerlySerializedAs("villageAnimalId")]
        [SerializeField] private List<string> villageAnimalIds = new List<string>();

        [Header("Unlocked 규칙 (테스트)")]
        [SerializeField] private int unlockedAtLevel1 = DefaultUnlockedAtLevel1;
        [SerializeField] private int[] unlockedByLevel = new int[10] { 5, 6, 7, 9, 10, 12, 13, 14, 16, 20 };
        [SerializeField] private int testTownLevel = 1;
        [SerializeField] private VillageUpgradeUI_Manager villageUpgradeUIManager;

        private bool openPanelOnPlay = false;
        private bool hidePanelOnAwake = true;

        private List<string> editDraftIds;

        private bool isPanelOpen;
        private bool isEditMode;
        private bool isSubscribed;
        private int pendingSlotIndex = -1;

        public event Action<int> OnRequestOpenAnimalSetList;
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
            ResolveSubPanelRect(); // 서브 패널 RectTransform 자동으로 감지

            // 초기 위치 숨김 처리 (애니메이션 없이 즉시 이동)
            if (subPanelRect != null)
                subPanelRect.anchoredPosition = hiddenAnchoredPos;

            HideSetAnimalListPanelImmediate();
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
            DiscardEditOnPanelLeave();
            UnbindEditButton();
            UnbindConfirmButton();
            UnsubscribeEvents();
            UnsubscribePanelEvents();

            // 비활성화 시 진행 중인 슬라이드 코루틴 정지 및 상태 복구
            if (slideCoroutine != null)
            {
                StopCoroutine(slideCoroutine);
                slideCoroutine = null;
            }

            // 비활성화될 때 UI 위치를 즉시 숨김 상태로 초기화
            if (subPanelRect != null)
            {
                subPanelRect.anchoredPosition = hiddenAnchoredPos;
                subPanelRect.gameObject.SetActive(false);
            }
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
            if (VillageSystemManager.Instance != null)
                return Mathf.Clamp(VillageSystemManager.Instance.GetUnlockedPlacementSlotCount(), 0, MaxCapacity);

            return GetUnlockedSlotCount(GetCurrentTownLevel());
        }

        public int GetUnlockedSlotCount(int townLevel)
        {
            int level = Mathf.Max(1, townLevel);
            int unlocked;

            if (unlockedByLevel != null && unlockedByLevel.Length > level - 1)
                unlocked = unlockedByLevel[level - 1];
            else
                unlocked = unlockedAtLevel1;

            return Mathf.Clamp(unlocked, 0, MaxCapacity);
        }

        public bool CanUseSlot(int index) => index >= 0 && index < GetUnlockedSlotCount();

        public bool IsPlaced(string animalId)
        {
            if (string.IsNullOrEmpty(animalId)) return false;
            IReadOnlyList<string> ids = GetActivePlacementIds();
            for (int i = 0; i < ids.Count; i++)
            {
                if (ids[i] == animalId) return true;
            }
            return false;
        }

        public bool IsConfirmedPlaced(string animalId)
        {
            if (string.IsNullOrEmpty(animalId) || villageAnimalIds == null) return false;
            for (int i = 0; i < villageAnimalIds.Count; i++)
            {
                if (villageAnimalIds[i] == animalId) return true;
            }
            return false;
        }

        public bool IsSlotEmpty(int index)
        {
            if (!CanUseSlot(index)) return true;
            IReadOnlyList<string> ids = GetActivePlacementIds();
            if (index < 0 || index >= ids.Count) return true;
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
            isEditMode = true;
            BeginEditDraft();
            SetConfirmButtonActive(true);
            ApplyEditModeToSlots();
            RefreshUI();
        }

        public void CancelEditMode()
        {
            if (!isEditMode)
            {
                SetConfirmButtonActive(false);
                return;
            }

            isEditMode = false;
            pendingSlotIndex = -1;
            editDraftIds = null;
            HideSetAnimalListPanel();
            SetConfirmButtonActive(false);
            ApplyEditModeToSlots();
            RefreshUI();
        }

        private void DiscardEditOnPanelLeave()
        {
            if (!isEditMode && editDraftIds == null)
            {
                SetConfirmButtonActive(false);
                return;
            }
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
            DiscardEditOnPanelLeave();
            HideSetAnimalListPanel();
            isPanelOpen = false;
        }

        private void SubscribePanelEvents()
        {
            if (villageAnimalSetPanel == null) return;
            villageAnimalSetPanel.OnPanelOpened -= HandlePanelOpened;
            villageAnimalSetPanel.OnPanelOpened += HandlePanelOpened;
            villageAnimalSetPanel.OnPanelClosed -= HandlePanelClosed;
            villageAnimalSetPanel.OnPanelClosed += HandlePanelClosed;
        }

        private void UnsubscribePanelEvents()
        {
            if (villageAnimalSetPanel == null) return;
            villageAnimalSetPanel.OnPanelOpened -= HandlePanelOpened;
            villageAnimalSetPanel.OnPanelClosed -= HandlePanelClosed;
        }

        public void OnClickConfirmAnimalSet()
        {
            if (!isEditMode) return;

            bool committed = CommitEditDraft();
            isEditMode = false;
            pendingSlotIndex = -1;
            editDraftIds = null;
            HideSetAnimalListPanel();
            SetConfirmButtonActive(false);
            ApplyEditModeToSlots();

            if (committed)
                SynchronizeConfirmedPlacementToVillageSystem();

            RefreshUI();

            if (committed)
            {
                OnVillagePlacementChanged?.Invoke();
                SaveManager.Instance?.SaveGame();
            }

            ClosePanel();
        }

        /// <summary>
        /// (+) 버튼 클릭 시 서브 목록 패널을 슬라이드로 엽니다.
        /// </summary>
        public void OnClickSetAnimal(int slotIndex)
        {
            if (!isEditMode || !CanUseSlot(slotIndex) || !IsSlotEmpty(slotIndex))
                return;

            pendingSlotIndex = slotIndex;
            OnRequestOpenAnimalSetList?.Invoke(slotIndex);

            if (animalSetList != null)
            {
                animalSetList.Open(slotIndex, OnAnimalSelectedFromList, IsPlaced);
            }
            else if (setAnimalListPanel != null)
            {
                setAnimalListPanel.SetActive(true);
            }

            // 서브 패널 슬라이드 오픈 연출 실행
            AnimateSubPanel(true);
        }

        private void OnAnimalSelectedFromList(int villageSlotIndex, string animalId)
        {
            if (!TryPlaceAt(villageSlotIndex, animalId))
                return;

            // 동물 선택 성공 시 슬라이드 닫기 연출 후 종료
            HideSetAnimalListPanel();
        }

        public void OnClickRemoveAnimal(int slotIndex)
        {
            TryRemoveAt(slotIndex);
        }

        public bool TryPlaceAt(int slotIndex, string animalId)
        {
            if (!isEditMode) return false;

            EnsureEditDraft();
            if (!CanUseSlot(slotIndex) || string.IsNullOrEmpty(animalId)) return false;
            if (!string.IsNullOrEmpty(editDraftIds[slotIndex])) return false;
            if (IsPlaced(animalId)) return false;

            ResolveInventory();
            if (animalInventory != null && !animalInventory.TryGetAnimalSlot(animalId, out _))
                return false;

            editDraftIds[slotIndex] = animalId;
            pendingSlotIndex = -1;

            // 배치 완료 시 서브패널 슬라이드 닫기
            HideSetAnimalListPanel();
            RefreshUI();

            return true;
        }

        public bool TryRemoveAt(int slotIndex)
        {
            if (!isEditMode) return false;

            EnsureEditDraft();
            if (!CanUseSlot(slotIndex)) return false;

            editDraftIds[slotIndex] = string.Empty;
            if (pendingSlotIndex == slotIndex) pendingSlotIndex = -1;

            RefreshUI();
            return true;
        }

        [ContextMenu("Refresh UI")]
        public void RefreshUI()
        {
            if (uiController == null) return;

            EnsurePlacementBuffer();
            if (isEditMode) EnsureEditDraft();

            SanitizeActivePlacement();

            int unlocked = GetUnlockedSlotCount();
            IReadOnlyList<string> activeIds = GetActivePlacementIds();
            uiController.BindSlotCallbacks(OnClickSetAnimal, OnClickRemoveAnimal);
            uiController.RefreshSlots(activeIds, unlocked, isEditMode);
            uiController.SetConfirmButtonActive(isEditMode);
            uiController.RefreshCountTexts(CountPlacedIds(villageAnimalIds), unlocked);
        }

        // =========================================================================
        // [ 서브 패널 슬라이딩 연출 로직 ]
        // =========================================================================

        /// <summary>
        /// 애니메이션 호출 매개자
        /// </summary>
        private void AnimateSubPanel(bool show)
        {
            ResolveSubPanelRect();
            if (subPanelRect == null) return;

            // 연속 클릭으로 코루틴 중첩 방지
            if (slideCoroutine != null)
                StopCoroutine(slideCoroutine);

            // 오브젝트가 비활성화 상태면 코루틴 실행이 불가능하므로 예외 처리
            if (gameObject.activeInHierarchy)
            {
                slideCoroutine = StartCoroutine(Co_SlideSubPanel(show));
            }
            else
            {
                subPanelRect.anchoredPosition = show ? visibleAnchoredPos : hiddenAnchoredPos;
                subPanelRect.gameObject.SetActive(show);
            }
        }

        /// <summary>
        /// SmoothStep 함수 기반 슬라이드 애니메이션 코루틴
        /// </summary>
        private IEnumerator Co_SlideSubPanel(bool show)
        {
            if (show)
            {
                subPanelRect.gameObject.SetActive(true);
                // 메인 UI 패널 뒤로 들어가서 그려지도록 Sibling 순서를 맨 앞으로 보냄
                subPanelRect.SetAsFirstSibling();
            }

            Vector2 startPos = subPanelRect.anchoredPosition;
            Vector2 targetPos = show ? visibleAnchoredPos : hiddenAnchoredPos;
            float elapsedTime = 0f;

            // slideDuration이 0 이하일 경우 몫 나누기 예외 처리
            if (slideDuration <= 0f)
            {
                subPanelRect.anchoredPosition = targetPos;
            }
            else
            {
                while (elapsedTime < slideDuration)
                {
                    elapsedTime += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsedTime / slideDuration);

                    // 3차 SmoothStep 보간 ($S(t) = 3t^2 - 2t^3$): 부드러운 출발 및 감속
                    float smoothT = Mathf.SmoothStep(0f, 1f, t);

                    subPanelRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, smoothT);
                    yield return null; // Garbage Collection 없는 Frame 대기
                }
            }

            subPanelRect.anchoredPosition = targetPos;

            // 닫히는 연출 완료 후 오브젝트 비활성화
            if (!show)
            {
                subPanelRect.gameObject.SetActive(false);
                animalSetList?.Close();
            }

            slideCoroutine = null;
        }

        private void ResolveSubPanelRect()
        {
            if (subPanelRect != null) return;

            if (animalSetList != null)
                subPanelRect = animalSetList.GetComponent<RectTransform>();
            else if (setAnimalListPanel != null)
                subPanelRect = setAnimalListPanel.GetComponent<RectTransform>();
        }

        /// <summary>
        /// 슬라이드 애니메이션 연출과 함께 닫기
        /// </summary>
        private void HideSetAnimalListPanel()
        {
            AnimateSubPanel(false);
        }

        /// <summary>
        /// 애니메이션 없이 즉시 닫기 (Awake 전용)
        /// </summary>
        private void HideSetAnimalListPanelImmediate()
        {
            ResolveSubPanelRect();
            if (subPanelRect != null)
            {
                subPanelRect.anchoredPosition = hiddenAnchoredPos;
                subPanelRect.gameObject.SetActive(false);
            }
            animalSetList?.Close();
        }
        // =========================================================================

        private static int CountPlacedIds(IReadOnlyList<string> ids)
        {
            if (ids == null) return 0;
            int count = 0;
            for (int i = 0; i < ids.Count; i++)
            {
                if (!string.IsNullOrEmpty(ids[i])) count++;
            }
            return count;
        }

        private IReadOnlyList<string> GetActivePlacementIds()
        {
            if (isEditMode && editDraftIds != null) return editDraftIds;
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

        private void EnsureEditDraft()
        {
            if (editDraftIds == null)
            {
                BeginEditDraft();
                return;
            }
            while (editDraftIds.Count < MaxCapacity) editDraftIds.Add(string.Empty);
            if (editDraftIds.Count > MaxCapacity) editDraftIds.RemoveRange(MaxCapacity, editDraftIds.Count - MaxCapacity);
        }

        private bool CommitEditDraft()
        {
            if (editDraftIds == null) return false;
            EnsurePlacementBuffer();
            SanitizeList(editDraftIds);

            int capacity = MaxCapacity;
            for (int i = 0; i < capacity; i++)
            {
                string id = i < editDraftIds.Count ? editDraftIds[i] : string.Empty;
                villageAnimalIds[i] = id ?? string.Empty;
            }
            return true;
        }

        private void SanitizeActivePlacement()
        {
            if (isEditMode && editDraftIds != null) SanitizeList(editDraftIds);
            else SanitizeList(villageAnimalIds);
        }

        private void SanitizeList(List<string> ids)
        {
            if (ids == null) return;
            int unlocked = GetUnlockedSlotCount();
            var seen = new HashSet<string>();

            for (int i = 0; i < ids.Count; i++)
            {
                if (ids[i] == null) ids[i] = string.Empty;
                if (i >= unlocked) continue;

                string id = ids[i];
                if (string.IsNullOrEmpty(id)) continue;

                if (!seen.Add(id)) ids[i] = string.Empty;
            }
        }

        private void ApplyEditModeToSlots()
        {
            if (uiController == null) return;
            uiController.SetEditMode(isEditMode);
            uiController.SetConfirmButtonActive(isEditMode);
            if (!isEditMode) pendingSlotIndex = -1;
        }

        private void OnVillageUpgradeStateChanged() => RefreshUI();

        private void EnsurePlacementBuffer()
        {
            if (villageAnimalIds == null) villageAnimalIds = new List<string>();
            int capacity = MaxCapacity;
            while (villageAnimalIds.Count < capacity) villageAnimalIds.Add(string.Empty);
            if (villageAnimalIds.Count > capacity) villageAnimalIds.RemoveRange(capacity, villageAnimalIds.Count - capacity);
        }

        private void RestoreConfirmedPlacementFromVillageSystem()
        {
            VillageSystemManager villageSystem = VillageSystemManager.Instance;
            if (villageSystem == null) return;

            EnsurePlacementBuffer();
            IReadOnlyList<string> restoredIds = villageSystem.PlacedAnimalIds;
            for (int i = 0; i < MaxCapacity; i++)
            {
                string animalId = i < restoredIds.Count ? restoredIds[i] ?? string.Empty : string.Empty;
                if (!string.IsNullOrEmpty(animalId) && animalInventory != null && !animalInventory.TryGetAnimalSlot(animalId, out _))
                {
                    animalId = string.Empty;
                }
                villageAnimalIds[i] = animalId;
            }

            SanitizeList(villageAnimalIds);
            if (!PlacementListsEqual(villageSystem.PlacedAnimalIds, villageAnimalIds))
                villageSystem.SetPlacedAnimalIds(villageAnimalIds);
        }

        private void SynchronizeConfirmedPlacementToVillageSystem()
        {
            VillageSystemManager villageSystem = VillageSystemManager.Instance;
            if (villageSystem != null)
                villageSystem.SetPlacedAnimalIds(villageAnimalIds);
        }

        private static bool PlacementListsEqual(IReadOnlyList<string> left, IReadOnlyList<string> right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left == null || right == null || left.Count != right.Count) return false;
            for (int i = 0; i < left.Count; i++)
            {
                if (!string.Equals(left[i] ?? string.Empty, right[i] ?? string.Empty, StringComparison.Ordinal)) return false;
            }
            return true;
        }

        private void ResolveEditButton()
        {
            if (editAnimalSetButton == null && uiController != null)
                editAnimalSetButton = uiController.EditSetAnimalButton;
        }

        private void ResolveConfirmButton()
        {
            if (confirmAnimalSetButton == null && uiController != null)
                confirmAnimalSetButton = uiController.ConfirmSetAnimalButton;
        }

        private void BindEditButton()
        {
            ResolveEditButton();
            if (editAnimalSetButton == null) return;
            editAnimalSetButton.onClick.RemoveListener(ToggleEditMode);
            editAnimalSetButton.onClick.AddListener(ToggleEditMode);
        }

        private void UnbindEditButton()
        {
            if (editAnimalSetButton != null)
                editAnimalSetButton.onClick.RemoveListener(ToggleEditMode);
        }

        private void BindConfirmButton()
        {
            ResolveConfirmButton();
            if (confirmAnimalSetButton == null) return;
            confirmAnimalSetButton.onClick.RemoveListener(OnClickConfirmAnimalSet);
            confirmAnimalSetButton.onClick.AddListener(OnClickConfirmAnimalSet);
        }

        private void UnbindConfirmButton()
        {
            if (confirmAnimalSetButton != null)
                confirmAnimalSetButton.onClick.RemoveListener(OnClickConfirmAnimalSet);
        }

        private void SetConfirmButtonActive(bool active)
        {
            ResolveConfirmButton();
            if (confirmAnimalSetButton == null) return;
            if (confirmAnimalSetButton.gameObject.activeSelf != active)
                confirmAnimalSetButton.gameObject.SetActive(active);
            confirmAnimalSetButton.interactable = active;
        }

        private void SubscribeEvents()
        {
            if (isSubscribed) return;
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
            if (!isSubscribed) return;
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
            if (villageUpgradeUIManager != null) return;
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
    }
}