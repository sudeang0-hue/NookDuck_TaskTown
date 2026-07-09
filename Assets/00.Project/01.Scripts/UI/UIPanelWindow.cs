using System;
using UnityEngine;

public enum GameMenuState
{
    Compact,
    ExpandedTown,
    Gacha,
    AnimalDex,
    ToolPlacement,
    VillageUpgrade,
    MiniGame
}

namespace KAY
{

    public class UIPanelWindow : MonoBehaviour
    {

        [SerializeField] private RectTransform panelRect;
        private Vector2 defaultUIPanelPosition; // UI 패널의 초기 위치

        private bool isInitialized;


        private void Awake()
        {
            Initialize();

        }

        private void Initialize()
        {
            if (isInitialized)
                return;

            if (panelRect == null)
                panelRect = GetComponent<RectTransform>();

            if (panelRect == null)
            {
                Debug.LogWarning($"[UIPanelWindow] RectTransform이 없습니다 : {name}");
                return;
            }

            defaultUIPanelPosition = panelRect.anchoredPosition;
            isInitialized = true;
        }


        /// <summary>
        /// 초기 위치에서 패널 활성화
        /// </summary>
        public void OpenPanelDefaultPosition()
        {
            Initialize();

            if (panelRect != null)
                panelRect.anchoredPosition = defaultUIPanelPosition;

            gameObject.SetActive(true);
        }

        /// <summary>
        /// 이동한 위치에서 패널 활성화
        /// </summary>
        public void OpenPanelSetPosition()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 패널 닫기
        /// </summary>
        public void ClosePanel()
        {
            gameObject.SetActive(false);
        }


        public void TogglePanelDefaultPosition()
        {
            if (gameObject.activeSelf)
                ClosePanel();
            else
                OpenPanelDefaultPosition();
        }


        public void TogglePanelSetPosition()
        {
            if (gameObject.activeSelf)
                ClosePanel();
            else
                OpenPanelSetPosition();
        }

    }
}
