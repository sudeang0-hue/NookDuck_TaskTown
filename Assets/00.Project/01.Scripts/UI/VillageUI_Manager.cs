using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// VillageHouse 레이어 클릭 시 마을 패널 출력 및 정보 연결을 담당합니다.
/// Canvas가 Overlay인 경우 WorldSpace 빌보드/추적을 하지 않습니다.
/// </summary>
public class VillageUI_Manager : MonoBehaviour
{
    private const string VillageClickLayerName = "VillageHouse";

    [Header("참조")]
    [SerializeField] private Camera _camera;
    [SerializeField] private Transform targetObject; // 패널 위치 기준 (WorldSpace일 때)
    [SerializeField] private Transform villagePanelRoot;
    [SerializeField] private GameObject villagePanelContent; // VillageInfo_Root
    [SerializeField] private UIController_Village uiController;
    [SerializeField] private Canvas villageCanvas;

    [Header("패널 배치 (WorldSpace 전용)")]
    [SerializeField] private Vector3 panelOffset = new Vector3(0f, 1.5f, 0f);

    [Header("클릭 / 자동 닫힘")]
    [SerializeField] private float autoCloseSeconds = 3f;
    [Tooltip("VillageHouse 레이어만 체크하세요.")]
    [SerializeField] private LayerMask clickLayerMask;
    [Tooltip("true면 VillageInfo_Root 밖 클릭 시 패널을 닫습니다. (현재 보류: 기본 false)")]
    [SerializeField] private bool closeOnOutsideClick = false;

    [Header("테스트용 마을 정보 (VillageSystem 연동 전)")]
    [SerializeField] private int townLevel = 1;
    [SerializeField] private int clickCoin = 10;
    [SerializeField] private int typingCoin = 5;
    [SerializeField] private int toolCapacity = 3;
    [SerializeField] private int requireLevelupCoin = 200;

    private bool isPanelOpen;
    private Coroutine autoCloseCoroutine;
    private readonly List<RaycastResult> _uiRaycastResults = new List<RaycastResult>(16);

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

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        HandleClick();
    }

    private void LateUpdate()
    {
        // Overlay / Screen Space 에서는 위치·빌보드 추적 불필요
        if (!IsWorldSpacePanel)
            return;

        if (!isPanelOpen || villagePanelRoot == null || _camera == null)
            return;

        if (targetObject != null)
            villagePanelRoot.position = targetObject.position + panelOffset;

        FaceCamera();
    }

    private void OnDisable()
    {
        StopAutoCloseTimer();
    }

    private void HandleClick()
    {
        if (IsPointerOverVillagePanel())
        {
            RestartAutoCloseTimer();
            return;
        }

        if (_camera == null)
        {
            Debug.LogWarning("[VillageUI_Manager] Camera가 연결되지 않았습니다.");
            return;
        }

        EnsureClickLayerMask();

        Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
        LogRaycastAll(ray);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, clickLayerMask))
        {
            TogglePanel();
            return;
        }

        // 보류 기능: Inspector에서 closeOnOutsideClick 체크 시에만 바깥 클릭으로 닫기
        if (closeOnOutsideClick && isPanelOpen)
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
                "'을(를) 찾을 수 없습니다. Tags and Layers에서 레이어를 확인하세요.");
            return;
        }

        clickLayerMask = mask;
    }

    private void LogRaycastAll(Ray ray)
    {
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, clickLayerMask);
        if (hits.Length == 0)
        {
            //Debug.Log("[VillageRay] hit nothing (mask=" + clickLayerMask.value + ")");
            return;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit h = hits[i];
            Debug.Log(
                "[VillageRay] " + i + ": " + h.transform.name +
                " layer=" + LayerMask.LayerToName(h.transform.gameObject.layer) +
                " dist=" + h.distance.ToString("F3") +
                " trigger=" + h.collider.isTrigger);
        }
    }

    private void TogglePanel()
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

    /// <summary>
    /// 패널이 열린 상태에서 마을 정보 UI를 다시 갱신합니다.
    /// </summary>
    public void RefreshVillageUI()
    {
        if (!isPanelOpen)
            return;

        PushVillageDataToUI();
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

        // EarnProcessor: 실제 획득량 = Base * Multiplier
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

    private bool IsPointerOverVillagePanel()
    {
        if (!isPanelOpen || EventSystem.current == null)
            return false;

        if (villagePanelRoot == null && villagePanelContent == null)
            return false;

        var eventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        _uiRaycastResults.Clear();
        EventSystem.current.RaycastAll(eventData, _uiRaycastResults);

        for (int i = 0; i < _uiRaycastResults.Count; i++)
        {
            Transform hitTransform = _uiRaycastResults[i].gameObject.transform;

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
        closeOnOutsideClick = false;
    }
#endif
}
