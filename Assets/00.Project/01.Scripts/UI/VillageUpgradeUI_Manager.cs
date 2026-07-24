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
    public class VillageUpgradeUI_Manager : MonoBehaviour
    {
        private const int RequiredUpgradeTotal = 3;

        /// <summary>
        /// 트랙 완료/마을 레벨/비용 UI 상태가 바뀔 때 발행합니다.
        /// VillageInfo 등 외부 패널 동기화용입니다.
        /// </summary>
        public event Action OnVillageUpgradeStateChanged;

        [Header("참조")]
        [SerializeField] private UIController_VillageUpgrade uiController;

        [Header("마을 레벨업 비용")]
        [SerializeField] private TownUpgradeCostConfig townUpgradeCostConfig = new TownUpgradeCostConfig();

        [Header("UI 전용 마을 레벨")]
        [SerializeField] private int uiTownLevel = 1;

        private bool clickDone;
        private bool typingDone;
        private bool toolDone;

        private bool isCoinSubscribed;
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

        private void Awake()
        {
            if (uiController == null)
                uiController = GetComponent<UIController_VillageUpgrade>();

            BindButtons();
        }

        private void OnDestroy()
        {
            UnbindButtons();
            UnsubscribeCoinEvent();
        }

        private void OnEnable()
        {
            TrySubscribeCoinEvent();
            RefreshAllUI();

            if (!isCoinSubscribed && subscribeRoutine == null)
                subscribeRoutine = StartCoroutine(SubscribeWhenCoinManagerReady());
        }

        private void OnDisable()
        {
            if (subscribeRoutine != null)
            {
                StopCoroutine(subscribeRoutine);
                subscribeRoutine = null;
            }

            UnsubscribeCoinEvent();
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

        private void OnClickUpgradeClick()
        {
            TryCompleteTrack(ref clickDone, () =>
                TownUpgradeManager.Instance != null && TownUpgradeManager.Instance.TryUpgradeClick());
        }

        private void OnClickUpgradeTyping()
        {
            TryCompleteTrack(ref typingDone, () =>
                TownUpgradeManager.Instance != null && TownUpgradeManager.Instance.TryUpgradeTyping());
        }

        private void OnClickUpgradeTool()
        {
            TryCompleteTrack(ref toolDone, () =>
                TownUpgradeManager.Instance != null && TownUpgradeManager.Instance.TryUpgradeToolEfficiency());
        }

        private void TryCompleteTrack(ref bool doneFlag, System.Func<bool> tryUpgrade)
        {
            if (doneFlag)
                return;

            if (tryUpgrade == null || !tryUpgrade())
                return;

            doneFlag = true;
            RefreshAllUI();
        }

        private void OnClickVillageLevelUp()
        {
            TryVillageLevelUp();
        }

        /// <summary>
        /// 필수 3종 완료 + 비용 지불 가능 시 마을 레벨업을 수행합니다.
        /// VillageInfo의 Lv_Up_Button에서도 호출합니다.
        /// </summary>
        public bool TryVillageLevelUp()
        {
            if (!IsReadyForVillageLevelUp)
                return false;

            long cost = GetVillageLevelUpCost();
            if (CoinManager.Instance == null || !CoinManager.Instance.TrySpend(cost))
                return false;

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

            // 각 _pan Value: 클릭/타이핑은 EarnProcessor 현재 획득량, 도구효율은 TownUpgrade 배율
            int clickValue = 0;
            int typingValue = 0;
            if (EarnProcessor.Instance != null)
            {
                clickValue = EarnProcessor.Instance.BaseCoinPerClick * EarnProcessor.Instance.ClickMultiplier;
                typingValue = EarnProcessor.Instance.BaseCoinPerTyping * EarnProcessor.Instance.TypingMultiplier;
            }

            float toolProduct = tum != null ? tum.ToolEfficiencyMultiplier : 1f;
            uiController.RefreshTrackValues(clickValue, typingValue, toolProduct);

            uiController.SetTrackComplete(clickDone, typingDone, toolDone);
            uiController.RefreshRequiredUpgrade(CompletedCount);
            uiController.RefreshVillageLevel(uiTownLevel);

            long villageCost = GetVillageLevelUpCost();
            uiController.RefreshVillageLevelUpCost(villageCost);
            uiController.SetVillageLevelUpVisible(IsReadyForVillageLevelUp);

            RefreshInteractableStates();
            OnVillageUpgradeStateChanged?.Invoke();
        }

        private void RefreshInteractableStates()
        {
            if (uiController == null)
                return;

            long coin = CoinManager.Instance != null ? CoinManager.Instance.totalCoin : 0;
            TownUpgradeManager tum = TownUpgradeManager.Instance;

            bool clickEnabled = !clickDone
                && tum != null
                && !tum.IsClickUpgradeMaxLevel
                && coin >= tum.ClickUpgradeNextCost;

            bool typingEnabled = !typingDone
                && tum != null
                && !tum.IsTypingUpgradeMaxLevel
                && coin >= tum.TypingUpgradeNextCost;

            bool toolEnabled = !toolDone
                && tum != null
                && !tum.IsToolEfficiencyUpgradeMaxLevel
                && coin >= tum.ToolEfficiencyUpgradeNextCost;

            uiController.SetTrackButtonsInteractable(clickEnabled, typingEnabled, toolEnabled);

            long villageCost = GetVillageLevelUpCost();
            bool villageEnabled = IsReadyForVillageLevelUp && coin >= villageCost;
            uiController.SetVillageLevelUpInteractable(villageEnabled);
        }

        private IEnumerator SubscribeWhenCoinManagerReady()
        {
            const float timeoutSeconds = 3f;
            float elapsed = 0f;

            while (CoinManager.Instance == null && elapsed < timeoutSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            subscribeRoutine = null;
            TrySubscribeCoinEvent();
            RefreshInteractableStates();
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
    }
}