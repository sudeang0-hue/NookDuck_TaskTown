using System;
using System.Collections.Generic;
using UnityEngine;

namespace TaskTown.Tutorial
{
    public enum TutorialProgressDisplayType
    {
        None = 0,
        ManualCoin = 1
    }

    [Serializable]
    public sealed class TutorialStepContent
    {
        [SerializeField] private TutorialStep step;
        [SerializeField, TextArea(2, 6)] private List<string> messages = new();
        [SerializeField, TextArea(2, 4)] private string objectiveText;
        [SerializeField] private TutorialProgressDisplayType progressDisplayType;
        [SerializeField] private string progressFormat = "{0} / {1}";
        [SerializeField] private bool allowClickAdvance;

        public TutorialStep Step => step;
        public IReadOnlyList<string> Messages => messages;
        public string ObjectiveText => objectiveText;
        public TutorialProgressDisplayType ProgressDisplayType => progressDisplayType;
        public string ProgressFormat => progressFormat;
        public bool AllowClickAdvance => allowClickAdvance;
    }

    /// <summary>
    /// 튜토리얼 단계별 대사와 표시 규칙을 보관하는 정적 설정 데이터입니다.
    /// 진행 상태나 저장 데이터는 이 에셋에 기록하지 않습니다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "TutorialConfig",
        menuName = "TaskTown/Tutorial/Tutorial Config")]
    public sealed class TutorialConfigSO : ScriptableObject
    {
        [SerializeField] private string speakerName = "신사 오리";
        [SerializeField] private Sprite speakerPortrait;
        [SerializeField] private List<TutorialStepContent> steps = new();

        public string SpeakerName => speakerName;
        public Sprite SpeakerPortrait => speakerPortrait;
        public IReadOnlyList<TutorialStepContent> Steps => steps;

        public bool TryGetStepContent(
            TutorialStep step,
            out TutorialStepContent content)
        {
            for (int index = 0; index < steps.Count; index++)
            {
                TutorialStepContent candidate = steps[index];
                if (candidate != null && candidate.Step == step)
                {
                    content = candidate;
                    return true;
                }
            }

            content = null;
            return false;
        }

        public int GetMessageCount(TutorialStep step)
        {
            return TryGetStepContent(step, out TutorialStepContent content)
                ? content.Messages.Count
                : 0;
        }

        public int ClampDialogueIndex(TutorialStep step, int dialogueIndex)
        {
            int messageCount = GetMessageCount(step);
            if (messageCount <= 0)
                return 0;

            return Mathf.Clamp(dialogueIndex, 0, messageCount - 1);
        }

        public bool TryGetMessage(
            TutorialStep step,
            int dialogueIndex,
            out string message)
        {
            if (!TryGetStepContent(step, out TutorialStepContent content) ||
                content.Messages.Count == 0)
            {
                message = string.Empty;
                return false;
            }

            int clampedIndex = ClampDialogueIndex(step, dialogueIndex);
            message = content.Messages[clampedIndex] ?? string.Empty;
            return !string.IsNullOrWhiteSpace(message);
        }
    }
}
