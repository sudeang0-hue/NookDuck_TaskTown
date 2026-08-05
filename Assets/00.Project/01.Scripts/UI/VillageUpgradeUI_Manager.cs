using System;
using System.Collections;
using Manager;
using TaskTown.Gacha;
using TaskTown.KDH;
using UnityEngine;

/// <summary>
/// VillageUpgrade_root 연동 UI.
/// 마을 레벨·사이클 완료·엔드리스 상태는 VillageSystemManager가 소유하고,
/// 이 클래스는 버튼/팝업/표시와 TownUpgradeManager 호출만 담당합니다.
///
/// SaveManager / TownUpgradeManager / Inventory 등이 Inspector로 이 컴포넌트를
/// ITownLevelProvider로 참조하는 기존 연결을 유지하기 위해 인터페이스를 파사드로 구현합니다.
/// </summary>
namespace UI
{
    public class VillageUpgradeUI_Manager : MonoBehaviour, ITownLevelProvider, IEndlessModeProvider
    {
        /// <summary>
        /// 트랙 완료/마을 레벨/비용 UI 상태가 바뀔 때 발행합니다.
        /// VillageInfo 등 외부 패널 동기화용입니다.
        /// </summary>
        public event Action OnVillageUpgradeStateChanged;

        [Header("참조")]
        [SerializeField] private UIController_VillageUpgrade uiController;
        [Tooltip("VillageUpgrade_root의 UIPanelWindow. 비어 있으면 자식에서 찾습니다.")]
        [SerializeField] private UIPanelWindow upgradePanel;
        [SerializeField] private LevelUpPopup levelUpPopup;
        
        [SerializeField, TextArea(2, 4)]
        private string complteVillageEndMessage = "마을 재건을 완료하시겠어요?";


        [Header("마을 시스템 (비어 있으면 Instance 사용)")]
        [SerializeField] private VillageSystemManager villageSystem;

        private bool isCoinSubscribed;
        private bool isPanelOpenSubscribed;
        private bool isVillageStateSubscribed;
        private bool isRefreshing;
        private Coroutine subscribeRoutine;

        /// <summary>
        /// 최대 레벨 직전→최대 도달 직후 CompleteImg를 유지합니다.
        /// VillageCompletionPopup에서 엔드리스 선택 시에만 해제합니다.
        /// </summary>
        private bool holdTrackCompleteOverlays;

        private VillageSystemManager System => ResolveVillageSystem();

        /// <summary>필수 업그레이드 3종이 모두 완료되었는지.</summary>
        public bool IsReadyForVillageLevelUp =>
            System != null && System.IsReadyForVillageLevelUp;

        /// <summary>UI/세이브 호환용 마을 레벨 조회.</summary>
        public int UiTownLevel => System != null ? System.TownLevel : 1;

        /// <summary>ITownLevelProvider 파사드 → VillageSystemManager.</summary>
        public int CurrentTownLevel => UiTownLevel;

        /// <summary>IEndlessModeProvider 파사드 → VillageSystemManager.</summary>
        public bool IsEndlessMode => System != null && System.IsEndlessMode;

        /// <summary>마을 레벨 상한(10) 도달 여부.</summary>
        public bool IsVillageLevelMaxed =>
            System != null && System.IsVillageLevelMaxed;

        //-----------------------26.08.03.KDH------------------------------
        // 필드 추가
        [Header("엔딩 선택")]
        [SerializeField] private VillageCompletionPopup completionPopup;
        //-----------------------------------------------------------------

        //------------------26.08.05 KAY 추가 (최대 레벨 엔드 버튼)---------------------------------
        [Header("엔딩 선택 비용 (추후 VillageSystemManager 이관 예정)")]
        [Tooltip("최대 레벨 도달 후 마을 재건/엔드 선택 팝업을 열 때 소모하는 코인")]
        [SerializeField] private long villageCompletionCost;
        //-----------------------------------------------------------------------------

