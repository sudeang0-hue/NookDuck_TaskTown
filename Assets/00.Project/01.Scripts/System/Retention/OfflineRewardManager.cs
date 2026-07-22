using System;
using TaskTown.Gacha;
using UnityEngine;

namespace TaskTown.KDH
{
    // 앱이 백그라운드/종료 상태였던 시간을 계산해서 오프라인 보상을 지급합니다.
    // 마지막으로 활성 상태였던 시각을 PlayerPrefs에 저장해뒀다가, 다음 실행/복귀 시
    // 경과 시간을 계산합니다. 실제 계산식은 OfflineRewardCalculator 참고.
    public class OfflineRewardManager : MonoBehaviour
    {
        public static OfflineRewardManager Instance { get; private set; }

        private const string LastActiveTicksKey = "OfflineReward_LastActiveTicksUtc";

        // 강제 종료/정전 등 OnApplicationQuit이 못 불리는 비정상 종료에 대비해, 이 주기로도
        // 마지막 활성 시각을 저장합니다(최악의 경우에도 이 주기만큼만 오차가 생김).
        private const float AutoSaveIntervalSeconds = 60f;

        [Tooltip("ICoinWallet을 구현한 컴포넌트(CoinManager)를 연결합니다.")]
        [SerializeField] private MonoBehaviour coinWalletSource;

        private ICoinWallet CoinWallet => coinWalletSource as ICoinWallet;

        // 오프라인 보상이 지급될 때(코인, 인정된 오프라인 시간) 호출됩니다. UI(환영 팝업 등)가 구독합니다.
        public event Action<long, TimeSpan> OnOfflineRewardGranted;

        // 시작 시점의 보상은 UI가 이벤트를 구독하기 전에 지급될 수 있으므로, 대기 상태로도 보관합니다.
        // UI는 준비된 시점에 HasPendingReward를 확인해서 표시한 뒤 ConsumePendingReward()로 비웁니다.
        public bool HasPendingReward { get; private set; }
        public long PendingRewardCoins { get; private set; }
        public TimeSpan PendingRewardDuration { get; private set; }

        public void ConsumePendingReward()
        {
            HasPendingReward = false;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            InvokeRepeating(nameof(SaveLastActiveTime), AutoSaveIntervalSeconds, AutoSaveIntervalSeconds);

            // 오프라인 보상은 "저장 시점의 생산량"으로 계산해야 하므로(실시간 생산량은 로드 타이밍에
            // 따라 0으로 잡힐 수 있음), 세이브 로드를 끝낸 SaveManager가 저장돼 있던 생산량을 넘겨
            // CheckOfflineReward(rate)를 호출합니다(SaveManager.Start 참고). 이 매니저 스스로는 계산하지
            // 않습니다(중복/조기 실행 방지). SaveManager 없이 쓰는 씬이라면 외부에서 CheckOfflineReward를
            // 직접 호출해야 합니다.
        }

        // SaveManager가 세이브 로드를 끝낸 뒤, 저장돼 있던 초당 생산량을 넘겨 호출합니다.
        public void CheckOfflineReward(float productionRatePerSecond)
        {
            GrantOfflineRewardIfDue(productionRatePerSecond);
        }

        private void OnDestroy()
        {
            CancelInvoke(nameof(SaveLastActiveTime));
        }

        private void OnApplicationPause(bool isPaused)
        {
            // PC 타겟이라 종료는 OnApplicationQuit이 담당합니다. 여기서 복귀 시 보상을 지급하면
            // 에디터 포커스 전환 등에서 생산량 0으로 오발동해 오프라인 시간을 소모시키므로,
            // 일시정지 시 마지막 접속 시각 저장만 합니다(모바일 대응이 필요해지면 별도 처리).
            if (isPaused)
                SaveLastActiveTime();
        }

        private void OnApplicationQuit()
        {
            SaveLastActiveTime();
        }

        private void GrantOfflineRewardIfDue(float productionRatePerSecond)
        {
            if (!PlayerPrefs.HasKey(LastActiveTicksKey))
            {
                SaveLastActiveTime();
                return;
            }

            long savedTicks = long.Parse(PlayerPrefs.GetString(LastActiveTicksKey));
            DateTime lastActive = new DateTime(savedTicks, DateTimeKind.Utc);
            TimeSpan offlineDuration = DateTime.UtcNow - lastActive;

            if (CoinWallet != null)
            {
                long reward = OfflineRewardCalculator.CalculateRewardCoins(productionRatePerSecond, offlineDuration);

                if (reward > 0)
                {
                    CoinWallet.Add(reward);

                    HasPendingReward = true;
                    PendingRewardCoins = reward;
                    PendingRewardDuration = offlineDuration;

                    OnOfflineRewardGranted?.Invoke(reward, offlineDuration);
                }
            }

            SaveLastActiveTime();
        }

        private void SaveLastActiveTime()
        {
            PlayerPrefs.SetString(LastActiveTicksKey, DateTime.UtcNow.Ticks.ToString());
            PlayerPrefs.Save();
        }

        // 디버그/QA용: 실제로 몇 시간을 기다리지 않고 오프라인 보상을 즉시 확인할 수 있게 합니다.
        // 게임이 실행 중이므로 실시간 생산량을 사용합니다.
        public void SimulateOfflineElapsed(float hours)
        {
            DateTime fakeLastActive = DateTime.UtcNow.AddHours(-hours);
            PlayerPrefs.SetString(LastActiveTicksKey, fakeLastActive.Ticks.ToString());

            float liveRate = RealProductionTicker.Instance != null
                ? RealProductionTicker.Instance.CalculateTotalCoinPerSecond()
                : 0f;
            GrantOfflineRewardIfDue(liveRate);
        }
    }
}
