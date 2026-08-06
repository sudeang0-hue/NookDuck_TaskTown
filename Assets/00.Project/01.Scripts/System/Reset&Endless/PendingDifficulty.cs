using TaskTown.Gacha;

namespace TaskTown
{
    //-----------------26.08.05 KDH-------------------------
    /// <summary>
    /// DifficultySelect → Main 사이 난이도 전달용.
    /// Main의 RealProductionTicker가 Start에서 소비합니다.
    /// </summary>
    public static class PendingDifficulty
    {
        public static bool HasPending { get; private set; }
        public static DifficultyType Value { get; private set; } = DifficultyType.Normal;

        public static void Set(DifficultyType difficulty)
        {
            Value = difficulty;
            HasPending = true;
        }

        public static bool TryConsume(out DifficultyType difficulty)
        {
            if (!HasPending)
            {
                difficulty = DifficultyType.Normal;
                return false;
            }

            difficulty = Value;
            HasPending = false;
            return true;
        }

        [UnityEngine.RuntimeInitializeOnLoadMethod(
            UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            HasPending = false;
            Value = DifficultyType.Normal;
        }
    }
    //----------------------------------------
}