        private void Awake()
        {
            if (uiController == null)
                uiController = GetComponent<UIController_VillageUpgrade>();

            if (upgradePanel == null)
                upgradePanel = GetComponentInChildren<UIPanelWindow>(true);

            ResolveVillageSystem();
            BindButtons();
        }

        private void OnDestroy()
        {
            UnbindButtons();
            UnsubscribePanelOpened();
            UnsubscribeCoinEvent();
            UnsubscribeVillageState();
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
                RefreshAllUI();
        }

        private void OnEnable()
        {
            TrySubscribePanelOpened();
            TrySubscribeCoinEvent();
            TrySubscribeVillageState();
            RefreshAllUI();

            if (subscribeRoutine == null)
                subscribeRoutine = StartCoroutine(SubscribeWhenDependenciesReady());
        }

        private void OnDisable()
        {
            if (subscribeRoutine != null)
            {
                StopCoroutine(subscribeRoutine);
                subscribeRoutine = null;
            }

            UnsubscribePanelOpened();
            UnsubscribeCoinEvent();
            UnsubscribeVillageState();
        }

        private VillageSystemManager ResolveVillageSystem()
        {
            if (villageSystem != null)
                return villageSystem;

            if (VillageSystemManager.Instance != null)
            {
                villageSystem = VillageSystemManager.Instance;
                return villageSystem;
            }

            villageSystem = FindFirstObjectByType<VillageSystemManager>();
            return villageSystem;
        }

        /// <summary>
        /// 세이브 townLevel 복원용. 사이클 완료 플래그는 건드리지 않습니다.
        /// VillageSystemManager가 없을 때 SaveManager 폴백 경로에서만 사용합니다.
        /// 정상 로드는 SaveManager → VillageSystemManager.ApplyProgressFromSave → RefreshUIAfterSaveRestore.
        /// </summary>
        public void SetTownLevelFromSave(int level)
        {
            VillageSystemManager system = ResolveVillageSystem();
            if (system == null)
            {
                Debug.LogWarning("[VillageUpgradeUI_Manager] VillageSystemManager가 없어 세이브 마을 레벨을 적용할 수 없습니다.");
                return;
            }

            // 2026.08.02 - KAY - 정상 세이브 복원은 SaveManager가 System에 Snapshot 적용.
            // 이 메서드는 System 부재 시 폴백/하위 호환용으로 레벨만 반영합니다.
            system.SetTownLevelFromSave(level);
            RefreshAllUI();
        }

        // 2026.08.02 - KAY - 저장 시점(로드 직후) UI 반영. 데이터는 이미 System에 있음.
        /// <summary>
        /// SaveManager가 VillageSystemManager에 진행 상태를 복원한 직후 호출합니다.
        /// 로드 타이밍상 OnVillageStateChanged 구독 전일 수 있어 UI를 강제 Refresh합니다.
        /// 런타임 중 변경은 OnVillageStateChanged → RefreshAllUI 경로를 사용합니다.
        /// </summary>
        public void RefreshUIAfterSaveRestore()
        {
            ResolveVillageSystem();
            TrySubscribeVillageState();
            RefreshAllUI();

            //-------------------------26.08.04 KDH-----------------------------------
            VillageSystemManager system = ResolveVillageSystem();
            if (system != null && system.IsVillageLevelMaxed && !system.IsEndlessMode)
            {
                EnableEndlessMode(); // 내부에서 RefreshAllUI 한 번 더 함
                SaveManager.Instance?.SaveGame();
            }
            //-----------------------------------------------------------------------
        }

        private void BindButtons()
        {
            if (uiController == null)
                return;

            if (uiController.ClickCoinUpButton != null)
                uiController.ClickCoinUpButton.onClick.AddListener(OnClickUpgradeClick);

            if (uiController.TypingCoinUpButton != null)
                uiController.TypingCoinUpButton.onClick.AddListener(OnClickUpgradeTyping);

            if (uiController.ToolProductUpButton != null)
                uiController.ToolProductUpButton.onClick.AddListener(OnClickUpgradeTool);

            if (uiController.VillageLevelUpButton != null)
                uiController.VillageLevelUpButton.onClick.AddListener(OnClickVillageLevelUp);

            //------------------26.08.05 KAY 추가 (최대 레벨 엔드 버튼)---------------------------------
            if (uiController.ComplteVillageEndButton != null)
                uiController.ComplteVillageEndButton.onClick.AddListener(OnClickCompleteVillageEnd);
            //-----------------------------------------------------------------------------

            if (uiController.WindowClose != null)
                uiController.WindowClose.onClick.AddListener(OnClickWindowClose);
        }

