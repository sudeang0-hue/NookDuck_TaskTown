using TaskTown.Gacha;
using UnityEngine;
using UnityEngine.UI;

namespace TaskTown.SceneFlow
{
    //-----------------26.08.05 KDH-------------------------
    /// <summary>
    /// 엔딩 후 리셋 플로우에서만 진입하는 난이도 선택 UI.
    /// 선택 후 Main Scene으로 이동합니다.
    /// </summary>
    public sealed class DifficultySelectController : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button easyButton;
        [SerializeField] private Button normalButton;
        [SerializeField] private Button hardButton;
        [SerializeField] private Button veryHardButton;

        [Header("Flow")]
        [SerializeField] private SceneId nextScene = SceneId.Main;

        private bool isSelecting;

        private void Awake()
        {
            if (easyButton != null)
                easyButton.onClick.AddListener(() => Select(DifficultyType.Easy));
            if (normalButton != null)
                normalButton.onClick.AddListener(() => Select(DifficultyType.Normal));
            if (hardButton != null)
                hardButton.onClick.AddListener(() => Select(DifficultyType.Hard));
            if (veryHardButton != null)
                veryHardButton.onClick.AddListener(() => Select(DifficultyType.VeryHard));

            //-----------------26.08.07 KAY 'VeryHard 버튼 해금'-------------------------
            ApplyVeryHardButtonUnlock();
            //----------------------------------------
        }

        private void OnDestroy()
        {
            if (easyButton != null) easyButton.onClick.RemoveAllListeners();
            if (normalButton != null) normalButton.onClick.RemoveAllListeners();
            if (hardButton != null) hardButton.onClick.RemoveAllListeners();
            if (veryHardButton != null) veryHardButton.onClick.RemoveAllListeners();
        }

        private void Select(DifficultyType difficulty)
        {
            if (isSelecting) return;
            isSelecting = true;

            // Main 진입 후 RealProductionTicker가 소비
            PendingDifficulty.Set(difficulty);
            EndingMeta.ClearDifficultySelectRequest(); // 다음 실행부터 Main

            SceneFlowManager manager = SceneFlowManager.EnsureInstance();
            if (manager == null || !manager.LoadScene(nextScene))
            {
                Debug.LogError(
                    $"[DifficultySelectController] {nextScene} 로드 실패: {manager?.LastError}",
                    this);
                isSelecting = false;
            }
        }

        //-----------------26.08.07 KAY 'VeryHard 버튼 해금'-------------------------
        /// <summary>
        /// Hard 이상 엔딩 클리어 기록이 있을 때만 VeryHard 버튼을 누를 수 있게 합니다.
        /// </summary>
        private void ApplyVeryHardButtonUnlock()
        {
            if (veryHardButton == null)
                return;

            veryHardButton.interactable = EndingMeta.IsVeryHardUnlocked;
        }
        //----------------------------------------
    }
    //----------------------------------------
}
