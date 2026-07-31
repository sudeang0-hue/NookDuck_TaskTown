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
        }
    }

    // -----------------------------------------------------------------------------
    // [ 2026.07.27 - Choi - 튜토리얼 기능 업데이트 ]
    // 기능: 튜토리얼 단계, 대화 위치, 실제 수동 코인 획득량을 저장합니다.
    // -----------------------------------------------------------------------------
    [Serializable]
    public class TutorialSaveData
    {
        public const int CurrentVersion = 2;

        public int version = CurrentVersion;
        public TutorialStep currentStep = TutorialStep.IntroDialogue;
        public int dialogueIndex;
        public long manualEarnedCoin;
        public long autoProductionEarnedCoin;
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
                rewardFlags = rewardFlags
            };

            copy.Normalize();
            return copy;
        }

        public void Normalize()
        {
            if (version < CurrentVersion)
                version = CurrentVersion;

            if (!Enum.IsDefined(typeof(TutorialStep), currentStep))
                currentStep = TutorialStep.IntroDialogue;

            dialogueIndex = Math.Max(0, dialogueIndex);
            manualEarnedCoin = Math.Max(0L, manualEarnedCoin);
            autoProductionEarnedCoin = Math.Max(0L, autoProductionEarnedCoin);
            rewardFlags = Math.Max(0, rewardFlags);

            // 보상 지급 직후 비정상 종료된 저장도 동물 뽑기 단계에서 안전하게 재개합니다.
            if (currentStep == TutorialStep.CollapseAndExpandTown &&
                HasProgressFlag(TutorialProgressFlags.TownWindowRewardGranted))
            {
                currentStep = TutorialStep.DrawAnimal;
                dialogueIndex = 0;
            }

            // 도구 뽑기용 보상 플래그가 저장된 비정상 종료 상태는 다음 단계에서 재개합니다.
            if (currentStep == TutorialStep.DrawAnimal &&
                HasProgressFlag(TutorialProgressFlags.ToolDrawCoinRewardGranted))
            {
                currentStep = TutorialStep.DrawTool;
                dialogueIndex = 0;
            }
        }

        public bool HasProgressFlag(TutorialProgressFlags flag)
        {
            return (rewardFlags & (int)flag) != 0;
        }

        public void SetProgressFlag(TutorialProgressFlags flag)
        {
            rewardFlags |= (int)flag;
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
