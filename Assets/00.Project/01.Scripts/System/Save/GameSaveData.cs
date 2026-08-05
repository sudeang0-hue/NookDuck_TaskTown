using System;
using System.Collections.Generic;
using TaskTown.Tutorial;

namespace TaskTown.KDH
{
    // 디스크(JSON)에 저장하기 위한 게임 진행 데이터입니다. 인벤토리 슬롯이 들고 있는
    // ScriptableObject(AnimalDataSO/ToolDataSO)는 세션 간 JSON 직렬화가 안 되므로,
    // 여기서는 문자열 ID만 저장하고 로드 시 데이터베이스에서 다시 조회합니다.
    [System.Serializable]
    public class GameSaveData
    {
        public long coins;
        public int townLevel = 1;
        // 저장 시점의 초당 생산량. 오프라인 보상은 로드 타이밍(인벤토리 복원 완료 여부)에
        // 의존하지 않도록, 실시간 생산량 대신 이 저장값으로 계산합니다.
        public float productionRatePerSecond;
        // 클릭/타이핑/도구효율 업그레이드 레벨(TownUpgradeManager, 이슈 #72)
        public int clickUpgradeLevel;
        public int typingUpgradeLevel;
        public int toolEfficiencyUpgradeLevel;
        public List<AnimalSaveEntry> animals = new List<AnimalSaveEntry>();
        public List<ToolSaveEntry> tools = new List<ToolSaveEntry>();

        // 2026.08.02 - KAY - 마을 사이클 게이트/엔드리스를 JSON에 포함 (VillageSystemManager 연동)
        // 구버전 세이브에 필드가 없으면 역직렬화 시 기본값(0)으로 유지됩니다.
        // 3종 업그레이드 10->20 세분화(사용자 확인)로 bool(1회)에서 int(트랙당 필요 횟수)로 변경.
        // 구버전 세이브의 cycleClickDone 등 bool 필드는 이름이 달라 무시되고, 진행중이던 사이클만
        // 0으로 리셋됩니다(실제 업그레이드 레벨/코인 등 다른 진행도는 영향 없음).
        public int cycleClickCount;
        public int cycleTypingCount;
        public int cycleToolCount;
        public bool isEndlessMode;
        // 배치 동물 ID는 팀원 AnimalSet 흐름 정리 후 추가 예정 (현재 단계 보류)

        // -----------------------------------------------------------------------------
        // [ 2026.07.27 - Choi - 튜토리얼 기능 업데이트 ]
        // 기능: 기존 게임 저장 데이터에 튜토리얼 진행 상태를 포함하고 누락 값을 보정합니다.
        // -----------------------------------------------------------------------------
        public TutorialSaveData tutorial = TutorialSaveData.CreateDefault();

        /// <summary>
        /// 이전 버전 저장 데이터의 누락 컬렉션과 튜토리얼 값을 안전한 기본값으로 보정합니다.
        /// </summary>
        public void Normalize()
        {
            animals ??= new List<AnimalSaveEntry>();
            tools ??= new List<ToolSaveEntry>();
            tutorial ??= TutorialSaveData.CreateDefault();
            tutorial.Normalize();

            // 2026.08.02 - KAY - 마을 레벨 하한 보정 (사이클 bool은 기본 false로 충분)
            if (townLevel < 1)
                townLevel = 1;
        }
    }

    // -----------------------------------------------------------------------------
    // [ 2026.07.27 - Choi - 튜토리얼 기능 업데이트 ]
    // 기능: 튜토리얼 단계, 대화 위치, 실제 수동 코인 획득량을 저장합니다.
    // -----------------------------------------------------------------------------
    [Serializable]
    public class TutorialSaveData
    {
        private const int RunProgressFlagMask =
            (int)TutorialProgressFlags.TownWindowGuideCompleted |
            (int)TutorialProgressFlags.TownWindowMinimized |
            (int)TutorialProgressFlags.TownWindowExpanded;
        private const int OneTimeRewardFlagMask =
            (int)TutorialProgressFlags.TownWindowRewardGranted |
            (int)TutorialProgressFlags.ToolDrawCoinRewardGranted;

        // -----------------------------------------------------------------------------
        // [ 2026.07.31 - Choi - 튜토리얼 단계 확장 ]
        // 기능: 동물 뽑기 결과 설명과 마을 배치 단계를 포함한 저장 데이터를 구분합니다.
        // -----------------------------------------------------------------------------
        public const int CurrentVersion = 4;

