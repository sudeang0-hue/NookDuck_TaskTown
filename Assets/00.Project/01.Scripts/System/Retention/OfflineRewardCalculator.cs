using System;

namespace TaskTown.KDH
{
    // 오프라인 보상 계산식. simulate_game.py의 offline_reward_coins()와 동일한 공식입니다.
    // 상한(12시간 고정)과 배율(2.5%)은 N=1,000 시뮬레이션으로 검증한 값입니다:
    // 상한 시간만큼 방치해도 다음 마을 레벨 비용의 5~11% 수준(레벨1만 예외적으로 31%)이라,
    // 실제 플레이를 대체하지 않는 선에서 의미 있는 보너스로 확정했습니다.
    public static class OfflineRewardCalculator
    {
        public const float RewardMultiplier = 0.025f;
        public const float CapHours = 12f;

        // 최소 이 시간 이상 떠나 있어야 보상을 지급합니다(짧은 알트탭 등에 보상이 뜨는 것을 방지).
        public const float MinOfflineSeconds = 60f;

        public static long CalculateRewardCoins(float productionCoinPerSecond, TimeSpan offlineDuration)
        {
            if (productionCoinPerSecond <= 0f || offlineDuration.TotalSeconds < MinOfflineSeconds)
                return 0L;

            double cappedSeconds = Math.Min(offlineDuration.TotalSeconds, CapHours * 3600.0);
            double reward = cappedSeconds * productionCoinPerSecond * RewardMultiplier;

            return (long)Math.Floor(reward);
        }
    }
}
