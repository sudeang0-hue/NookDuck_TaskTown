using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// 메뉴에서 마을 리셋 확정 시 확인창.
    /// Yes: 진행도 초기화 → TeamLogo / No: 닫기
    /// </summary>
    public class VillageResetConfirmPopup : MonoBehaviour
    {
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private UIPanelWindow panelWindow;

        private Action onSelectConfirm;
        private Action onSelectCancel;
        private bool isSelecting;

        private void Awake()
        {
            if (confirmButton != null)
                confirmButton.onClick.AddListener(SelectConfirm);

            if (cancelButton != null)
                cancelButton.onClick.AddListener(SelectCancel);
        }

        private void OnDestroy()
        {
            if (confirmButton != null)
                confirmButton.onClick.RemoveListener(SelectConfirm);

            if (cancelButton != null)
                cancelButton.onClick.RemoveListener(SelectCancel);
        }

        public void Open(Action confirmAction, Action cancelAction = null)
        {
            if (confirmAction == null)
            {
                Debug.LogWarning("[VillageResetConfirmPopup] 확인 콜백이 비어 있습니다.");
                return;
            }

            onSelectConfirm = confirmAction;
            onSelectCancel = cancelAction;
            isSelecting = false;

            // UIPanelWindow로 열어 레이어·트윈을 메뉴 패널과 맞춤
            if (panelWindow != null)
                panelWindow.OpenPanelDefaultPosition();
            else
                gameObject.SetActive(true);
        }

        private void SelectConfirm()
        {
            if (isSelecting)
                return;

            isSelecting = true;
            Action action = onSelectConfirm;
            Close();
            action?.Invoke();
        }

        private void SelectCancel()
        {
            if (isSelecting)
                return;

            isSelecting = true;
            Action action = onSelectCancel;
            Close();
            action?.Invoke();
        }

        private void Close()
        {
            onSelectConfirm = null;
            onSelectCancel = null;

            if (panelWindow != null)
                panelWindow.ClosePanel();
            else
                gameObject.SetActive(false);
        }
    }
}
