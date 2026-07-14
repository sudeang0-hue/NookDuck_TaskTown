using UnityEngine;

namespace TaskTown.SceneFlow
{
    /// <summary>
    /// Title Scene에서 준비한 GameSession을 Main Scene까지 유지합니다.
    /// </summary>
    public static class GameSessionStore
    {
        public static GameSession Current { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Current = null;
        }

        internal static void Publish(GameSession session)
        {
            Current = session;
        }
    }
}
