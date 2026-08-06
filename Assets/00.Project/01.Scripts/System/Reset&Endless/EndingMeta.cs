using UnityEngine;

namespace TaskTown
{
    //-----------------26.08.05 KDH-------------------------
    public static class EndingMeta
    {
        private const string ClearedKey = "EndingMeta_HasClearedEnding";
        private const string NeedsSelectKey = "EndingMeta_NeedsDifficultySelect";

        public static bool HasClearedEnding
        {
            get => PlayerPrefs.GetInt(ClearedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(ClearedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>리셋 직후 1회만 난이도 선택 씬으로 보냅니다.</summary>
        public static bool NeedsDifficultySelect
        {
            get => PlayerPrefs.GetInt(NeedsSelectKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(NeedsSelectKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// 리셋 직후 다음 Main 인벤이 빈 상태로 시작해야 함을 표시합니다.
        /// (세이브 재생성/DDOL 잔존으로 옛 슬롯이 되살아나는 것을 막습니다.)
        /// </summary>
        public static bool ForceEmptyInventoryOnNextMain { get; set; }

        public static void MarkCleared()
        {
            HasClearedEnding = true;
        }

        public static void RequestDifficultySelect()
        {
            NeedsDifficultySelect = true;
        }

        public static void ClearDifficultySelectRequest()
        {
            NeedsDifficultySelect = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ForceEmptyInventoryOnNextMain = false;
        }
    }
    //----------------------------------------
}
