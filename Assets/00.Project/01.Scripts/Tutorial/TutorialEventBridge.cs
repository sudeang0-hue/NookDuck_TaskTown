using System.Collections;
using System.Collections.Generic;
using TaskTown.Gacha;
using TaskTown.KDH;
using UI;
using UnityEngine;

namespace TaskTown.Tutorial
{
    /// <summary>
    /// 현재 튜토리얼 단계에 필요한 게임 이벤트 하나만 구독하여 진행 신호로 변환합니다.
    /// 단계 전환 또는 튜토리얼 완료 시 이전 구독을 즉시 해제합니다.
    /// </summary>
    [DefaultExecutionOrder(-80)]
    public sealed class TutorialEventBridge : MonoBehaviour
    {
        [Header("튜토리얼")]
        [SerializeField] private TutorialManager tutorialManager;

        [Header("게임 이벤트 소스 (비어 있으면 시작 시 1회 자동 탐색)")]
        [SerializeField] private EarnProcessor earnProcessor;
        [SerializeField] private AnimalGachaManager animalGachaManager;
        [SerializeField] private ToolGachaManager toolGachaManager;
        [SerializeField] private InventoryManager_Tool toolInventoryManager;
        [SerializeField] private RealProductionTicker realProductionTicker;
        [SerializeField] private VillageInfoUI_Manager villageInfoUIManager;
        [SerializeField] private TownUpgradeManager townUpgradeManager;
        [SerializeField] private GameMasterManager gameMasterManager;

        private readonly HashSet<string> assignedToolIds = new HashSet<string>();

        private bool hasStarted;
        private bool isConnected;
        private TutorialStep? subscribedStep;
        private Coroutine windowStateSyncCoroutine;

        private void Start()
        {
            hasStarted = true;
            Connect();
        }

        private void OnEnable()
        {
            if (hasStarted)
                Connect();
        }

