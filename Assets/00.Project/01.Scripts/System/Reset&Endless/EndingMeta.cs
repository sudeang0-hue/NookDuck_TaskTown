using System;
using TaskTown.Gacha;
using TaskTown.KDH;
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
            //-----------------26.08.07 KAY '클리어 난이도 기록'-------------------------
            RecordClearedDifficultyFromCurrent();
            //----------------------------------------
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

        //-----------------26.08.07 KAY '클리어 난이도 기록'-------------------------
        private const string ClearedDifficultyKey = "EndingMeta_LastClearedDifficulty";

        /// <summary>
        /// 가장 최근에 클리어한 난이도가 Hard일 때만 VeryHard 버튼을 해금합니다.
        /// </summary>
        public static bool IsVeryHardUnlocked
        {
            get
            {
                if (!PlayerPrefs.HasKey(ClearedDifficultyKey))
                    return false;

                int stored = PlayerPrefs.GetInt(ClearedDifficultyKey, -1);
                return Enum.IsDefined(typeof(DifficultyType), stored)
                    && stored == (int)DifficultyType.Hard;
            }
        }

        /// <summary>
        /// 현재 플레이 난이도를 '최근 클리어 난이도'로 덮어씁니다.
        /// </summary>
        private static void RecordClearedDifficultyFromCurrent()
        {
            DifficultyType cleared = DifficultyType.Normal;

            if (RealProductionTicker.Instance != null)
            {
                cleared = RealProductionTicker.Instance.CurrentDifficulty;
            }
            else if (PlayerPrefs.HasKey(RealProductionTicker.DifficultyPrefsKey))
            {
                int stored = PlayerPrefs.GetInt(RealProductionTicker.DifficultyPrefsKey, (int)DifficultyType.Normal);
                if (Enum.IsDefined(typeof(DifficultyType), stored))
                    cleared = (DifficultyType)stored;
            }

            PlayerPrefs.SetInt(ClearedDifficultyKey, (int)cleared);
            PlayerPrefs.Save();
        }
        //----------------------------------------
    }
    //----------------------------------------
}
