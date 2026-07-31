using System;
using System.Collections;
using TaskTown.Gacha;
using UI;
using UnityEngine;

/// <summary>
/// VillageUpgrade_root 연동.
/// 3종 업그레이드는 TownUpgradeManager를 호출하고,
/// 마을 레벨/Required Upgrade 게이트는 UI 전용으로 관리합니다.
/// </summary>

namespace UI
{
    public class VillageUpgradeUI_Manager : MonoBehaviour, ITownLevelProvider, IEndlessModeProvider
    {
        private const int RequiredUpgradeTotal = 3;

        // #19(엔드리스 사전 작업): 일반 모드에서는 마을 레벨이 10에서 멈춥니다(완주). 완주 판정 후
        // "다음 난이도로" vs "엔드리스로 계속" 선택 UI는 별도 작업이라 아직 이 상한을 넘는 방법은
        // EnableEndlessMode()/DebugSetEndlessMode(true)뿐입니다.
        private const int NormalModeMaxTownLevel = 10;

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

        [Header("마을 레벨업 비용")]
        [SerializeField] private TownUpgradeCostConfig townUpgradeCostConfig = new TownUpgradeCostConfig();

        [Header("UI 전용 마을 레벨")]
        [SerializeField] private int uiTownLevel = 1;

        [Header("엔드리스 모드 (#19 사전 작업)")]
        [Tooltip("레벨10 완주 후 '엔드리스로 계속'을 선택하면 켜집니다. 켜지면 업그레이드/동물·도구 레벨 상한이 전부 해제됩니다.")]
        [SerializeField] private bool isEndlessMode = false;

        private bool clickDone;
        private bool typingDone;
        private bool toolDone;

        private bool isCoinSubscribed;
        private bool isPanelOpenSubscribed;
        private Coroutine subscribeRoutine;

        private int CompletedCount
        {
            get
            {
                int count = 0;
                if (clickDone) count++;
                if (typingDone) count++;
                if (toolDone) count++;
                return count;
            }
        }

        /// <summary>필수 업그레이드 3종이 모두 완료되었는지.</summary>
        public bool IsReadyForVillageLevelUp => CompletedCount >= RequiredUpgradeTotal;

        /// <summary>UI 전용 마을 레벨.</summary>
        public int UiTownLevel => uiTownLevel;

        /// <summary>
        /// ITownLevelProvider 구현 (#18: 도구 상한/뽑기 확률 등 다른 시스템이 마을 레벨을
        /// 참조할 때 이 컴포넌트를 그대로 연결할 수 있도록).
        /// </summary>
        public int CurrentTownLevel => uiTownLevel;

        /// <summary>
        /// IEndlessModeProvider 구현. 켜져 있으면 TownUpgradeManager/InventoryManager_Tool/Animal이
        /// 각자의 레벨 상한 체크를 건너뜁니다.
        /// </summary>
        public bool IsEndlessMode => isEndlessMode;

        private void Awake()
        {
            if (uiController == null)
                uiController = GetComponent<UIController_VillageUpgrade>();

            if (upgradePanel == null)
                upgradePanel = GetComponentInChildren<UIPanelWindow>(true);

            BindButtons();
        }

        private void OnDestroy()
        {
            UnbindButtons();
            UnsubscribePanelOpened();
            UnsubscribeCoinEvent();
        }

        // #19: 인스펙터에서 isEndlessMode 체크박스를 Play Mode 중 직접 토글하는 경우
        // (DebugSetEndlessMode()를 거치지 않으므로 RefreshAllUI가 호출되지 않음) UI가 갱신되지
        // 않는 문제를 방지합니다.
        private void OnValidate()
        {
            if (Application.isPlaying)
                RefreshAllUI();
        }

        private void OnEnable()
        {
            TrySubscribePanelOpened();
            TrySubscribeCoinEvent();
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
        }

        /// <summary>
        /// 세이브 townLevel 복원용. 사이클 완료 플래그는 건드리지 않습니다(사이클 저장은 추후).
        /// </summary>
        public void SetTownLevelFromSave(int level)
        {
            uiTownLevel = Mathf.Max(1, level);
            RefreshAllUI();
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
        }

