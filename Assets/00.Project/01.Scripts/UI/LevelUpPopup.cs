using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{

    public class LevelUpPopup : MonoBehaviour
    {

        [SerializeField] private TMP_Text messageText;  // 출력할 텍스트
        [SerializeField] private TMP_Text expendCoinText;  // 소모되는 코인이 포함된 텍스트

        [SerializeField] private Button yesButton;
        [SerializeField] private Button noButton;

        private Action onSelectYesToConfirm; // Yes 를 선택했을때
        private Action onSelectNoToCancel;   // No 를 선택했을때
        private bool isSelecting;

        private void Awake()
        {
            if (yesButton != null) yesButton.onClick.AddListener(SelectYes);
            if (noButton != null) noButton.onClick.AddListener(SelectNo);

        }

        private void OnDestroy()
        {
            if (yesButton != null) yesButton.onClick.RemoveListener(SelectYes);

            if (noButton != null) noButton.onClick.RemoveListener(SelectNo);
        }

        /// <summary>
        /// 레벨업 팝업 오픈
        /// </summary>
        public void OpenLevelUpPopup(string message,string coinMessage, Action confirmAction, Action cancelAction = null)
        {
            if (confirmAction == null)
            {
                Debug.LogWarning("[LevelUpPopup] 확인 후 실행할 기능이 없습니다.");
                return;
            }

            if (messageText == null || expendCoinText == null)
            {
                Debug.LogWarning("[LevelUpPopup] 출력할 텍스트가 연결되지 않았습니다.");
                return;
            }

            messageText.text = message;
            expendCoinText.text = coinMessage;
            onSelectYesToConfirm = confirmAction;
            onSelectNoToCancel = cancelAction;
            isSelecting = false;

            gameObject.SetActive(true);
        }

        /// <summary>
        /// Yes 를 선택했을때
        /// </summary>
        private void SelectYes()
        {
            if (isSelecting)
                return;

            isSelecting = true;

            Action confirmAction = onSelectYesToConfirm;

            Close();
            confirmAction?.Invoke();
        }

        /// <summary>
        /// No 를 선택했을때
        /// </summary>
        private void SelectNo()
        {
            if (isSelecting)
                return;

            isSelecting = true;

            Action cancelAction = onSelectNoToCancel;

            Close();
            cancelAction?.Invoke();
        }

        /// <summary>
        /// 창 닫기
        /// </summary>
        private void Close()
        {
            onSelectYesToConfirm = null;
            onSelectNoToCancel = null;

            gameObject.SetActive(false);
        }

    }
}