using UnityEngine;

namespace TaskTown.Tutorial
{
    /// <summary>
    /// 튜토리얼 말풍선의 클릭음과 퀘스트 완료음 재생 규칙을 담당합니다.
    /// 실제 재생은 Bootstrap에서 유지되는 SoundManager에 위임합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TutorialBubbleSfxPlayer : MonoBehaviour
    {
        [Header("Dialogue")]
        [SerializeField] private string dialogueAdvanceSoundId =
            "SFX_Pop_Bubble_Single_1";

        [Header("Skip Buttons")]
        [SerializeField] private string skipOpenSoundId =
            "SFX_Pop_Bubble_Single_1";
        [SerializeField] private string skipConfirmSoundId = "UI_Click";
        [SerializeField] private string skipCancelSoundId = "UI_Close";

        [Header("Quest Clear Playlist")]
        [SerializeField] private string[] questClearSoundIds =
        {
            "SFX_Player_Collect_Pop_1",
            "SFX_Player_Collect_Pop_2",
            "SFX_Player_Collect_Pop_3"
        };

        [Tooltip("완료음을 재생한 뒤 다음 튜토리얼 문구를 표시하기까지의 시간입니다.")]
        [SerializeField, Min(0f)] private float questClearPresentationDelay = 0.5f;

        private bool warnedMissingSoundManager;

        public float QuestClearPresentationDelay => questClearPresentationDelay;

        public bool PlayDialogueAdvance()
        {
            return PlayUI(dialogueAdvanceSoundId);
        }

        public bool PlaySkipOpen()
        {
            return PlayUI(skipOpenSoundId);
        }

        public bool PlaySkipConfirm()
        {
            return PlayUI(skipConfirmSoundId);
        }

        public bool PlaySkipCancel()
        {
            return PlayUI(skipCancelSoundId);
        }

        /// <summary>
        /// 완료한 퀘스트 순서를 기준으로 1 → 2 → 3 → 1 순환 재생합니다.
        /// 재생에 성공하면 다음 문구 표시를 늦출 시간을 반환합니다.
        /// </summary>
        public float PlayQuestClear(TutorialStep completedStep)
        {
            string soundId = GetQuestClearSoundId(completedStep);
            if (string.IsNullOrWhiteSpace(soundId) || !PlayUI(soundId))
                return 0f;

            return questClearPresentationDelay;
        }

        public string GetQuestClearSoundId(TutorialStep completedStep)
        {
            int questIndex = GetQuestIndex(completedStep);
            if (questIndex < 0 || questClearSoundIds == null ||
                questClearSoundIds.Length == 0)
            {
                return string.Empty;
            }

            int playlistIndex = questIndex % questClearSoundIds.Length;
            return questClearSoundIds[playlistIndex] ?? string.Empty;
        }

        private bool PlayUI(string soundId)
        {
            if (string.IsNullOrWhiteSpace(soundId))
                return false;

            SoundManager manager = SoundManager.Instance;
            if (manager == null)
            {
                if (!warnedMissingSoundManager)
                {
                    Debug.LogWarning(
                        "[TutorialBubbleSfxPlayer] SoundManager가 없어 " +
                        "튜토리얼 효과음을 재생하지 못했습니다.",
                        this);
                    warnedMissingSoundManager = true;
                }

                return false;
            }

            warnedMissingSoundManager = false;
            return manager.PlayUI(soundId);
        }

        private static int GetQuestIndex(TutorialStep step)
        {
            return step switch
            {
                TutorialStep.EarnManualCoin => 0,
                TutorialStep.CollapseAndExpandTown => 1,
                TutorialStep.DrawAnimal => 2,
                TutorialStep.DrawTool => 3,
                TutorialStep.AssignAnimal => 4,
                TutorialStep.ConfirmAutoProduction => 5,
                TutorialStep.OpenVillageInfo => 6,
                TutorialStep.UpgradeVillage => 7,
                _ => -1
            };
        }
    }
}