        /// <summary>
        /// 클릭당 코인 업그레이드 버튼클릭
        /// </summary>
        private void OnClickUpgradeClick()
        {
            // 기존 코드
            //TryCompleteTrack(ref clickDone, () =>
            //    TownUpgradeManager.Instance != null && TownUpgradeManager.Instance.TryUpgradeClick());

            // 2026.07.30 KAY 수정 ---------------------------------------------------------------------
            TownUpgradeManager manager = TownUpgradeManager.Instance;

            // #19: 엔드리스 모드에서는 clickDone 완료 사이클 개념이 없으므로(TryCompleteTrack 참고)
            // 팝업을 여는 이 시점부터도 막지 않습니다.
            if ((!isEndlessMode && clickDone) || manager == null || manager.IsClickUpgradeMaxLevel)
                return;

            long cost = manager.ClickUpgradeNextCost;

            OpenUpgradePopup(
                "클릭 코인 획득량을 업그레이드하시겠습니까?",
                cost,
                () => TryCompleteTrack(
                    ref clickDone,
                    () => manager.TryUpgradeClick()));
            Debug.Log("클릭 코인 획득량을 업그레이드하시겠습니까?");
        }

        /// <summary>
        /// 타이핑당 코인 업그레이드 버튼클릭
        /// </summary>
        private void OnClickUpgradeTyping()
        {
            // 기존 코드
            //TryCompleteTrack(ref typingDone, () =>
            //    TownUpgradeManager.Instance != null && TownUpgradeManager.Instance.TryUpgradeTyping());

            // 2026.07.30 KAY 수정 ---------------------------------------------------------------------
            TownUpgradeManager manager = TownUpgradeManager.Instance;

            // #19: 엔드리스 모드에서는 typingDone 완료 사이클 개념이 없으므로 팝업을 여는
            // 이 시점부터도 막지 않습니다.
            if ((!isEndlessMode && typingDone) || manager == null || manager.IsTypingUpgradeMaxLevel)
                return;

            long cost = manager.TypingUpgradeNextCost;

            OpenUpgradePopup(
                "타이핑 코인 획득량을 업그레이드하시겠습니까?",
                cost,
                () => TryCompleteTrack(
                    ref typingDone,
                    () => manager.TryUpgradeTyping()));

            Debug.Log("타이핑 코인 획득량을 업그레이드하시겠습니까?");
        }

        /// <summary>
        /// 도구 생산 효율 업그레이드 버튼클릭
        /// </summary>
        private void OnClickUpgradeTool()
        {
            // 기존 코드
            //TryCompleteTrack(ref toolDone, () =>
            //    TownUpgradeManager.Instance != null && TownUpgradeManager.Instance.TryUpgradeToolEfficiency());

            // 2026.07.30 KAY 수정 ---------------------------------------------------------------------
            TownUpgradeManager manager = TownUpgradeManager.Instance;

            // #19: 엔드리스 모드에서는 toolDone 완료 사이클 개념이 없으므로 팝업을 여는
            // 이 시점부터도 막지 않습니다.
            if ((!isEndlessMode && toolDone) || manager == null || manager.IsToolEfficiencyUpgradeMaxLevel)
                return;

            long cost = manager.ToolEfficiencyUpgradeNextCost;

            OpenUpgradePopup(
                "도구 생산 효율을 업그레이드하시겠습니까?",
                cost,
                () => TryCompleteTrack(
                    ref toolDone,
                    () => manager.TryUpgradeToolEfficiency()));

            Debug.Log("도구 생산 효율을 업그레이드하시겠습니까?");
        }

        /// <summary>
        /// 마을 레벨업 버튼클릭
        /// </summary>
        private void OnClickVillageLevelUp()
        {
            // 기존 코드
            // TryVillageLevelUp();

            // 2026.07.30 KAY 수정 ---------------------------------------------------------------------
            // #19: 마을 레벨 상한(10) 도달 시 방어적으로 팝업조차 열지 않습니다.
            if (IsVillageLevelMaxed || !IsReadyForVillageLevelUp)
                return;

            long cost = GetVillageLevelUpCost();

            OpenUpgradePopup(
                $"마을을 Lv.{uiTownLevel + 1}로 업그레이드하시겠습니까?",
                cost,
                () => TryVillageLevelUp());
        }