        private void UnbindButtons()
        {
            if (uiController == null)
                return;

            if (uiController.ClickCoinUpButton != null)
                uiController.ClickCoinUpButton.onClick.RemoveListener(OnClickUpgradeClick);

            if (uiController.TypingCoinUpButton != null)
                uiController.TypingCoinUpButton.onClick.RemoveListener(OnClickUpgradeTyping);

            if (uiController.ToolProductUpButton != null)
                uiController.ToolProductUpButton.onClick.RemoveListener(OnClickUpgradeTool);

            if (uiController.VillageLevelUpButton != null)
                uiController.VillageLevelUpButton.onClick.RemoveListener(OnClickVillageLevelUp);

            //------------------26.08.05 KAY 추가 (최대 레벨 엔드 버튼)---------------------------------
            if (uiController.ComplteVillageEndButton != null)
                uiController.ComplteVillageEndButton.onClick.RemoveListener(OnClickCompleteVillageEnd);
            //-----------------------------------------------------------------------------

            if (uiController.WindowClose != null)
                uiController.WindowClose.onClick.RemoveListener(OnClickWindowClose);
        }

        /// <summary>열려 있는 VillageUpgrade 창을 닫습니다.</summary>
        private void OnClickWindowClose()
        {
            if (upgradePanel == null)
                upgradePanel = GetComponentInChildren<UIPanelWindow>(true);

            if (upgradePanel != null)
                upgradePanel.ClosePanel();
        }

        private void OnClickUpgradeClick()
        {
            VillageSystemManager system = ResolveVillageSystem();
            TownUpgradeManager manager = TownUpgradeManager.Instance;

            if (system == null
                || !system.CanPurchaseTrackInCycle(VillageElementTrack.Click)
                || manager == null
                || manager.IsClickUpgradeMaxLevel)
                return;

            long cost = manager.ClickUpgradeNextCost;

            OpenUpgradePopup(
                "클릭 코인 획득량을 업그레이드하시겠습니까?",
                cost,
                () => TryCompleteTrack(
                    VillageElementTrack.Click,
                    () => manager.TryUpgradeClick()));
        }

        private void OnClickUpgradeTyping()
        {
            VillageSystemManager system = ResolveVillageSystem();
            TownUpgradeManager manager = TownUpgradeManager.Instance;

            if (system == null
                || !system.CanPurchaseTrackInCycle(VillageElementTrack.Typing)
                || manager == null
                || manager.IsTypingUpgradeMaxLevel)
                return;

            long cost = manager.TypingUpgradeNextCost;

            OpenUpgradePopup(
                "타이핑 코인 획득량을 업그레이드하시겠습니까?",
                cost,
                () => TryCompleteTrack(
                    VillageElementTrack.Typing,
                    () => manager.TryUpgradeTyping()));
        }

        private void OnClickUpgradeTool()
        {
            VillageSystemManager system = ResolveVillageSystem();
            TownUpgradeManager manager = TownUpgradeManager.Instance;

            if (system == null
                || !system.CanPurchaseTrackInCycle(VillageElementTrack.ToolEfficiency)
                || manager == null
                || manager.IsToolEfficiencyUpgradeMaxLevel)
                return;

            long cost = manager.ToolEfficiencyUpgradeNextCost;

            OpenUpgradePopup(
                "도구 생산 효율을 업그레이드하시겠습니까?",
                cost,
                () => TryCompleteTrack(
                    VillageElementTrack.ToolEfficiency,
                    () => manager.TryUpgradeToolEfficiency()));
        }

