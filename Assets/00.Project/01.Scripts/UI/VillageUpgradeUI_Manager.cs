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

        [Header("마을 시스템 (비어 있으면 Instance 사용)")]
        [SerializeField] private VillageSystemManager villageSystem;

        private bool isCoinSubscribed;
        private bool isPanelOpenSubscribed;
        private bool isVillageStateSubscribed;
        private bool isRefreshing;
        private Coroutine subscribeRoutine;

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
            ///Before
            //if (ok)
            //    RefreshAllUI();
            //return ok;
            ///After
            if (!ok)
                return false;

            RefreshAllUI();

            // 방금 상한에 도달했고, 아직 엔드리스가 아니면 선택지 제공
            if (system.IsVillageLevelMaxed && !system.IsEndlessMode)
                OpenCompletionChoicePopup();
            
            return true;
            //------------------------------------------------------------
        }

        /// <summary>현재 마을 레벨 기준 레벨업 필요 코인.</summary>
        public long GetVillageLevelUpCost()
        {
            VillageSystemManager system = ResolveVillageSystem();
            return system != null ? system.GetVillageLevelUpCost() : 0;
        }

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
                bool showClickComplete = !endless && system != null && system.CycleClickDone;
                bool showTypingComplete = !endless && system != null && system.CycleTypingDone;
                bool showToolComplete = !endless && system != null && system.CycleToolDone;
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
                // 버튼 오브젝트는 항상 표시. 클릭 가능 여부는 RefreshInteractableStates에서 처리.
                uiController.SetVillageLevelUpVisible(true);

                // checkImg[3] 보유 코인 — CompleteImg와 같은 Refresh 시점에 갱신
                long coin = CoinManager.Instance != null ? CoinManager.Instance.totalCoin : 0;
                uiController.SetCoinCheckImg(coin >= villageCost);

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
            bool villageEnabled = system != null
                && !system.IsVillageLevelMaxed
                && system.IsReadyForVillageLevelUp
                && coin >= villageCost;
            uiController.SetVillageLevelUpInteractable(villageEnabled);

            // 코인 변동 시 checkImg[3]도 즉시 반영
            uiController.SetCoinCheckImg(coin >= villageCost);
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
            RefreshAllUI();
        }

        //--------------------------------------26.07.30 KNW---------------------------------------------------------
        public void EnableEndlessMode()
        {
            VillageSystemManager system = ResolveVillageSystem();
            if (system == null)
                return;

            system.EnableEndlessMode();
            RefreshAllUI();
        }

        public void DebugSetEndlessMode(bool value)
        {
            VillageSystemManager system = ResolveVillageSystem();
            if (system == null)
                return;

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

            completionPopup.Open(
                resetAction: () => TaskTown.GameResetService.ResetProgressAndGoToTeamLogo(),
                endlessAction: () =>
                {
                    EnableEndlessMode();
                    if (TaskTown.KDH.SaveManager.Instance != null)
                        TaskTown.KDH.SaveManager.Instance.SaveGame();
                });
        }
    }
}