        private void TryCompleteTrack(ref bool doneFlag, System.Func<bool> tryUpgrade)
        {
            // #19: doneFlag는 "이번 사이클(마을 레벨업 1회)에 1번만 구매 가능"을 막는 용도로,
            // 원래 마을 레벨업 시 ResetCycleAndRefreshUI()가 리셋해줬습니다. 엔드리스 모드에서는
            // 마을 레벨업 자체가 막혀있어 리셋이 다시는 일어나지 않으므로, doneFlag 체크/설정을
            // 건너뛰어 클릭/타이핑/도구효율 구매가 계속 반복 가능하도록 합니다.
            if (!isEndlessMode && doneFlag)
                return;

            if (tryUpgrade == null || !tryUpgrade())
                return;

            if (!isEndlessMode)
                doneFlag = true;

            RefreshAllUI();
        }

        /// <summary>
        /// 코인 소모 업그레이드 확인 팝업을 엽니다.
        /// </summary>
        private void OpenUpgradePopup(string message, long cost, Action confirmAction)
        {
            if (levelUpPopup == null)
            {
                Debug.LogWarning("[VillageUpgradeUI_Manager] LevelUpPopup이 연결되지 않았습니다.");
                return;
            }

            if (confirmAction == null)
                return;

            levelUpPopup.OpenLevelUpPopup(message, $"Expend Coin : <color=#4F2002><b>{cost:N0}</b></color>", confirmAction);
        }


        /// <summary>
        /// 마을 레벨이 상한(10)에 도달했는지. 엔드리스 모드여도 마을 레벨 자체는 10에서 고정되고,
        /// 클릭/타이핑/도구효율 3종 업그레이드만 상한이 풀립니다(사용자 확인).
        /// </summary>
        public bool IsVillageLevelMaxed => uiTownLevel >= NormalModeMaxTownLevel;

        /// <summary>
        /// 필수 3종 완료 + 비용 지불 가능 시 마을 레벨업을 수행합니다.
        /// 엔드리스 모드 여부와 무관하게 레벨10(완주)에서 멈춥니다.
        /// </summary>
        public bool TryVillageLevelUp()
        {
            if (IsVillageLevelMaxed)
                return false;

            if (!IsReadyForVillageLevelUp)
                return false;

            long cost = GetVillageLevelUpCost();
            if (CoinManager.Instance == null || !CoinManager.Instance.TrySpend(cost))
                return false;

            // #18: 도구 상한(ToolPlacementService.MaxPlacedToolCount)은 이제 InventoryManager_Tool.GetToolCapacity()에서
            // 마을 레벨 기준으로 직접 계산되므로, uiTownLevel만 올리면 자동으로 함께 늘어납니다(별도 수동 증가 불필요).
            uiTownLevel++;
            ResetCycleAndRefreshUI();
            return true;
        }

        /// <summary>
        /// 현재 마을 레벨 기준 레벨업 필요 코인.
        /// </summary>
        public long GetVillageLevelUpCost()
        {
            if (townUpgradeCostConfig == null)
                return 0;

            return townUpgradeCostConfig.GetCostForTownLevel(uiTownLevel);
        }

        /// <summary>
        /// 마을 레벨업 후 사이클 플래그/덮개/Required Upgrade/버튼을 초기화하고 표시를 갱신합니다.
        /// </summary>
        public void ResetCycleAndRefreshUI()
        {
            clickDone = false;
            typingDone = false;
            toolDone = false;
            RefreshAllUI();
        }

        public void RefreshAllUI()
        {
            if (uiController == null)
                return;

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

            // #19: 엔드리스 모드에서는 완료 사이클 개념이 없으므로(TryCompleteTrack 참고),
            // 일반 모드에서 이미 세팅됐던 done 플래그가 남아있어도 완료 덮개를 띄우지 않습니다.
            bool showClickComplete = !isEndlessMode && clickDone;
            bool showTypingComplete = !isEndlessMode && typingDone;
            bool showToolComplete = !isEndlessMode && toolDone;
            uiController.SetTrackComplete(showClickComplete, showTypingComplete, showToolComplete);
            uiController.RefreshRequiredUpgrade(CompletedCount);
            uiController.RefreshVillageLevel(uiTownLevel);

            long villageCost = GetVillageLevelUpCost();
            uiController.RefreshVillageLevelUpCost(villageCost);
            uiController.SetVillageLevelUpVisible(!IsVillageLevelMaxed && IsReadyForVillageLevelUp);

            RefreshInteractableStates();
            OnVillageUpgradeStateChanged?.Invoke();
        }