        private void OnClickVillageLevelUp()
        {
            VillageSystemManager system = ResolveVillageSystem();
            if (system == null || system.IsVillageLevelMaxed || !system.IsReadyForVillageLevelUp)
                return;

            long cost = system.GetVillageLevelUpCost();

            OpenUpgradePopup(
                $"마을을 Lv.{system.TownLevel + 1}로 업그레이드하시겠습니까?",
                cost,
                () => TryVillageLevelUp());
        }

        //------------------26.08.05 KAY 추가 (최대 레벨 엔드 버튼)---------------------------------
        /// <summary>
        /// 최대 레벨 전용. 마을 레벨업은 하지 않고, 설정 코인을 소모한 뒤 완주 선택 팝업을 엽니다.
        /// </summary>
        private void OnClickCompleteVillageEnd()
        {
            VillageSystemManager system = ResolveVillageSystem();
            if (system == null || !system.IsVillageLevelMaxed || system.IsEndlessMode)
                return;

            long cost = GetVillageCompletionCost();
            OpenUpgradePopup(complteVillageEndMessage,
                cost,
                () => TryOpenCompletionPopupWithCost());
        }

        /// <summary>
        /// 코인 소모 후 VillageCompletionPopup을 엽니다. 레벨업은 수행하지 않습니다.
        /// </summary>
        private bool TryOpenCompletionPopupWithCost()
        {
            VillageSystemManager system = ResolveVillageSystem();
            if (system == null || !system.IsVillageLevelMaxed || system.IsEndlessMode)
                return false;

            long cost = GetVillageCompletionCost();
            if (cost > 0)
            {
                if (CoinManager.Instance == null || !CoinManager.Instance.TrySpend(cost))
                    return false;
            }

            OpenCompletionChoicePopup();
            RefreshInteractableStates();
            return true;
        }
        //-----------------------------------------------------------------------------

        /// <summary>
        /// TownUpgradeManager 구매 성공 후 사이클 완료 플래그를 VillageSystemManager에 기록합니다.
        /// </summary>
        private void TryCompleteTrack(VillageElementTrack track, Func<bool> tryUpgrade)
        {
            VillageSystemManager system = ResolveVillageSystem();
            if (system == null)
                return;

            if (!system.CanPurchaseTrackInCycle(track))
                return;

            if (tryUpgrade == null || !tryUpgrade())
                return;

            system.TryMarkCycleTrackDone(track);
            // OnVillageStateChanged → RefreshAllUI (구독). 미구독 대비 한 번 더 갱신.
            RefreshAllUI();
        }

        private void OpenUpgradePopup(string message, long cost, Action confirmAction)
        {
            if (levelUpPopup == null)
            {
                Debug.LogWarning("[VillageUpgradeUI_Manager] LevelUpPopup이 연결되지 않았습니다.");
                return;
            }

            if (confirmAction == null)
                return;

            levelUpPopup.OpenLevelUpPopup(message, $"소모 코인 : <color=#4F2002><b>{cost:N0}</b></color>", confirmAction);
        }

        /// <summary>
        /// 필수 3종 완료 + 비용 지불 가능 시 마을 레벨업을 수행합니다.
        /// </summary>
        public bool TryVillageLevelUp()
        {
            VillageSystemManager system = ResolveVillageSystem();
            if (system == null)
                return false;

            bool ok = system.TryVillageLevelUp();
            //-----------------------26.08.03 KDH---------------------------
            if (!ok)
                return false;

            // 최대 레벨 도달(엔드리스 전): 사이클 플래그가 리셋되어도 CompleteImg 유지
            if (system.IsVillageLevelMaxed && !system.IsEndlessMode)
                holdTrackCompleteOverlays = true;

            // 26.08.05 KAY: 최대 도달 시 자동 VillageCompletionPopup 호출 제거.
            // 엔드 버튼(complteVillageEndButton) 클릭 → 코인 소모 후 팝업으로 변경.
            RefreshAllUI();
            return true;
            //------------------------------------------------------------
        }