        private void OnDisable()
        {
            Disconnect();
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        /// <summary>
        /// Additive Scene에서는 다른 Scene 오브젝트를 직렬화 참조할 수 없으므로,
        /// 비어 있는 참조만 시작 시 한 번 탐색합니다.
        /// </summary>
        public void Connect()
        {
            if (isConnected)
                return;

            ResolveReferences();
            if (tutorialManager == null)
            {
                Debug.LogWarning(
                    "[TutorialEventBridge] TutorialManager를 찾을 수 없어 이벤트를 연결하지 않습니다.",
                    this);
                return;
            }

            tutorialManager.StepChanged -= HandleStepChanged;
            tutorialManager.StepChanged += HandleStepChanged;
            tutorialManager.TutorialCompleted -= HandleTutorialCompleted;
            tutorialManager.TutorialCompleted += HandleTutorialCompleted;

            SubscribeWindowState();
            isConnected = true;

            if (!tutorialManager.IsCompleted)
                SubscribeCurrentStep(tutorialManager.CurrentStep);
        }

        public void Disconnect()
        {
            UnsubscribeCurrentStep();
            UnsubscribeWindowState();

            if (tutorialManager != null)
            {
                tutorialManager.StepChanged -= HandleStepChanged;
                tutorialManager.TutorialCompleted -= HandleTutorialCompleted;
            }

            isConnected = false;
        }

        private void ResolveReferences()
        {
            if (tutorialManager == null)
            {
                tutorialManager = GetComponent<TutorialManager>();
                if (tutorialManager == null)
                    tutorialManager = FindAnyObjectByType<TutorialManager>();
            }

            if (earnProcessor == null)
                earnProcessor = EarnProcessor.Instance;
            if (animalGachaManager == null)
                animalGachaManager = FindAnyObjectByType<AnimalGachaManager>();
            if (toolGachaManager == null)
                toolGachaManager = FindAnyObjectByType<ToolGachaManager>();
            if (toolInventoryManager == null)
                toolInventoryManager = InventoryManager_Tool.Instance;
            if (realProductionTicker == null)
                realProductionTicker = RealProductionTicker.Instance;
            if (villageInfoUIManager == null)
                villageInfoUIManager = FindAnyObjectByType<VillageInfoUI_Manager>();
            if (townUpgradeManager == null)
                townUpgradeManager = TownUpgradeManager.Instance;
            if (gameMasterManager == null)
                gameMasterManager = FindAnyObjectByType<GameMasterManager>();
        }

        private void HandleStepChanged(TutorialStep previousStep, TutorialStep nextStep)
        {
            UnsubscribeCurrentStep();

            if (nextStep != TutorialStep.Completed)
                SubscribeCurrentStep(nextStep);
        }

        private void HandleTutorialCompleted()
        {
            Disconnect();
            enabled = false;
        }

        private void SubscribeWindowState()
        {
            if (gameMasterManager == null)
                return;

            tutorialManager.SetPaused(!gameMasterManager.GetIsExpanded());

            if (gameMasterManager.btnMinimize != null)
            {
                gameMasterManager.btnMinimize.onClick.RemoveListener(HandleWindowStateButtonClicked);
                gameMasterManager.btnMinimize.onClick.AddListener(HandleWindowStateButtonClicked);
            }

            if (gameMasterManager.btnMaximize != null)
            {
                gameMasterManager.btnMaximize.onClick.RemoveListener(HandleWindowStateButtonClicked);
                gameMasterManager.btnMaximize.onClick.AddListener(HandleWindowStateButtonClicked);
            }
        }

        private void UnsubscribeWindowState()
        {
            if (windowStateSyncCoroutine != null)
            {
                StopCoroutine(windowStateSyncCoroutine);
                windowStateSyncCoroutine = null;
            }

            if (gameMasterManager == null)
                return;

            if (gameMasterManager.btnMinimize != null)
                gameMasterManager.btnMinimize.onClick.RemoveListener(HandleWindowStateButtonClicked);

            if (gameMasterManager.btnMaximize != null)
                gameMasterManager.btnMaximize.onClick.RemoveListener(HandleWindowStateButtonClicked);
        }

        private void HandleWindowStateButtonClicked()
        {
            if (windowStateSyncCoroutine != null)
                StopCoroutine(windowStateSyncCoroutine);

            windowStateSyncCoroutine = StartCoroutine(SyncWindowStateNextFrame());
        }

        private IEnumerator SyncWindowStateNextFrame()
        {
            yield return null;

            windowStateSyncCoroutine = null;
            if (tutorialManager != null && gameMasterManager != null)
                tutorialManager.SetPaused(!gameMasterManager.GetIsExpanded());
        }

        private void SubscribeCurrentStep(TutorialStep step)
        {
            subscribedStep = step;

            switch (step)
            {
                case TutorialStep.EarnManualCoin:
                    if (earnProcessor != null)
                        earnProcessor.ManualCoinGranted += HandleManualCoinGranted;
                    else
                        WarnMissingSource(nameof(EarnProcessor), step);
                    break;

                case TutorialStep.DrawAnimal:
                    if (animalGachaManager != null)
                        animalGachaManager.OnGachaResolved += HandleAnimalDrawn;
                    else
                        WarnMissingSource(nameof(AnimalGachaManager), step);
                    break;

                case TutorialStep.DrawTool:
                    if (toolGachaManager != null)
                        toolGachaManager.OnGachaResolved += HandleToolDrawn;
                    else
                        WarnMissingSource(nameof(ToolGachaManager), step);
                    break;

                case TutorialStep.AssignAnimal:
                    if (toolInventoryManager != null)
                    {
                        CaptureAssignedTools();
                        toolInventoryManager.OnToolSlotChanged += HandleToolSlotChanged;
                    }
                    else
                    {
                        WarnMissingSource(nameof(InventoryManager_Tool), step);
                    }
                    break;

                case TutorialStep.ConfirmAutoProduction:
                    if (realProductionTicker != null)
                        realProductionTicker.ProductionCoinGranted += HandleProductionCoinGranted;
                    else
                        WarnMissingSource(nameof(RealProductionTicker), step);
                    break;

                case TutorialStep.OpenVillageInfo:
                    if (villageInfoUIManager != null)
                        villageInfoUIManager.PanelOpened += HandleVillageInfoOpened;
                    else
                        WarnMissingSource(nameof(VillageInfoUI_Manager), step);
                    break;

                case TutorialStep.UpgradeVillage:
                    if (townUpgradeManager != null)
                        townUpgradeManager.UpgradePurchased += HandleUpgradePurchased;
                    else
                        WarnMissingSource(nameof(TownUpgradeManager), step);
                    break;
            }
        }

        private void UnsubscribeCurrentStep()
        {
            if (!subscribedStep.HasValue)
                return;

            switch (subscribedStep.Value)
            {
                case TutorialStep.EarnManualCoin:
                    if (earnProcessor != null)
                        earnProcessor.ManualCoinGranted -= HandleManualCoinGranted;
                    break;

                case TutorialStep.DrawAnimal:
                    if (animalGachaManager != null)
                        animalGachaManager.OnGachaResolved -= HandleAnimalDrawn;
                    break;

                case TutorialStep.DrawTool:
                    if (toolGachaManager != null)
                        toolGachaManager.OnGachaResolved -= HandleToolDrawn;
                    break;

                case TutorialStep.AssignAnimal:
                    if (toolInventoryManager != null)
                        toolInventoryManager.OnToolSlotChanged -= HandleToolSlotChanged;
                    assignedToolIds.Clear();
                    break;

                case TutorialStep.ConfirmAutoProduction:
                    if (realProductionTicker != null)
                        realProductionTicker.ProductionCoinGranted -= HandleProductionCoinGranted;
                    break;

                case TutorialStep.OpenVillageInfo:
                    if (villageInfoUIManager != null)
                        villageInfoUIManager.PanelOpened -= HandleVillageInfoOpened;
                    break;

                case TutorialStep.UpgradeVillage:
                    if (townUpgradeManager != null)
                        townUpgradeManager.UpgradePurchased -= HandleUpgradePurchased;
                    break;
            }

            subscribedStep = null;
        }

        private void CaptureAssignedTools()
        {
            assignedToolIds.Clear();
            if (toolInventoryManager == null)
                return;

            foreach (SlotData_Tool slot in toolInventoryManager.ToolSlotsList)
            {
                if (slot != null && slot.CurrentAnimalSet && !string.IsNullOrEmpty(slot.ToolId))
                    assignedToolIds.Add(slot.ToolId);
            }
        }

        private void HandleManualCoinGranted(int amount)
        {
            tutorialManager?.ReportSignal(TutorialSignalType.ManualCoinEarned, amount);
        }

        private void HandleAnimalDrawn(GachaResult result)
        {
            tutorialManager?.ReportSignal(TutorialSignalType.AnimalDrawn);
        }

        private void HandleToolDrawn(GachaResult result)
        {
            tutorialManager?.ReportSignal(TutorialSignalType.ToolDrawn);
        }

        private void HandleToolSlotChanged(SlotData_Tool slot)
        {
            if (slot == null || string.IsNullOrEmpty(slot.ToolId))
                return;

            bool wasAssigned = assignedToolIds.Contains(slot.ToolId);
            if (!slot.CurrentAnimalSet)
            {
                assignedToolIds.Remove(slot.ToolId);
                return;
            }

            assignedToolIds.Add(slot.ToolId);
            if (!wasAssigned)
                tutorialManager?.ReportSignal(TutorialSignalType.AnimalAssigned);
        }

        private void HandleProductionCoinGranted(int amount)
        {
            tutorialManager?.ReportSignal(TutorialSignalType.AutoProductionConfirmed);
        }

        private void HandleVillageInfoOpened()
        {
            tutorialManager?.ReportSignal(TutorialSignalType.VillageInfoOpened);
        }

        private void HandleUpgradePurchased()
        {
            tutorialManager?.ReportSignal(TutorialSignalType.AnyUpgradePurchased);
        }

        private void WarnMissingSource(string sourceName, TutorialStep step)
        {
            Debug.LogWarning(
                $"[TutorialEventBridge] {step} 단계 이벤트 소스 {sourceName}을(를) 찾을 수 없습니다.",
                this);
        }
    }
}
