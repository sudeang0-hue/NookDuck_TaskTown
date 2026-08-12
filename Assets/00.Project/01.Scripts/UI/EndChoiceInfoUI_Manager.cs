using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// VillageCompletionPopup reset/endless button hover tooltip.
    /// Activates this object, updates message text, and follows the mouse inside the button rect.
    /// Prefab: keep this GameObject initially Active so Awake can bind (it hides itself at end of Awake).
    /// </summary>
    public class EndChoiceInfoUI_Manager : MonoBehaviour
    {
        [SerializeField] private Button resetButton;
        [SerializeField] private Button endlessButton;
        [SerializeField] private TMP_Text choiceInfo;

        [SerializeField, TextArea(2, 4)]
        private string resetMessage =
            "마을을 초기화하고 새로운 난이도를 해금합니다.\n다른 난이도에서는 특별한 주민을 만날 수 있습니다.";

        [SerializeField, TextArea(2, 4)]
        private string endlessMessage =
            "현재 마을에 머무르며 계속 성장합니다.\n설정창에서 언제든 새로운 마을로 넘어갈 수 있습니다.";

        [Tooltip("Mouse follow offset in parent local space")]
        [SerializeField] private Vector2 followOffset = new Vector2(20f, -20f);

        private RectTransform rectTransform;
        private Canvas rootCanvas;
        private RectTransform followBounds;
        private bool isFollowing;
        private bool bindingsReady;

        private void Awake()
        {
            rectTransform = transform as RectTransform;
            rootCanvas = GetComponentInParent<Canvas>();

            // Prevent tooltip from stealing raycasts (avoids hover flicker)
            if (choiceInfo != null)
                choiceInfo.raycastTarget = false;

            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            EnsureBindings();
            HideInfo();
        }

        private void OnDestroy()
        {
            RemoveHoverRelay(resetButton);
            RemoveHoverRelay(endlessButton);
        }

        private void EnsureBindings()
        {
            if (bindingsReady)
                return;

            BindHover(resetButton, resetMessage);
            BindHover(endlessButton, endlessMessage);
            bindingsReady = true;
        }

        private void BindHover(Button button, string message)
        {
            if (button == null)
                return;

            HoverRelay relay = button.GetComponent<HoverRelay>();
            if (relay == null)
                relay = button.gameObject.AddComponent<HoverRelay>();

            relay.Initialize(this, message);
        }

        private static void RemoveHoverRelay(Button button)
        {
            if (button == null)
                return;

            HoverRelay relay = button.GetComponent<HoverRelay>();
            if (relay != null)
                Destroy(relay);
        }

        private void ShowInfo(string message, RectTransform buttonRect, PointerEventData eventData)
        {
            if (choiceInfo != null)
                choiceInfo.text = message ?? string.Empty;

            followBounds = buttonRect;
            isFollowing = true;

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            FollowPointer(eventData);
        }

        private void HideInfo()
        {
            isFollowing = false;
            followBounds = null;

            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        private void FollowPointer(PointerEventData eventData)
        {
            if (!isFollowing || eventData == null || rectTransform == null || followBounds == null)
                return;

            Camera uiCamera = GetUiCamera();
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    followBounds,
                    eventData.position,
                    uiCamera,
                    out Vector2 localInButton))
                return;

            // Clamp tracking to button rect
            Rect buttonRect = followBounds.rect;
            localInButton.x = Mathf.Clamp(localInButton.x, buttonRect.xMin, buttonRect.xMax);
            localInButton.y = Mathf.Clamp(localInButton.y, buttonRect.yMin, buttonRect.yMax);

            Vector3 world = followBounds.TransformPoint(localInButton);
            RectTransform parent = rectTransform.parent as RectTransform;
            if (parent != null)
            {
                Vector2 localInParent = parent.InverseTransformPoint(world);
                rectTransform.anchoredPosition = localInParent + followOffset;
            }
            else
            {
                rectTransform.position = world;
            }
        }

        private Camera GetUiCamera()
        {
            if (rootCanvas == null)
                rootCanvas = GetComponentInParent<Canvas>();

            if (rootCanvas == null || rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;

            return rootCanvas.worldCamera;
        }

        /// <summary>
        /// Relays pointer enter/exit/move from a button to EndChoiceInfoUI_Manager.
        /// </summary>
        private sealed class HoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
        {
            private EndChoiceInfoUI_Manager owner;
            private string message;
            private RectTransform buttonRect;

            public void Initialize(EndChoiceInfoUI_Manager infoOwner, string infoMessage)
            {
                owner = infoOwner;
                message = infoMessage;
                buttonRect = transform as RectTransform;
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                if (owner == null)
                    return;

                owner.ShowInfo(message, buttonRect, eventData);
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                if (owner == null)
                    return;

                owner.HideInfo();
            }

            public void OnPointerMove(PointerEventData eventData)
            {
                if (owner == null)
                    return;

                owner.FollowPointer(eventData);
            }
        }
    }
}