        /// <summary>현재 마을 레벨 기준 레벨업 필요 코인.</summary>
        public long GetVillageLevelUpCost()
        {
            VillageSystemManager system = ResolveVillageSystem();
            return system != null ? system.GetVillageLevelUpCost() : 0;
        }

        //------------------26.08.05 KAY 추가 (최대 레벨 엔드 버튼)---------------------------------
        /// <summary>
        /// 완주 선택 팝업 오픈 비용. System 프로퍼티와 System.Math 이름 충돌을 피하기 위해 분리합니다.
        /// </summary>
        private long GetVillageCompletionCost()
        {
            return villageCompletionCost < 0L ? 0L : villageCompletionCost;
        }
        //-----------------------------------------------------------------------------

        /// <summary>사이클 플래그 리셋 후 UI 갱신.</summary>
        public void ResetCycleAndRefreshUI()
        {
            VillageSystemManager system = ResolveVillageSystem();
            system?.ResetCycleFlags();
            RefreshAllUI();
        }

        public void RefreshAllUI()
        {
            if (isRefreshing || uiController == null)
                return;

            isRefreshing = true;
            try
            {
                VillageSystemManager system = ResolveVillageSystem();

                // 요소 영구 업그레이드 상태 (TownUpgradeManager / EarnProcessor — 세이브 복원값 포함)
                TownUpgradeManager tum = TownUpgradeManager.Instance;
                if (tum != null)
                {
                    uiController.RefreshTrackLevels(tum.ClickLevel, tum.TypingLevel, tum.ToolEfficiencyLevel);
                    uiController.RefreshTrackCosts(
                        tum.ClickUpgradeNextCost,
                        tum.TypingUpgradeNextCost,
                        tum.ToolEfficiencyUpgradeNextCost,
                        tum.IsClickUpgradeMaxLevel,
                        tum.IsTypingUpgradeMaxLevel,
                        tum.IsToolEfficiencyUpgradeMaxLevel);
                }

                int clickValue = 0;
                int typingValue = 0;
                if (EarnProcessor.Instance != null)
                {
                    clickValue = EarnProcessor.Instance.BaseCoinPerClick * EarnProcessor.Instance.ClickMultiplier;
                    typingValue = EarnProcessor.Instance.BaseCoinPerTyping * EarnProcessor.Instance.TypingMultiplier;
                }

                float toolProduct = tum != null ? tum.ToolEfficiencyMultiplier : 1f;
                uiController.RefreshTrackValues(clickValue, typingValue, toolProduct);

                bool endless = system != null && system.IsEndlessMode;
                // 최대 도달 직후~엔드리스 선택 전: CompleteImg 강제 유지
                bool showClickComplete = holdTrackCompleteOverlays
                    || (!endless && system != null && system.CycleClickDone);
                bool showTypingComplete = holdTrackCompleteOverlays
                    || (!endless && system != null && system.CycleTypingDone);
                bool showToolComplete = holdTrackCompleteOverlays
                    || (!endless && system != null && system.CycleToolDone);
                uiController.SetTrackComplete(showClickComplete, showTypingComplete, showToolComplete);

                int completedCount = system != null ? system.CycleCompletedCount : 0;
                int townLevel = system != null ? system.TownLevel : 1;
                bool levelMaxed = system != null && system.IsVillageLevelMaxed;

                uiController.RefreshRequiredUpgrade(completedCount);
                uiController.RefreshVillageLevel(townLevel);
                // 다음 레벨업 보상 미리보기 (최대 레벨이면 RewardTexts 숨김, RewardEnd_txt 표시)
                int nextTownLevel = townLevel + 1;
                string decoBuildingName = system != null
                    ? system.GetVillageDecoBuildingName(nextTownLevel)
                    : null;
                uiController.RefreshReward(townLevel, !levelMaxed, decoBuildingName);

                long villageCost = GetVillageLevelUpCost();
                uiController.RefreshVillageLevelUpCost(villageCost);

                //------------------26.08.05 KAY 추가 (최대 레벨 엔드 버튼)---------------------------------
                // 최대 레벨 + 비엔드리스: 레벨업 버튼 숨기고 엔드 버튼 표시. 그 외는 레벨업 버튼.
                bool showEndButton = levelMaxed && system != null && !system.IsEndlessMode;
                uiController.SetMaxLevelEndButtons(showEndButton);
                if (!showEndButton)
                    uiController.SetVillageLevelUpVisible(true);

                long coin = CoinManager.Instance != null ? CoinManager.Instance.totalCoin : 0;
                long costForCoinCheck = showEndButton
                    ? GetVillageCompletionCost()
                    : villageCost;
                uiController.SetCoinCheckImg(coin >= costForCoinCheck);
                //-----------------------------------------------------------------------------

                RefreshInteractableStates();
                OnVillageUpgradeStateChanged?.Invoke();
            }
            finally
            {
                isRefreshing = false;
            }
        }

