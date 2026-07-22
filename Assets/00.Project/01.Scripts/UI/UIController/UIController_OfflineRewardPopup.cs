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

        private void Start()
        {
            // Awake() 실행 순서는 GameObject마다 달라질 수 있어서, 모든 Awake가 끝난 뒤 호출되는
            // Start()에서 구독합니다(OfflineRewardManager.Instance가 확실히 준비된 시점).
            if (OfflineRewardManager.Instance != null)
                OfflineRewardManager.Instance.OnOfflineRewardGranted += HandleOfflineRewardGranted;

            if (panel != null)
                panel.SetActive(false);
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
