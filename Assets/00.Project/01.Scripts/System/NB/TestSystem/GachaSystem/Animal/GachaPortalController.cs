//NB

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using TaskTown.Gacha;

/// 동물 차원문 뽑기 연출 및 결과창 UI 팝업 전담 컨트롤러
public class GachaPortalController : MonoBehaviour
{
    [Header("UI Panel References")]
    [Tooltip("차원문 연출 전체를 감싸는 최상위 패널")]
    [SerializeField] private GameObject gachaPanel;
    [Tooltip("결과물 슬롯들이 출력되는 결과 팝업 패널")]
    [SerializeField] private GameObject resultPopupPanel;
    [SerializeField] private CanvasGroup panelCanvasGroup;

    [Header("Portal UI Elements")]
    [SerializeField] private Transform portalContainer;
    [SerializeField] private GameObject closedImgObj;
    [SerializeField] private GameObject halfOpenImgObj;
    [SerializeField] private GameObject fullOpenImgObj;

    [Header("Result Popup Elements")]
    [SerializeField] private Transform itemGridParent;
    [SerializeField] private GameObject itemSlotPrefab;
    [SerializeField] private Button btnConfirm;

    [Header("Timing Settings")]
    [SerializeField] private float popupEmergeDuration = 0.45f;

    [Header("Debug & Dummy Data")]
    [SerializeField] private bool enableSpaceKeyTest = true;
    [SerializeField] private List<GachaEntryData> debugDummyEntries = new List<GachaEntryData>();

    public bool IsAnimating => _isAnimating;
    public event Action OnPortalOpened;

    private bool _isAnimating = false;
    private Vector3 _baseScale;
    private Quaternion _baseRotation;
    private Coroutine _activeGachaCoroutine;

    private readonly List<GameObject> _spawnedSlotPool = new List<GameObject>();

    private void Awake()
    {
        panelCanvasGroup ??= GetComponent<CanvasGroup>();

        if (portalContainer == null) portalContainer = transform;

        if (portalContainer != null)
        {
            _baseScale = portalContainer.localScale;
            _baseRotation = portalContainer.localRotation;
        }

        if (btnConfirm != null)
        {
            btnConfirm.onClick.RemoveAllListeners();
            btnConfirm.onClick.AddListener(CloseGachaUI);
        }

        ValidateReferences();
    }

    private void Start()
    {
        // 에디터에 UI가 켜져 있더라도 게임 시작 시 완전히 숨김 처리
        ResetPortalState();
    }

    private void ValidateReferences()
    {
        if (closedImgObj == null || halfOpenImgObj == null || fullOpenImgObj == null)
        {
            Debug.LogError($"[{GetType().Name}] 경고: Closed, HalfOpen, FullOpen 이미지 오브젝트 중 일부가 할당되지 않았습니다!", this);
        }
    }