        private void OnUpgradePanelOpened()
        {
            // Canvas는 상시 활성, root만 토글되므로 오픈 시 세이브 반영 상태(요소/마을 레벨)를 다시 그립니다.
            RefreshAllUI();
        }

        // 2026.08.02 - KAY - 런타임 변경 시점: System 상태 변경 → UI Refresh
        private void OnVillageSystemStateChanged()
        {
            RefreshAllUI();
        }

        private void TrySubscribePanelOpened()
        {
            if (isPanelOpenSubscribed)
                return;

            if (upgradePanel == null)
                upgradePanel = GetComponentInChildren<UIPanelWindow>(true);

            if (upgradePanel == null)
                return;

            upgradePanel.OnPanelOpened -= OnUpgradePanelOpened;
            upgradePanel.OnPanelOpened += OnUpgradePanelOpened;
            isPanelOpenSubscribed = true;
        }

        private void UnsubscribePanelOpened()
        {
            if (!isPanelOpenSubscribed)
                return;

            if (upgradePanel != null)
                upgradePanel.OnPanelOpened -= OnUpgradePanelOpened;

            isPanelOpenSubscribed = false;
        }

        private void TrySubscribeVillageState()
        {
            if (isVillageStateSubscribed)
                return;

            VillageSystemManager system = ResolveVillageSystem();
            if (system == null)
                return;

            system.OnVillageStateChanged -= OnVillageSystemStateChanged;
            system.OnVillageStateChanged += OnVillageSystemStateChanged;
            isVillageStateSubscribed = true;
        }

        private void UnsubscribeVillageState()
        {
            if (!isVillageStateSubscribed)
                return;

            if (villageSystem != null)
                villageSystem.OnVillageStateChanged -= OnVillageSystemStateChanged;
            else if (VillageSystemManager.Instance != null)
                VillageSystemManager.Instance.OnVillageStateChanged -= OnVillageSystemStateChanged;

            isVillageStateSubscribed = false;
        }

        private void RefreshInteractableStates()
        {
            if (uiController == null)
                return;

            VillageSystemManager system = ResolveVillageSystem();
            long coin = CoinManager.Instance != null ? CoinManager.Instance.totalCoin : 0;
            TownUpgradeManager tum = TownUpgradeManager.Instance;

            bool clickEnabled = system != null
                && system.CanPurchaseTrackInCycle(VillageElementTrack.Click)
                && tum != null
                && !tum.IsClickUpgradeMaxLevel
                && coin >= tum.ClickUpgradeNextCost;

            bool typingEnabled = system != null
                && system.CanPurchaseTrackInCycle(VillageElementTrack.Typing)
                && tum != null
                && !tum.IsTypingUpgradeMaxLevel
                && coin >= tum.TypingUpgradeNextCost;

            bool toolEnabled = system != null
                && system.CanPurchaseTrackInCycle(VillageElementTrack.ToolEfficiency)
                && tum != null
                && !tum.IsToolEfficiencyUpgradeMaxLevel
                && coin >= tum.ToolEfficiencyUpgradeNextCost;

            uiController.SetTrackButtonsInteractable(clickEnabled, typingEnabled, toolEnabled);

            long villageCost = GetVillageLevelUpCost();
            //------------------26.08.05 KAY 추가 (최대 레벨 엔드 버튼)---------------------------------
            bool showEndButton = system != null
                && system.IsVillageLevelMaxed
                && !system.IsEndlessMode;

            if (showEndButton)
            {
                long completionCost = GetVillageCompletionCost();
                uiController.SetVillageLevelUpInteractable(false);
                uiController.SetCompleteVillageEndInteractable(coin >= completionCost);
                uiController.SetCoinCheckImg(coin >= completionCost);
            }
            else
            {
                bool villageEnabled = system != null
                    && !system.IsVillageLevelMaxed
                    && system.IsReadyForVillageLevelUp
                    && coin >= villageCost;
                uiController.SetVillageLevelUpInteractable(villageEnabled);
                uiController.SetCompleteVillageEndInteractable(false);
                uiController.SetCoinCheckImg(coin >= villageCost);
            }
            //-----------------------------------------------------------------------------
        }

