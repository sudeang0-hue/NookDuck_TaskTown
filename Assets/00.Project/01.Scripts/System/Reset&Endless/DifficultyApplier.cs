using TaskTown.Gacha;
using TaskTown.KDH;
using UnityEngine;

namespace TaskTown
{
    //-----------------26.08.05 KDH-------------------------
    /// <summary>
    /// DifficultySelect에서 넘긴 PendingDifficulty를 RealProductionTicker에 적용합니다.
    /// </summary>
    public sealed class DifficultyApplier : MonoBehaviour
    {
        [SerializeField] private RealProductionTicker productionTicker;

        private void Start()
        {
            if (!PendingDifficulty.TryConsume(out DifficultyType difficulty))
                return;

            // Instance를 우선합니다. Inspector 참조는 리셋 후 파괴된 옛 객체를 가리킬 수 있습니다.
            RealProductionTicker ticker = RealProductionTicker.Instance != null
                ? RealProductionTicker.Instance
                : productionTicker;

            if (ticker == null)
            {
                Debug.LogWarning("[DifficultyApplier] RealProductionTicker가 없습니다.");
                return;
            }

            ticker.SetDifficulty(difficulty);

            // 선택 직후 앱을 바로 종료해도 유지되도록 즉시 저장합니다.
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.EndProgressReset();
                SaveManager.Instance.SaveGame();
            }

            Debug.Log($"[DifficultyApplier] 난이도 적용: {difficulty}");
        }
    }
    //----------------------------------------
}