        public int version = CurrentVersion;
        public TutorialStep currentStep = TutorialStep.IntroDialogue;
        public int dialogueIndex;
        public long manualEarnedCoin;
        public long autoProductionEarnedCoin;

        // -----------------------------------------------------------------------------
        // [ 2026.08.03 - Choi - 튜토리얼 다시 보기 저장 분리 ]
        // 기능: 현재 실행의 세부 진행은 초기화할 수 있게 분리하고, 일회성 보상 기록은 보존합니다.
        // -----------------------------------------------------------------------------
        public int progressFlags;
        public int rewardFlags;

        public bool IsCompleted => currentStep == TutorialStep.Completed;

        public static TutorialSaveData CreateDefault()
        {
            return new TutorialSaveData();
        }

        public TutorialSaveData Copy()
        {
            TutorialSaveData copy = new TutorialSaveData
            {
                version = version,
                currentStep = currentStep,
                dialogueIndex = dialogueIndex,
                manualEarnedCoin = manualEarnedCoin,
                autoProductionEarnedCoin = autoProductionEarnedCoin,
                progressFlags = progressFlags,
                rewardFlags = rewardFlags
            };

            copy.Normalize();
            return copy;
        }

        public void Normalize()
        {
            bool requiresLegacyRecovery = version < CurrentVersion;

            if (!Enum.IsDefined(typeof(TutorialStep), currentStep))
                currentStep = TutorialStep.IntroDialogue;

            dialogueIndex = Math.Max(0, dialogueIndex);
            manualEarnedCoin = Math.Max(0L, manualEarnedCoin);
            autoProductionEarnedCoin = Math.Max(0L, autoProductionEarnedCoin);
            int normalizedFlags = Math.Max(0, progressFlags) |
                                  Math.Max(0, rewardFlags);
            progressFlags = normalizedFlags & RunProgressFlagMask;
            rewardFlags = normalizedFlags & OneTimeRewardFlagMask;

            // v3 이하 저장은 진행/보상 플래그가 하나의 필드에 섞여 있었습니다.
            // 이미 지급된 보상 플래그와 이전 단계가 함께 남은 경우에만 한 번 복구합니다.
            if (requiresLegacyRecovery &&
                currentStep == TutorialStep.CollapseAndExpandTown &&
                HasProgressFlag(TutorialProgressFlags.TownWindowRewardGranted))
            {
                currentStep = TutorialStep.DrawAnimal;
                dialogueIndex = 0;
            }

            if (requiresLegacyRecovery &&
                currentStep == TutorialStep.DrawAnimal &&
                HasProgressFlag(TutorialProgressFlags.ToolDrawCoinRewardGranted))
            {
                currentStep = TutorialStep.AnimalDrawExplanation;
                dialogueIndex = 0;
            }

            version = Math.Max(version, CurrentVersion);
        }

        public bool HasProgressFlag(TutorialProgressFlags flag)
        {
            int flagValue = (int)flag;
            if (flagValue == 0)
                return false;

            int allFlags = progressFlags | rewardFlags;
            return (allFlags & flagValue) == flagValue;
        }

        public void SetProgressFlag(TutorialProgressFlags flag)
        {
            int flagValue = (int)flag;
            progressFlags |= flagValue & RunProgressFlagMask;
            rewardFlags |= flagValue & OneTimeRewardFlagMask;
        }

        /// <summary>
        /// 현재 튜토리얼 진행만 처음으로 되돌리고 이미 받은 일회성 보상 기록은 유지합니다.
        /// 설정의 '튜토리얼 다시 보기'에서 사용하는 저장 복사본입니다.
        /// </summary>
        public TutorialSaveData CreateReplayProgress()
        {
            TutorialSaveData normalized = Copy();
            return new TutorialSaveData
            {
                version = CurrentVersion,
                currentStep = TutorialStep.IntroDialogue,
                dialogueIndex = 0,
                manualEarnedCoin = 0L,
                autoProductionEarnedCoin = 0L,
                progressFlags = 0,
                rewardFlags = normalized.rewardFlags & OneTimeRewardFlagMask
            };
        }
    }

    [System.Serializable]
    public class AnimalSaveEntry
    {
        public string id;
        public int level;
        public int count;
    }

    [System.Serializable]
    public class ToolSaveEntry
    {
        public string id;
        public int level;
        public int count;
        public bool currentSet;
        public bool currentAnimalSet;
        public string currentAnimalId;
        // 26.07.29. KAY 수정
        public bool hasRevealedSpecialAnimal;
    }
}