        private IEnumerator SubscribeWhenDependenciesReady()
        {
            const float timeoutSeconds = 3f;
            float elapsed = 0f;

            while (elapsed < timeoutSeconds
                   && (CoinManager.Instance == null
                       || EarnProcessor.Instance == null
                       || TownUpgradeManager.Instance == null
                       || ResolveVillageSystem() == null))
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            subscribeRoutine = null;
            TrySubscribeCoinEvent();
            TrySubscribeVillageState();
            RefreshAllUI();
        }

        private void TrySubscribeCoinEvent()
        {
            if (isCoinSubscribed)
                return;

            if (CoinManager.Instance == null)
                return;

            CoinManager.Instance.OnCoinChanged += OnCoinChanged;
            isCoinSubscribed = true;
        }

        private void UnsubscribeCoinEvent()
        {
            if (!isCoinSubscribed)
                return;

            if (CoinManager.Instance != null)
                CoinManager.Instance.OnCoinChanged -= OnCoinChanged;

            isCoinSubscribed = false;
        }

        private void OnCoinChanged(long _)
        {
            RefreshInteractableStates();
        }

        //-----------------------26.07.27 KDH-------------------------------------------------------------
        /// <summary>
        /// 디버그/테스트용. 마을 레벨을 지정값으로 두고 사이클·UI를 갱신합니다.
        /// </summary>
        public void DebugSetTownLevel(int level)
        {
            VillageSystemManager system = ResolveVillageSystem();
            if (system == null)
                return;

            system.DebugSetTownLevel(level);

            //------------------26.08.05 KAY 추가 (마을 레벨 설정 연동)---------------------------------
            // 정식 진행은 사이클마다 요소 1회 → 현재 마을 Lv면 요소 영구 레벨은 보통 Lv-1.
            // 요소 다음 비용·상한(IsMaxLevelAt)이 마을 레벨과 맞게 보이도록 동기화합니다.
            int elementLevel = Mathf.Max(0, system.TownLevel - 1);
            if (TownUpgradeManager.Instance != null)
                TownUpgradeManager.Instance.LoadLevels(elementLevel, elementLevel, elementLevel);

            // UIController_Gacha는 팀원 편집 중이므로 해당 파일은 수정하지 않음.
            // 기존 공개 API(NotifyPanelOpened)로 뽑기 가격 텍스트만 갱신합니다.
            UIController_Gacha gachaUI = FindAnyObjectByType<UIController_Gacha>(FindObjectsInactive.Include);
            gachaUI?.NotifyPanelOpened();
            //-----------------------------------------------------------------------------

            RefreshAllUI();
        }

        //--------------------------------------26.07.30 KNW---------------------------------------------------------
        public void EnableEndlessMode()
        {
            VillageSystemManager system = ResolveVillageSystem();
            if (system == null)
                return;

            // 엔드리스 진입 시에만 최대 도달 직후 유지하던 CompleteImg 해제
            holdTrackCompleteOverlays = false;
            system.EnableEndlessMode();
            RefreshAllUI();
        }