    private void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (enableSpaceKeyTest && Input.GetKeyDown(KeyCode.Space))
        {
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
            {
                var selectedObj = EventSystem.current.currentSelectedGameObject;
                if (selectedObj.GetComponent<InputField>() != null ||
                    selectedObj.GetComponent<TMPro.TMP_InputField>() != null)
                {
                    return;
                }
            }

            StartGachaSequence(10, null);
        }
#endif
    }

    // 버튼 클릭 시 가챠 연출 시작 (꺼져있던 UI를 켜면서 연출 돌입)
    public void StartGachaSequence(int drawCount, List<GachaEntryData> resultEntries)
    {
        if (_isAnimating) return;
        _isAnimating = true;

        StopAndResetAllCoroutinesAndTweens();

        if (resultEntries == null || resultEntries.Count == 0)
        {
            resultEntries = GetDummyEntries(drawCount);
        }

        // 숨겨져 있던 차원문 패널 즉시 활성화
        if (gachaPanel != null) gachaPanel.SetActive(true);
        if (resultPopupPanel != null) resultPopupPanel.SetActive(false);

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 1f;
            panelCanvasGroup.blocksRaycasts = false; // 연출 동안 클릭 방지
        }

        _activeGachaCoroutine = StartCoroutine(Co_PlayGachaSequence(drawCount, resultEntries));
    }

    private IEnumerator Co_PlayGachaSequence(int drawCount, List<GachaEntryData> resultEntries)
    {
        portalContainer.DOKill();
        portalContainer.localScale = _baseScale * 0.05f;
        portalContainer.localRotation = _baseRotation;

        // 닫힌 문으로 연출 시작
        SetDoorState(showClosed: true, showHalf: false, showFull: false);

        // 연출 1단계: $T_1 = 0.75\text{s}$ (줌인)
        Tween zoomTween = portalContainer.DOScale(_baseScale, 0.75f).SetEase(Ease.OutQuart);
        yield return zoomTween.WaitForCompletion();

        // 연출 2단계: $T_2 = 0.60\text{s}$ (진동)
        portalContainer.DOShakePosition(0.6f, strength: 12f, vibrato: 30);
        portalContainer.DOShakeRotation(0.6f, strength: new Vector3(0f, 0f, 6f), vibrato: 25);
        yield return new WaitForSeconds(0.6f);

        // 연출 3단계: $T_3 = 0.40\text{s}$ (반열림)
        SetDoorState(showClosed: false, showHalf: true, showFull: false);
        portalContainer.DOPunchScale(_baseScale * 0.15f, 0.35f, vibrato: 8, elasticity: 0.5f);
        portalContainer.DOShakePosition(0.4f, strength: 15f, vibrato: 35);
        yield return new WaitForSeconds(0.4f);

        // 연출 4단계: $T_4 = 0.50\text{s}$ (완전열림)
        SetDoorState(showClosed: false, showHalf: false, showFull: true);
        portalContainer.DOPunchScale(_baseScale * 0.25f, 0.5f, vibrato: 10, elasticity: 0.5f);
        portalContainer.DOShakePosition(0.5f, strength: 20f, vibrato: 40);
        yield return new WaitForSeconds(0.5f);

        portalContainer.localScale = _baseScale;
        portalContainer.localRotation = _baseRotation;

        AnimateResultEmergenceFromPortal(drawCount, resultEntries);

        _isAnimating = false;
        if (panelCanvasGroup != null) panelCanvasGroup.blocksRaycasts = true;

        OnPortalOpened?.Invoke();
    }

    private void AnimateResultEmergenceFromPortal(int drawCount, List<GachaEntryData> resultEntries)
    {
        if (resultPopupPanel == null) return;

        resultPopupPanel.SetActive(true);

        RectTransform resultRect = resultPopupPanel.transform as RectTransform;
        if (resultRect != null)
        {
            resultRect.localScale = Vector3.zero;
            resultRect.localRotation = Quaternion.identity;

            Sequence emergeSeq = DOTween.Sequence();
            emergeSeq.Append(resultRect.DOScale(Vector3.one, popupEmergeDuration).SetEase(Ease.OutBack));
            emergeSeq.Join(portalContainer.DOScale(_baseScale * 0.85f, popupEmergeDuration).SetEase(Ease.OutQuad));
        }

        PopulateResultSlots(drawCount, resultEntries);
    }

    private void PopulateResultSlots(int drawCount, List<GachaEntryData> resultEntries)
    {
        for (int i = 0; i < _spawnedSlotPool.Count; i++)
        {
            _spawnedSlotPool[i].SetActive(false);
        }

        int targetSlotCount = (resultEntries != null && resultEntries.Count > 0) ? resultEntries.Count : drawCount;

        for (int i = 0; i < targetSlotCount; i++)
        {
            GameObject slotObj;

            if (i < _spawnedSlotPool.Count)
            {
                slotObj = _spawnedSlotPool[i];
                slotObj.SetActive(true);
            }
            else
            {
                slotObj = Instantiate(itemSlotPrefab, itemGridParent);
                _spawnedSlotPool.Add(slotObj);
            }

            // 다형성 슬롯 UI 데이터 바인딩
            if (resultEntries != null && i < resultEntries.Count && resultEntries[i] != null)
            {
                if (slotObj.TryGetComponent<GachaAnimalSlotUI>(out var animalSlot))
                {
                    animalSlot.SetItem(resultEntries[i]);
                }
                else if (slotObj.TryGetComponent<GachaItemSlotUI>(out var itemSlot))
                {
                    itemSlot.SetItem(resultEntries[i]);
                }
            }

            // 슬롯 등장 시차 수식 적용: $T_{\text{delay}}(i) = T_{\text{emerge}} \times 0.5 + (0.04\text{s} \times i)$
            slotObj.transform.localScale = Vector3.zero;
            slotObj.transform.DOScale(1.0f, 0.25f)
                   .SetDelay(popupEmergeDuration * 0.5f + (0.04f * i))
                   .SetEase(Ease.OutBack);
        }

        if (itemGridParent is RectTransform gridRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(gridRect);
        }
    }

    // 연출 없이 단순히 가챠 창만 오픈할 때 호출
    public void OpenGachaWindowOnly()
    {
        if (gachaPanel != null) gachaPanel.SetActive(true);
        SetDoorState(showClosed: true, showHalf: false, showFull: false);
    }

    // UI 닫기 및 초기 상태로 초기화
    public void CloseGachaUI()
    {
        ResetPortalState();
    }

    // 차원문 및 결과창 UI 상태 완전 리셋 
    public void ResetPortalState()
    {
        StopAndResetAllCoroutinesAndTweens();
        _isAnimating = false;

        if (resultPopupPanel != null) resultPopupPanel.SetActive(false);
        if (gachaPanel != null) gachaPanel.SetActive(false);

        SetDoorState(showClosed: false, showHalf: false, showFull: false);

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 1f;
            panelCanvasGroup.blocksRaycasts = true;
        }
    }

    private void SetDoorState(bool showClosed, bool showHalf, bool showFull)
    {
        if (closedImgObj != null) closedImgObj.SetActive(showClosed);
        if (halfOpenImgObj != null) halfOpenImgObj.SetActive(showHalf);
        if (fullOpenImgObj != null) fullOpenImgObj.SetActive(showFull);
    }

    private void StopAndResetAllCoroutinesAndTweens()
    {
        if (_activeGachaCoroutine != null)
        {
            StopCoroutine(_activeGachaCoroutine);
            _activeGachaCoroutine = null;
        }

        if (portalContainer != null) portalContainer.DOKill();
        if (resultPopupPanel != null) resultPopupPanel.transform.DOKill();
    }

    private List<GachaEntryData> GetDummyEntries(int count)
    {
        List<GachaEntryData> list = new List<GachaEntryData>();
        for (int i = 0; i < count; i++)
        {
            if (debugDummyEntries != null && debugDummyEntries.Count > 0)
            {
                list.Add(debugDummyEntries[i % debugDummyEntries.Count]);
            }
            else
            {
                list.Add(null);
            }
        }
        return list;
    }

    private void OnDisable()
    {
        ResetPortalState();
    }

    private void OnDestroy()
    {
        ResetPortalState();
    }
}