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
        [SerializeField] private Button normalButton;
        [SerializeField] private Button hardButton;
        [SerializeField] private Button veryHardButton;

        [Header("Flow")]
        [SerializeField] private SceneId nextScene = SceneId.Main;

        private bool isSelecting;

        private void Awake()
        {
            if (normalButton != null)
                normalButton.onClick.AddListener(() => Select(DifficultyType.Normal));
            if (hardButton != null)
                hardButton.onClick.AddListener(() => Select(DifficultyType.Hard));
            if (veryHardButton != null)
                veryHardButton.onClick.AddListener(() => Select(DifficultyType.VeryHard));

            //-----------------26.08.07 KAY '난이도 버튼 초기 상태'-------------------------
            ApplyDifficultyButtonInitialState();
            //----------------------------------------
        }

        private void OnDestroy()
        {
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

        //-----------------26.08.07 KAY '난이도 버튼 초기 상태'-------------------------
        /// <summary>
        /// 인트로 전까지 버튼을 모두 끕니다.
        /// Easy는 미사용, VeryHard는 최근 클리어 난이도가 Hard일 때만 인트로에서 켜집니다.
        /// </summary>
        private void ApplyDifficultyButtonInitialState()
        {
            //// Easy: 삭제 예정 — 항상 비활성
            //if (easyButton != null)
            //{
            //    easyButton.gameObject.SetActive(false);
            //    easyButton.interactable = false;
            //}

            // Normal / Hard: 인트로에서 동시 오픈 (여기선 끔)
            if (normalButton != null)
                normalButton.gameObject.SetActive(false);

            if (hardButton != null)
                hardButton.gameObject.SetActive(false);

            // VeryHard: 인트로 전 끔. 클릭 가능 여부만 해금 상태로 맞춤
            if (veryHardButton != null)
            {
                veryHardButton.gameObject.SetActive(false);
                veryHardButton.interactable = EndingMeta.IsVeryHardUnlocked;
            }
        }
        //----------------------------------------
    }
    //----------------------------------------
}
