using System;
using System.Collections.Generic;
using UnityEngine;

namespace TaskTown.Tutorial
{
    public enum TutorialProgressDisplayType
    {
        None = 0,
        ManualCoin = 1,
        AutoProductionCoin = 2
    }

    public enum TutorialSpeakerSide
    {
        Left = 0,
        Right = 1
    }

    [Flags]
    public enum TutorialHighlightEffect
    {
        None = 0,
        ScalePulse = 1 << 0,
        Pointer = 1 << 1,
        FocusRing = 1 << 2
    }

    public enum TutorialPointerPositionMode
    {
        FollowHighlightedButton = 0,
        CanvasPosition = 1
    }

    [Serializable]
    public sealed class TutorialStepContent
    {
        [SerializeField] private TutorialStep step;
        [SerializeField, TextArea(2, 6)] private List<string> messages = new();
        [SerializeField, TextArea(2, 6)] private List<string> completionMessages = new();
        [SerializeField, TextArea(2, 4)] private string objectiveText;
        [SerializeField] private TutorialProgressDisplayType progressDisplayType;
        [SerializeField] private string progressFormat = "{0} / {1}";
        [SerializeField] private bool allowClickAdvance;
        [SerializeField] private TutorialSpeakerSide speakerSide =
            TutorialSpeakerSide.Left;
        [SerializeField] private Vector2 layoutOffset;
        [SerializeField] private TutorialHighlightEffect highlightEffects =
            TutorialHighlightEffect.None;
        [SerializeField] private TutorialPointerPositionMode pointerPositionMode =
            TutorialPointerPositionMode.FollowHighlightedButton;
        [SerializeField] private Vector2 pointerOffset;
        [SerializeField] private Vector2 pointerCanvasPosition;
        [SerializeField, Min(16f)] private float pointerRingSize = 120f;
        [SerializeField, Min(0f)] private float pointerRingPadding = 24f;

        public TutorialStep Step => step;
        public IReadOnlyList<string> Messages => messages;
        public IReadOnlyList<string> CompletionMessages => completionMessages;
        public string ObjectiveText => objectiveText;
        public TutorialProgressDisplayType ProgressDisplayType => progressDisplayType;
        public string ProgressFormat => progressFormat;
        public bool AllowClickAdvance => allowClickAdvance;
        public TutorialSpeakerSide SpeakerSide => speakerSide;
        public Vector2 LayoutOffset => layoutOffset;
        public TutorialHighlightEffect HighlightEffects => highlightEffects;
        public TutorialPointerPositionMode PointerPositionMode => pointerPositionMode;
        public Vector2 PointerOffset => pointerOffset;
        public Vector2 PointerCanvasPosition => pointerCanvasPosition;
        public float PointerRingSize => pointerRingSize;
        public float PointerRingPadding => pointerRingPadding;

        public bool UsesHighlightEffect(TutorialHighlightEffect effect)
        {
            return (highlightEffects & effect) != 0;
        }
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

        public int GetCompletionMessageCount(TutorialStep step)
        {
            return TryGetStepContent(step, out TutorialStepContent content)
                ? content.CompletionMessages.Count
                : 0;
        }

        public int ClampDialogueIndex(TutorialStep step, int dialogueIndex)
        {
            int messageCount = GetMessageCount(step);
            if (messageCount <= 0)
                return 0;

            return Mathf.Clamp(dialogueIndex, 0, messageCount - 1);
        }

        public int ClampCompletionDialogueIndex(
            TutorialStep step,
            int dialogueIndex)
        {
            int messageCount = GetCompletionMessageCount(step);
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

        public bool TryGetCompletionMessage(
            TutorialStep step,
            int dialogueIndex,
            out string message)
        {
            if (!TryGetStepContent(step, out TutorialStepContent content) ||
                content.CompletionMessages.Count == 0)
            {
                message = string.Empty;
                return false;
            }

            int clampedIndex = ClampCompletionDialogueIndex(step, dialogueIndex);
            message = content.CompletionMessages[clampedIndex] ?? string.Empty;
            return !string.IsNullOrWhiteSpace(message);
        }
    }
}