        public void DebugSetEndlessMode(bool value)
        {
            VillageSystemManager system = ResolveVillageSystem();
            if (system == null)
                return;

            if (value)
                holdTrackCompleteOverlays = false;

            system.DebugSetEndlessMode(value);
            RefreshAllUI();
        }

        //--------------------------------------26.07.29 KDH---------------------------------------------------------
        public bool DebugTryUpgradeClick()
        {
            VillageSystemManager system = ResolveVillageSystem();
            if (system == null || system.CycleClickDone)
                return false;

            if (TownUpgradeManager.Instance == null
                || !TownUpgradeManager.Instance.DebugForceUpgradeClick())
                return false;

            RefreshAllUI();
            return true;
        }

        public bool DebugTryUpgradeTyping()
        {
            VillageSystemManager system = ResolveVillageSystem();
            if (system == null || system.CycleTypingDone)
                return false;

            if (TownUpgradeManager.Instance == null
                || !TownUpgradeManager.Instance.DebugForceUpgradeTyping())
                return false;

            RefreshAllUI();
            return true;
        }

        public bool DebugTryUpgradeTool()
        {
            VillageSystemManager system = ResolveVillageSystem();
            if (system == null || system.CycleToolDone)
                return false;

            if (TownUpgradeManager.Instance == null
                || !TownUpgradeManager.Instance.DebugForceUpgradeToolEfficiency())
                return false;

            RefreshAllUI();
            return true;
        }

        //------------------26.08.05 KAY 추가 (마을 레벨업)---------------------------------
        /// <summary>
        /// 디버그용. 미완료 요소 3종을 강제 완료한 뒤, 정식 경로로 마을 레벨업(코인 소모)을 시도합니다.
        /// 상한 도달·코인 부족·매니저 없으면 false.
        /// </summary>
        public bool DebugForceVillageLevelUp()
        {
            VillageSystemManager system = ResolveVillageSystem();
            if (system == null || system.IsVillageLevelMaxed)
                return false;

            TownUpgradeManager manager = TownUpgradeManager.Instance;
            if (manager == null)
                return false;

            // 미완료 트랙만 영구 레벨업 + 사이클 완료 기록 (요소 비용은 디버그 스킵)
            if (!system.CycleClickDone)
            {
                if (!manager.DebugForceUpgradeClick())
                    return false;
                system.TryMarkCycleTrackDone(VillageElementTrack.Click);
            }

            if (!system.CycleTypingDone)
            {
                if (!manager.DebugForceUpgradeTyping())
                    return false;
                system.TryMarkCycleTrackDone(VillageElementTrack.Typing);
            }

            if (!system.CycleToolDone)
            {
                if (!manager.DebugForceUpgradeToolEfficiency())
                    return false;
                system.TryMarkCycleTrackDone(VillageElementTrack.ToolEfficiency);
            }

            // 마을 레벨업은 정식 코인 소모 경로
            return TryVillageLevelUp();
        }
        //-----------------------------------------------------------------------------

        //-------------------------------26.08.03 KDH-----------------------------------
        private void OpenCompletionChoicePopup()
        {
            if (completionPopup == null)
            {
                Debug.LogWarning("[VillageUpgradeUI_Manager] VillageCompletionPopup이 연결되지 않았습니다.");
                return;
            }

            // 완료 선택 팝업 전에 열려 있는 메인 메뉴(인벤/뽑기/도감/마을/옵션)를 닫습니다.
            // LevelUpPopup/None 타입은 CloseAllPanels 대상이 아니므로 Completion 팝업은 유지됩니다.
            UIController_Menu menu = FindFirstObjectByType<UIController_Menu>();
            menu?.CloseAllPanels();

            completionPopup.Open(
                resetAction: () => TaskTown.GameResetService.ResetProgressAndGoToTeamLogo(),
                endlessAction: () =>
                {
                    // onSelectEndless: CompleteImg 해제 + 엔드리스 진입
                    EnableEndlessMode();
                    if (TaskTown.KDH.SaveManager.Instance != null)
                        TaskTown.KDH.SaveManager.Instance.SaveGame();
                });
        }
    }
}
