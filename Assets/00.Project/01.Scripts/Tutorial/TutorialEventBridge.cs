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
        [SerializeField] private VillageAnimalSetUI_Manager villageAnimalSetUIManager;
        [SerializeField] private VillageInfoUI_Manager villageInfoUIManager;
        [SerializeField] private TownUpgradeManager townUpgradeManager;
        [SerializeField] private GameMasterManager gameMasterManager;

        [Header("튜토리얼 버튼 강조")]
        [SerializeField] private TutorialButtonHighlighter buttonHighlighter;
        [SerializeField] private UIController_Menu menuController;
        [SerializeField] private UIController_Gacha gachaController;
        [SerializeField] private UIController_VillageAnimalSet villageAnimalSetController;

        private readonly HashSet<string> assignedToolIds = new HashSet<string>();
        private readonly List<string> initialVillageAnimalIds = new List<string>();

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
            tutorialManager.ProgressChanged -= HandleProgressChanged;
            tutorialManager.ProgressChanged += HandleProgressChanged;
            tutorialManager.TutorialRestarted -= HandleTutorialRestarted;
            tutorialManager.TutorialRestarted += HandleTutorialRestarted;
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
                tutorialManager.ProgressChanged -= HandleProgressChanged;
                tutorialManager.TutorialRestarted -= HandleTutorialRestarted;
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
            if (villageAnimalSetUIManager == null)
            {
                villageAnimalSetUIManager = FindFirstObjectByType<VillageAnimalSetUI_Manager>(
                    FindObjectsInactive.Include);
            }
            if (villageInfoUIManager == null)
                villageInfoUIManager = FindAnyObjectByType<VillageInfoUI_Manager>();
            if (townUpgradeManager == null)
                townUpgradeManager = TownUpgradeManager.Instance;
            if (gameMasterManager == null)
                gameMasterManager = FindAnyObjectByType<GameMasterManager>();
            if (buttonHighlighter == null)
                TryGetComponent(out buttonHighlighter);
            if (menuController == null)
                menuController = FindAnyObjectByType<UIController_Menu>();
            if (gachaController == null)
                gachaController = FindAnyObjectByType<UIController_Gacha>();
            if (villageAnimalSetController == null)
            {
                villageAnimalSetController = FindFirstObjectByType<UIController_VillageAnimalSet>(
                    FindObjectsInactive.Include);
            }
        }

        private void HandleStepChanged(TutorialStep previousStep, TutorialStep nextStep)
        {
            UnsubscribeCurrentStep();

            if (nextStep != TutorialStep.Completed)
                SubscribeCurrentStep(nextStep);
        }

        private void HandleProgressChanged(TutorialSaveData progress)
        {
            RefreshButtonHighlight();
        }

        private void HandleTutorialRestarted()
        {
            UnsubscribeCurrentStep();

            if (tutorialManager != null && !tutorialManager.IsCompleted)
                SubscribeCurrentStep(tutorialManager.CurrentStep);

            RefreshButtonHighlight();
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

            SynchronizeWindowState();
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
            SynchronizeWindowState();
        }

        private void SynchronizeWindowState()
        {
            if (tutorialManager == null || gameMasterManager == null)
                return;

            bool isExpanded = gameMasterManager.GetIsExpanded();
            bool isWindowTutorial =
                tutorialManager.CurrentStep == TutorialStep.CollapseAndExpandTown;

            if (isWindowTutorial && tutorialManager.IsTownWindowGuideCompleted)
            {
                if (isExpanded)
                {
                    if (tutorialManager.IsTownWindowMinimized &&
                        !tutorialManager.IsTownWindowExpanded)
                    {
                        tutorialManager.ReportSignal(
                            TutorialSignalType.TownWindowExpanded);
                    }
                }
                else if (!tutorialManager.IsTownWindowMinimized)
                {
                    tutorialManager.ReportSignal(
                        TutorialSignalType.TownWindowMinimized);
                }
            }

            tutorialManager.SetPaused(!isExpanded);
            RefreshButtonHighlight();
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

                case TutorialStep.CollapseAndExpandTown:
                    SynchronizeWindowState();
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

                case TutorialStep.PlaceAnimalInVillage:
                    if (villageAnimalSetUIManager != null)
                    {
                        CaptureVillagePlacement();
                        villageAnimalSetUIManager.OnVillagePlacementChanged +=
                            HandleVillagePlacementChanged;
                    }
                    else
                    {
                        WarnMissingSource(nameof(VillageAnimalSetUI_Manager), step);
                    }
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

            RefreshButtonHighlight();
        }

        private void UnsubscribeCurrentStep()
        {
            buttonHighlighter?.Clear();

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

                case TutorialStep.PlaceAnimalInVillage:
                    if (villageAnimalSetUIManager != null)
                    {
                        villageAnimalSetUIManager.OnVillagePlacementChanged -=
                            HandleVillagePlacementChanged;
                    }
                    initialVillageAnimalIds.Clear();
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
            tutorialManager?.ReportSignal(
                TutorialSignalType.AutoProductionConfirmed,
                amount);
        }

        private void CaptureVillagePlacement()
        {
            initialVillageAnimalIds.Clear();
            if (villageAnimalSetUIManager == null)
                return;

            IReadOnlyList<string> placedIds = villageAnimalSetUIManager.PlacedAnimalIds;
            if (placedIds == null)
                return;

            for (int index = 0; index < placedIds.Count; index++)
                initialVillageAnimalIds.Add(placedIds[index] ?? string.Empty);
        }

        private void HandleVillagePlacementChanged()
        {
            if (!HasValidVillagePlacementChange())
                return;

            tutorialManager?.ReportSignal(TutorialSignalType.VillageAnimalPlaced);
        }

        private bool HasValidVillagePlacementChange()
        {
            if (villageAnimalSetUIManager == null)
                return false;

            IReadOnlyList<string> currentIds = villageAnimalSetUIManager.PlacedAnimalIds;
            if (currentIds == null)
                return false;

            int initialPlacedCount = 0;
            int currentPlacedCount = 0;
            bool hasChanged = currentIds.Count != initialVillageAnimalIds.Count;
            int compareCount = Mathf.Max(currentIds.Count, initialVillageAnimalIds.Count);

            for (int index = 0; index < compareCount; index++)
            {
                string initialId = index < initialVillageAnimalIds.Count
                    ? initialVillageAnimalIds[index]
                    : string.Empty;
                string currentId = index < currentIds.Count
                    ? currentIds[index] ?? string.Empty
                    : string.Empty;

                if (!string.IsNullOrEmpty(initialId))
                    initialPlacedCount++;
                if (!string.IsNullOrEmpty(currentId))
                    currentPlacedCount++;
                if (!string.Equals(initialId, currentId, System.StringComparison.Ordinal))
                    hasChanged = true;
            }

            // 단순 확정이나 주민 제거만으로는 완료하지 않고, 한 명 이상을 유지한 실제 배치 변경만 인정합니다.
            return hasChanged &&
                   currentPlacedCount > 0 &&
                   currentPlacedCount >= initialPlacedCount;
        }

        private void RefreshButtonHighlight()
        {
            if (buttonHighlighter == null || tutorialManager == null)
                return;

            switch (tutorialManager.CurrentStep)
            {
                case TutorialStep.CollapseAndExpandTown:
                    if (!tutorialManager.IsTownWindowGuideCompleted)
                    {
                        buttonHighlighter.Clear();
                    }
                    else if (!tutorialManager.IsTownWindowMinimized)
                    {
                        buttonHighlighter.Highlight(gameMasterManager?.btnMinimize);
                    }
                    else if (!tutorialManager.IsTownWindowExpanded)
                    {
                        buttonHighlighter.Highlight(gameMasterManager?.btnMaximize);
                    }
                    else
                    {
                        buttonHighlighter.Clear();
                    }
                    break;

                case TutorialStep.DrawAnimal:
                    buttonHighlighter.Highlight(
                        menuController?.GachaButton,
                        gachaController?.AnimalOnePickButton);
                    break;

                case TutorialStep.DrawTool:
                    buttonHighlighter.Highlight(
                        menuController?.GachaButton,
                        gachaController?.ToolOnePickButton);
                    break;

                case TutorialStep.PlaceAnimalInVillage:
                    buttonHighlighter.Highlight(
                        villageAnimalSetController?.EditSetAnimalButton,
                        villageAnimalSetController?.ConfirmSetAnimalButton);
                    break;

                default:
                    buttonHighlighter.Clear();
                    break;
            }
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
