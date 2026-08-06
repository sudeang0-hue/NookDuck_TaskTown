using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// 마을 Lv.10 완주 시 선택지.
    /// A: 마을 리셋 → TeamLogo
    /// B: 엔드리스로 계속
    /// </summary>
    public class VillageCompletionPopup : MonoBehaviour
    {
        [SerializeField] private Button resetButton;      // 처음부터 다시
        [SerializeField] private Button endlessButton;    // 계속하기
        [SerializeField] private UIPanelWindow panelWindow;

        private Action onSelectReset;
        private Action onSelectEndless;
        private bool isSelecting;

        private void Awake()
        {
            if (resetButton != null) resetButton.onClick.AddListener(SelectReset);

            if (endlessButton != null) endlessButton.onClick.AddListener(SelectEndless);
        }

        private void OnDestroy()
        {
            if (resetButton != null) resetButton.onClick.RemoveListener(SelectReset);

            if (endlessButton != null) endlessButton.onClick.RemoveListener(SelectEndless);
        }

        public void Open(Action resetAction, Action endlessAction)
        {
            if (resetAction == null || endlessAction == null)
            {
                Debug.LogWarning("[VillageCompletionPopup] 콜백이 비어 있습니다.");
                return;
            }

            onSelectReset = resetAction;
            onSelectEndless = endlessAction;
            isSelecting = false;

            if (panelWindow != null)
            {
                panelWindow.OpenPanelDefaultPosition();
            }
            else
            {
                gameObject.SetActive(true);
            }
        }

        private void SelectReset()
        {
            if (isSelecting) return;

            isSelecting = true;
            Action action = onSelectReset;
            Close();
            action?.Invoke();
        }

        private void SelectEndless()
        {
            if (isSelecting) return;

            isSelecting = true;
            Action action = onSelectEndless;
            Close();
            action?.Invoke();
        }

        private void Close()
        {
            onSelectReset = null;
            onSelectEndless = null;

            if (panelWindow != null)
            {
                panelWindow.ClosePanel();
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
