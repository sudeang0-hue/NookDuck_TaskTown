using System;
using System.Collections.Generic;
using UnityEngine;

namespace TaskTown.SceneFlow
{
    /// <summary>
    /// 로딩처럼 게임 입력을 잠시 막아야 하는 시스템들이 공유하는 입력 게이트입니다.
    /// 소유자 키별로 차단 상태를 보관하므로 한 시스템이 다른 시스템의 차단을 해제하지 않습니다.
    /// </summary>
    public static class GameInputGate
    {
        private static readonly HashSet<string> Blockers = new(StringComparer.Ordinal);

        public static bool IsInputAllowed => Blockers.Count == 0;
        public static int BlockerCount => Blockers.Count;

        public static event Action<bool> InputAvailabilityChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Blockers.Clear();
            InputAvailabilityChanged = null;
            GameInputGateDriver.ResetStatics();
        }

        public static bool Block(string ownerKey)
        {
            if (string.IsNullOrWhiteSpace(ownerKey))
                return false;

            bool wasAllowed = IsInputAllowed;
            bool added = Blockers.Add(ownerKey);

            if (added && wasAllowed)
            {
                GameInputGateDriver.EnsureInstance();
                InputAvailabilityChanged?.Invoke(false);
            }

            return added;
        }

        public static bool Release(string ownerKey)
        {
            if (string.IsNullOrWhiteSpace(ownerKey))
                return false;

            bool removed = Blockers.Remove(ownerKey);

            if (removed && IsInputAllowed)
            {
                GameInputGateDriver.ReleaseInstance();
                InputAvailabilityChanged?.Invoke(true);
            }

            return removed;
        }

        public static bool IsBlockedBy(string ownerKey)
        {
            return !string.IsNullOrWhiteSpace(ownerKey) && Blockers.Contains(ownerKey);
        }
    }
}
