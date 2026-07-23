using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// 마을 패널 출력/닫기 및 정보 연결을 담당합니다.
    /// Input System 클릭(wasPressedThisFrame) 시에만 VillageHouse LayerMask Raycast를 1회 수행합니다.
    /// </summary>
    public class VillageUI_Manager : MonoBehaviour
    {
        private const string VillageClickLayerName = "VillageHouse";

        [Header("참조")]
        [SerializeField] private Camera _camera;
        [SerializeField] private Transform targetObject;
        [SerializeField] private Transform villagePanelRoot;
        [SerializeField] private GameObject villagePanelContent; // VillageInfo_Root
        [SerializeField] private UIController_Village uiController;
        [SerializeField] private Canvas villageCanvas;

        [Header("클릭 (Input System + Raycast)")]
        [Tooltip("VillageHouse 레이어만 체크하세요.")]
        [SerializeField] private LayerMask clickLayerMask;
        [Tooltip("true면 Button 등 Selectable UI 위 클릭 시 집 Raycast를 무시합니다.")]
        [SerializeField] private bool blockWhenOverSelectableUI = true;

        [Header("패널 배치 (WorldSpace 전용)")]
        [SerializeField] private Vector3 panelOffset = new Vector3(0f, 1.5f, 0f);

        [Header("자동 닫힘")]
        [SerializeField] private float autoCloseSeconds = 3f;
        [Tooltip("true면 Village 패널 바깥을 클릭했을 때 패널을 닫습니다. (패널 위·VillageHouse 클릭은 제외)")]
        [SerializeField] private bool closeOnOutsideClick = false;

        [Header("테스트용 마을 정보 (VillageSystem 연동 전)")]
        [SerializeField] private int townLevel = 1;
        [SerializeField] private int clickCoin = 10;
        [SerializeField] private int typingCoin = 5;
        [SerializeField] private int toolCapacity = 3;
        [SerializeField] private float autoProductBonus = 1.2f;
        [SerializeField] private int requireLevelupCoin = 200;

        [Header("테스트용 다음 레벨 (LvUp Hover 미리보기)")]
        [SerializeField] private int nextTownLevel = 2;
        [SerializeField] private int nextClickCoin = 20;
        [SerializeField] private int nextTypingCoin = 10;
        [SerializeField] private int nextToolCapacity = 4;
        [SerializeField] private float nextAutoProductBonus = 1.5f;

        private bool isPanelOpen;
        private Coroutine autoCloseCoroutine;
        private readonly List<RaycastResult> _uiHits = new List<RaycastResult>(8);

        private GameObject PanelContent
        {
            get
            {
                if (villagePanelContent != null)
                    return villagePanelContent;

                return villagePanelRoot != null ? villagePanelRoot.gameObject : null;
            }
        }

        private bool IsWorldSpacePanel
        {
            get
            {
                if (villageCanvas == null)
                    return false;

                return villageCanvas.renderMode == RenderMode.WorldSpace;
            }
        }

        public bool IsPanelOpen => isPanelOpen;

        private void Awake()
        {
            if (_camera == null)
                _camera = Camera.main;

            if (villageCanvas == null && villagePanelRoot != null)
                villageCanvas = villagePanelRoot.GetComponentInParent<Canvas>();

            if (villageCanvas == null)
                villageCanvas = GetComponent<Canvas>();

            EnsureClickLayerMask();
            HidePanelContent();
            isPanelOpen = false;
        }

        private void OnEnable()
        {
            TargetSelector.OnTargetSelected += OnCameraTargetSelected;
        }

        private void OnDisable()
        {
            TargetSelector.OnTargetSelected -= OnCameraTargetSelected;
            StopAutoCloseTimer();
        }

        /// <summary>
        /// 카메라 타겟 선택(시점 전환) 시 Village 패널을 닫습니다.
        /// 빈 공간 클릭(null)은 무시합니다.
        /// </summary>
        private void OnCameraTargetSelected(Transform target)
        {
            if (target == null || !isPanelOpen)
                return;

            ClosePanel();
        }

        private void Update()
        {
            // Input System: 눌린 프레임에만 클릭 콜백 실행 (매 프레임 Raycast 없음)
            if (Mouse.current == null)
                return;

            if (!Mouse.current.leftButton.wasPressedThisFrame)
                return;

            OnClickPressed();
        }

        private void LateUpdate()
        {
            if (!IsWorldSpacePanel)
                return;

            if (!isPanelOpen || villagePanelRoot == null || _camera == null)
                return;

            if (targetObject != null)
                villagePanelRoot.position = targetObject.position + panelOffset;

            FaceCamera();
        }

        /// <summary>
        /// 클릭된 프레임에 1회만 호출됩니다.
        /// </summary>
        private void OnClickPressed()
        {
            // 패널 위 클릭: 닫지 않고 타이머만 갱신
            if (isPanelOpen && IsPointerOverVillagePanel())
            {
                RestartAutoCloseTimer();
                return;
            }

            if (blockWhenOverSelectableUI && IsPointerOverSelectableUI())
            {
                // 다른 UI 위 클릭은 집 Raycast를 막되, 옵션이면 패널은 외부 클릭으로 닫음
                TryCloseOnOutsideClick();
                return;
            }

            if (_camera == null)
            {
                Debug.LogWarning("[VillageUI_Manager] Camera가 연결되지 않았습니다.");
                return;
            }

            EnsureClickLayerMask();

            Vector2 screenPos = Mouse.current.position.ReadValue();
            Ray ray = _camera.ScreenPointToRay(screenPos);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, clickLayerMask))
            {
                TogglePanel();
                return;
            }

            // 월드/빈 공간 클릭
            TryCloseOnOutsideClick();
        }

        /// <summary>
        /// closeOnOutsideClick이 켜져 있고 패널이 열려 있으면 닫습니다.
        /// </summary>
        private void TryCloseOnOutsideClick()
        {
            if (!closeOnOutsideClick || !isPanelOpen)
                return;

            ClosePanel();
        }

        private void EnsureClickLayerMask()
        {
            if (clickLayerMask.value != 0)
                return;

            int mask = LayerMask.GetMask(VillageClickLayerName);
            if (mask == 0)
            {
                Debug.LogWarning(
                    "[VillageUI_Manager] Layer '" + VillageClickLayerName +
                    "'을(를) 찾을 수 없습니다.");
                return;
            }

            clickLayerMask = mask;
        }

        private bool IsPointerOverSelectableUI()
        {
            if (EventSystem.current == null)
                return false;

            var eventData = new PointerEventData(EventSystem.current)
            {
                position = Mouse.current != null
                    ? Mouse.current.position.ReadValue()
                    : (Vector2)Input.mousePosition
            };

            _uiHits.Clear();
            EventSystem.current.RaycastAll(eventData, _uiHits);

            for (int i = 0; i < _uiHits.Count; i++)
            {
                GameObject hitGo = _uiHits[i].gameObject;
                if (hitGo == null)
                    continue;

                if (hitGo.GetComponentInParent<Selectable>() != null)
                    return true;
            }

            return false;
        }

        private bool IsPointerOverVillagePanel()
        {
            if (!isPanelOpen || EventSystem.current == null)
                return false;

            if (villagePanelRoot == null && villagePanelContent == null)
                return false;

            var eventData = new PointerEventData(EventSystem.current)
            {
                position = Mouse.current != null
                    ? Mouse.current.position.ReadValue()
                    : (Vector2)Input.mousePosition
            };

            _uiHits.Clear();
            EventSystem.current.RaycastAll(eventData, _uiHits);

            for (int i = 0; i < _uiHits.Count; i++)
            {
                Transform hitTransform = _uiHits[i].gameObject.transform;

                if (villagePanelContent != null)
                {
                    if (hitTransform == villagePanelContent.transform ||
                        hitTransform.IsChildOf(villagePanelContent.transform))
                        return true;
                }

                if (villagePanelRoot != null)
                {
                    if (hitTransform == villagePanelRoot ||
                        hitTransform.IsChildOf(villagePanelRoot))
                        return true;
                }
            }

            return false;
        }

        public void TogglePanel()
        {
            if (isPanelOpen)
                ClosePanel();
            else
                OpenPanel();
        }

        public void OpenPanel()
        {
            if (PanelContent == null)
            {
                Debug.LogWarning("[VillageUI_Manager] 표시할 패널 Content가 없습니다.");
                return;
            }

            if (IsWorldSpacePanel && villagePanelRoot != null && targetObject != null)
                villagePanelRoot.position = targetObject.position + panelOffset;

            if (IsWorldSpacePanel)
                FaceCamera();

            PanelContent.SetActive(true);
            isPanelOpen = true;

            PushVillageDataToUI();
            RestartAutoCloseTimer();
        }

        public void ClosePanel()
        {
            StopAutoCloseTimer();
            HidePanelContent();
            isPanelOpen = false;
        }

        public void RefreshVillageUI()
        {
            if (!isPanelOpen)
                return;

            PushVillageDataToUI();
        }

        /// <summary>
        /// Coin_Slider Hover 시 다음 레벨 미리보기 수치를 LvUp 패널에 반영합니다.
        /// VillageSystem 연동 전까지는 Inspector 테스트 값을 사용합니다.
        /// </summary>
        public void ApplyLvUpHoverPreview(VillageLvUpStateHover hover)
        {
            if (hover == null)
                return;

            hover.SetPreview(
                nextTownLevel,
                nextClickCoin,
                nextTypingCoin,
                nextToolCapacity,
                nextAutoProductBonus);
        }

        private void HidePanelContent()
        {
            if (PanelContent != null)
                PanelContent.SetActive(false);
        }

        private void PushVillageDataToUI()
        {
            if (uiController == null)
                return;

            long currentCoin = 0;
            if (CoinManager.Instance != null)
                currentCoin = CoinManager.Instance.totalCoin;

            int displayClickCoin = clickCoin;
            int displayTypingCoin = typingCoin;
            if (EarnProcessor.Instance != null)
            {
                displayClickCoin = EarnProcessor.Instance.BaseCoinPerClick * EarnProcessor.Instance.ClickMultiplier;
                displayTypingCoin = EarnProcessor.Instance.BaseCoinPerTyping * EarnProcessor.Instance.TypingMultiplier;
            }

            bool canUpgrade = requireLevelupCoin > 0 && currentCoin >= requireLevelupCoin;

            uiController.Refresh(
                townLevel,
                displayClickCoin,
                displayTypingCoin,
                toolCapacity,
                autoProductBonus,
                currentCoin,
                requireLevelupCoin,
                canUpgrade);
        }

        private void FaceCamera()
        {
            if (villagePanelRoot == null || _camera == null)
                return;

            Transform cam = _camera.transform;
            villagePanelRoot.rotation = Quaternion.LookRotation(cam.forward, cam.up);
        }

        private void RestartAutoCloseTimer()
        {
            StopAutoCloseTimer();

            if (!isActiveAndEnabled || autoCloseSeconds <= 0f)
                return;

            autoCloseCoroutine = StartCoroutine(AutoCloseRoutine());
        }

        private void StopAutoCloseTimer()
        {
            if (autoCloseCoroutine == null)
                return;

            StopCoroutine(autoCloseCoroutine);
            autoCloseCoroutine = null;
        }

        private IEnumerator AutoCloseRoutine()
        {
            yield return new WaitForSeconds(autoCloseSeconds);
            autoCloseCoroutine = null;
            ClosePanel();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (autoCloseSeconds < 0f)
                autoCloseSeconds = 0f;

            if (clickLayerMask.value == 0 || clickLayerMask.value == ~0)
            {
                int mask = LayerMask.GetMask(VillageClickLayerName);
                if (mask != 0)
                    clickLayerMask = mask;
            }
        }

        private void Reset()
        {
            clickLayerMask = LayerMask.GetMask(VillageClickLayerName);
        }
#endif
    }
}
