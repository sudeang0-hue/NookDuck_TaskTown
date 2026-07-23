using System;
using TaskTown.KDH;
using TMPro;
using UnityEngine;

namespace UI
{
    // 오프라인 보상이 지급되면 잠깐 화면에 띄워서 얼마를 벌었는지 보여주는 팝업입니다.
    // OfflineRewardManager.OnOfflineRewardGranted 이벤트를 구독해서 텍스트를 채웁니다.
    public class UIController_OfflineRewardPopup : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text messageText;

        [Tooltip("팝업이 자동으로 닫히기까지 걸리는 시간(초)")]
        [SerializeField] private float autoHideSeconds = 5f;

        private float hideTimer;
        private bool isCountingDown;

        private void OnEnable()
        {
            // 플레이 중(디버그 시뮬레이션 등) 지급되는 보상을 받기 위해 구독합니다.
            if (OfflineRewardManager.Instance != null)
                OfflineRewardManager.Instance.OnOfflineRewardGranted += HandleOfflineRewardGranted;

            if (panel != null)
                panel.SetActive(false);
        }

        private void Start()
        {
            // 게임 시작 시 지급되는 오프라인 보상은 이 팝업이 이벤트를 구독하기 전에(SaveManager가
            // 아주 이른 시점에) 지급될 수 있습니다. 그런 경우를 위해 대기 중인 보상이 있으면 표시합니다.
            if (OfflineRewardManager.Instance != null && OfflineRewardManager.Instance.HasPendingReward)
            {
                HandleOfflineRewardGranted(
                    OfflineRewardManager.Instance.PendingRewardCoins,
                    OfflineRewardManager.Instance.PendingRewardDuration);
                OfflineRewardManager.Instance.ConsumePendingReward();
            }
        }

        private void OnDisable()
        {
            if (OfflineRewardManager.Instance != null)
                OfflineRewardManager.Instance.OnOfflineRewardGranted -= HandleOfflineRewardGranted;
        }

        private void Update()
        {
            if (!isCountingDown)
                return;

            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0f)
                ClosePopup();
        }

        private void HandleOfflineRewardGranted(long reward, TimeSpan offlineDuration)
        {
            if (messageText != null)
            {
                messageText.text = $"Welcome back!\nYou were away for {offlineDuration.TotalHours:0.#}h\n+{reward:N0} coins";
            }

            if (panel != null)
                panel.SetActive(true);

            hideTimer = autoHideSeconds;
            isCountingDown = true;
        }

        // 닫기 버튼에서 호출합니다.
        public void ClosePopup()
        {
            isCountingDown = false;

            if (panel != null)
                panel.SetActive(false);
        }
    }
}