        private void OnUpgradePanelOpened()
        {
            // Canvas는 상시 활성, root만 토글되므로 오픈 시 세이브 반영 상태(요소/마을 레벨)를 다시 그립니다.
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

        private void RefreshInteractableStates()
        {
            if (uiController == null)
                return;

            long coin = CoinManager.Instance != null ? CoinManager.Instance.totalCoin : 0;
            TownUpgradeManager tum = TownUpgradeManager.Instance;

            bool clickEnabled = (isEndlessMode || !clickDone)
                && tum != null
                && !tum.IsClickUpgradeMaxLevel
                && coin >= tum.ClickUpgradeNextCost;

            bool typingEnabled = (isEndlessMode || !typingDone)
                && tum != null
                && !tum.IsTypingUpgradeMaxLevel
                && coin >= tum.TypingUpgradeNextCost;

            bool toolEnabled = (isEndlessMode || !toolDone)
                && tum != null
                && !tum.IsToolEfficiencyUpgradeMaxLevel
                && coin >= tum.ToolEfficiencyUpgradeNextCost;

            uiController.SetTrackButtonsInteractable(clickEnabled, typingEnabled, toolEnabled);

            long villageCost = GetVillageLevelUpCost();
            bool villageEnabled = !IsVillageLevelMaxed && IsReadyForVillageLevelUp && coin >= villageCost;
            uiController.SetVillageLevelUpInteractable(villageEnabled);
        }

        private IEnumerator SubscribeWhenDependenciesReady()
        {
            const float timeoutSeconds = 3f;
            float elapsed = 0f;

            while (elapsed < timeoutSeconds
                   && (CoinManager.Instance == null
                       || EarnProcessor.Instance == null
                       || TownUpgradeManager.Instance == null))
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            subscribeRoutine = null;
            TrySubscribeCoinEvent();
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
        /// 도구 상한은 uiTownLevel에서 자동으로 다시 계산되므로 별도 초기화가 필요 없습니다(#18).
        /// </summary>
        public void DebugSetTownLevel(int level)
        {
            uiTownLevel = Mathf.Max(1, level);
            ResetCycleAndRefreshUI(); // 내부에서 RefreshAllUI → UIController 갱신 + OnVillageUpgradeStateChanged
        }
        //--------------------------------------26.07.30 KNW---------------------------------------------------------
        /// <summary>
        /// 레벨10 완주 후 "엔드리스로 계속" 선택 시 호출합니다(#19). 완주 판정/선택 UI는 별도 작업으로 아직
        /// 이 메서드를 호출하는 곳이 없습니다 - 지금은 시그니처만 준비해둡니다.
        /// </summary>
        public void EnableEndlessMode()
        {
            isEndlessMode = true;
            RefreshAllUI();
        }

        /// <summary>
        /// 디버그/테스트 전용. 완주 선택 UI 없이 엔드리스 모드를 강제로 켜고 끕니다.
        /// </summary>
        public void DebugSetEndlessMode(bool value)
        {
            isEndlessMode = value;
            RefreshAllUI();
        }
        //--------------------------------------26.07.29 KDH---------------------------------------------------------
        /// <summary>
        /// 디버그용. UI 버튼과 동일하게 해당 트랙을 1회 완료 처리하고 UI를 갱신합니다.
        /// </summary>
        public bool DebugTryUpgradeClick()
        {
            if (clickDone) return false;

            if (TownUpgradeManager.Instance == null|| !TownUpgradeManager.Instance.DebugForceUpgradeClick()) return false;

            //clickDone = true;   마을 레벨에 제한이 걸리게 하는 코드
            RefreshAllUI();
            return true;
        }
        public bool DebugTryUpgradeTyping()
        {
            if (typingDone) return false;

            if (TownUpgradeManager.Instance == null || !TownUpgradeManager.Instance.DebugForceUpgradeTyping()) return false;

            //typingDone = true;
            RefreshAllUI();
            return true;
        }
        public bool DebugTryUpgradeTool()
        {
            if (toolDone) return false;

            if (TownUpgradeManager.Instance == null || !TownUpgradeManager.Instance.DebugForceUpgradeToolEfficiency()) return false;

            //toolDone = true;
            RefreshAllUI();
            return true;
        }
        //---------------------------------------------------------------------------------------------
    }
}