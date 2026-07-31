//NB

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TaskTown.Gacha;

// 가챠 연출 전담 View 디렉터.
// GachaEntryData 기반으로 결과를 슬롯 UI에 바인딩합니다.
[RequireComponent(typeof(CanvasGroup))]
public class GachaDirector : MonoBehaviour
{
    [Header("UI Panel References")]
    [SerializeField] private GameObject gachaPanel;
    [SerializeField] private GameObject resultPopupPanel;
    [SerializeField] private CanvasGroup panelCanvasGroup;

    [Header("Box Visual Elements")]
    [SerializeField] private RectTransform imageBoxTransform;
    [SerializeField] private GameObject imageBoxClosed;
    [SerializeField] private GameObject imageBoxOpen;

    [Header("Result Popup Elements")]
    [SerializeField] private Transform itemGridParent;
    [SerializeField] private GameObject itemSlotPrefab;

    [Header("UI Buttons")]
    [SerializeField] private Button btnConfirm; // 결과창 확인 버튼

    [Header("Trajectory & Position Settings")]
    [SerializeField] private Vector2 startPosition = new Vector2(-1100f, 600f);
    [SerializeField] private Vector2 finalTargetPosition = new Vector2(0f, -220f);

    [Header("Bounce Jump Parameters")]
    [SerializeField] private float jumpPower1 = 450f;
    [SerializeField] private float duration1 = 0.50f;
    [SerializeField] private float jumpPower2 = 200f;
    [SerializeField] private float duration2 = 0.35f;
    [SerializeField] private float jumpPower3 = 80f;
    [SerializeField] private float duration3 = 0.22f;

    [Header("Detail Timing Settings")]
    [SerializeField] private float boxOpenWaitDelay = 0.7f;
    [SerializeField] private float popupEmergeDuration = 0.45f;

    [Header("Debug Dummy Entries")]
    [Tooltip("테스트용 더미 GachaEntryData 데이터베이스 리스트")]
    [SerializeField] private List<GachaEntryData> debugDummyEntries = new List<GachaEntryData>();

    private Sequence _gachaSequence;
    private bool _isAnimating;
    private readonly List<GameObject> _spawnedSlotPool = new List<GameObject>();

    private void Awake()
    {
        panelCanvasGroup ??= GetComponent<CanvasGroup>();

        if (imageBoxTransform != null) SetupTransformPivotCenter(imageBoxTransform);
        if (btnConfirm) btnConfirm.onClick.AddListener(CloseGachaUI);

        AutoHidePanelsOnStart();
    }

    private void AutoHidePanelsOnStart()
    {
        if (gachaPanel != null) gachaPanel.SetActive(false);
        if (resultPopupPanel != null) resultPopupPanel.SetActive(false);
        if (imageBoxClosed != null) imageBoxClosed.SetActive(false);
        if (imageBoxOpen != null) imageBoxOpen.SetActive(false);
    }

    private void SetupTransformPivotCenter(RectTransform rect)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    public void OpenGachaWindowOnly()
    {
        ResetAllTweens();
        _isAnimating = false;

        if (gachaPanel != null) gachaPanel.SetActive(true);
        if (resultPopupPanel != null) resultPopupPanel.SetActive(false);
        if (imageBoxClosed != null) imageBoxClosed.SetActive(false);
        if (imageBoxOpen != null) imageBoxOpen.SetActive(false);

        if (panelCanvasGroup != null) panelCanvasGroup.blocksRaycasts = true;

        Debug.Log("<color=cyan>[GachaDirector]</color> 가챠 UI 메인 창만 오픈되었습니다.");
    }

    // [수정 완료] GachaEntryData 리스트를 매개변수로 받아서 가챠 연출을 시작
    public void StartGachaSequence(int drawCount, List<GachaEntryData> resultEntries)
    {
        if (_isAnimating) return;
        _isAnimating = true;

        ResetAllTweens();

        // Null 데이터 수신 시 더미 데이터로 자동 보정 처리
        if (resultEntries == null || resultEntries.Count == 0)
        {
            resultEntries = GetDummyEntries(drawCount);
        }

        if (gachaPanel != null && !gachaPanel.activeSelf) gachaPanel.SetActive(true);
        if (resultPopupPanel != null) resultPopupPanel.SetActive(false);

        // 연출 도중 클릭 방지
        if (panelCanvasGroup != null) panelCanvasGroup.blocksRaycasts = false;

        if (imageBoxClosed != null) imageBoxClosed.SetActive(true);
        if (imageBoxOpen != null) imageBoxOpen.SetActive(false);

        SetupTransformPivotCenter(imageBoxTransform);
        imageBoxTransform.anchoredPosition = startPosition;
        imageBoxTransform.localScale = Vector3.one;
        imageBoxTransform.localRotation = Quaternion.Euler(0f, 0f, 35f);

        _gachaSequence = DOTween.Sequence();

        float startX = startPosition.x;
        float finalX = finalTargetPosition.x;
        float targetY = finalTargetPosition.y;

        Vector2 target1 = new Vector2(Mathf.Lerp(startX, finalX, 0.50f), targetY);
        Vector2 target2 = new Vector2(Mathf.Lerp(startX, finalX, 0.82f), targetY);
        Vector2 target3 = finalTargetPosition;

        // 바운드 점프 애니메이션 트윈 체이닝
        _gachaSequence.Append(imageBoxTransform.DOJumpAnchorPos(target1, jumpPower1, 1, duration1).SetEase(Ease.Linear));
        _gachaSequence.Join(imageBoxTransform.DORotate(new Vector3(0f, 0f, -360f), duration1, RotateMode.FastBeyond360).SetEase(Ease.Linear));

        _gachaSequence.Append(imageBoxTransform.DOJumpAnchorPos(target2, jumpPower2, 1, duration2).SetEase(Ease.Linear));
        _gachaSequence.Join(imageBoxTransform.DORotate(new Vector3(0f, 0f, -540f), duration2, RotateMode.FastBeyond360).SetEase(Ease.Linear));

        _gachaSequence.Append(imageBoxTransform.DOJumpAnchorPos(target3, jumpPower3, 1, duration3).SetEase(Ease.Linear));
        _gachaSequence.Join(imageBoxTransform.DORotate(Vector3.zero, duration3).SetEase(Ease.OutQuad));

        // 착지 후 찌그러짐 표현
        _gachaSequence.Append(imageBoxTransform.DOScale(new Vector3(1.35f, 0.65f, 1f), 0.08f).SetEase(Ease.OutQuad));
        _gachaSequence.Append(imageBoxTransform.DOScale(Vector3.one, 0.06f).SetEase(Ease.OutQuad));
        _gachaSequence.AppendInterval(0.1f);

        // 상자 진동 연출
        _gachaSequence.Append(imageBoxTransform.DOShakePosition(0.4f, strength: 18f, vibrato: 40));
        _gachaSequence.Join(imageBoxTransform.DOScale(1.15f, 0.4f).SetEase(Ease.InQuad));

        // 상자 오픈 스프라이트 교체
        _gachaSequence.AppendCallback(() =>
        {
            if (imageBoxClosed != null) imageBoxClosed.SetActive(false);
            if (imageBoxOpen != null) imageBoxOpen.SetActive(true);
        });

        _gachaSequence.Append(imageBoxTransform.DOScale(new Vector3(0.9f, 1.25f, 1f), 0.12f).SetEase(Ease.OutBack));
        _gachaSequence.Append(imageBoxTransform.DOScale(Vector3.one, 0.1f).SetEase(Ease.OutQuad));

        _gachaSequence.AppendInterval(boxOpenWaitDelay);

        // 결과 팝업 띄우기
        _gachaSequence.AppendCallback(() =>
        {
            AnimateResultEmergenceFromBox(drawCount, resultEntries);
        });

        _gachaSequence.OnComplete(() =>
        {
            _isAnimating = false;
            if (panelCanvasGroup != null) panelCanvasGroup.blocksRaycasts = true;
            Debug.Log("<color=lime>[GachaDirector]</color> 연출 완료!");
        });
    }

    private void AnimateResultEmergenceFromBox(int drawCount, List<GachaEntryData> resultEntries)
    {
        resultPopupPanel.SetActive(true);

        RectTransform resultRect = resultPopupPanel.transform as RectTransform;
        if (resultRect != null)
        {
            SetupTransformPivotCenter(resultRect);
            resultRect.anchoredPosition = imageBoxTransform.anchoredPosition;
            resultRect.localScale = Vector3.zero;
            resultRect.localRotation = Quaternion.identity;

            Sequence emergeSeq = DOTween.Sequence();
            emergeSeq.Append(resultRect.DOAnchorPos(Vector2.zero, popupEmergeDuration).SetEase(Ease.OutCubic));
            emergeSeq.Join(resultRect.DOScale(Vector3.one, popupEmergeDuration).SetEase(Ease.OutBack));
            emergeSeq.Join(imageBoxTransform.DOScale(0.85f, popupEmergeDuration).SetEase(Ease.OutQuad));
        }

        PopulateResultSlots(drawCount, resultEntries);
    }

    //GachaEntryData를 받아 각 UI 슬롯에 바인딩
    private void PopulateResultSlots(int drawCount, List<GachaEntryData> resultEntries)
    {
        for (int i = 0; i < _spawnedSlotPool.Count; i++)
        {
            _spawnedSlotPool[i].SetActive(false);
        }

        for (int i = 0; i < drawCount; i++)
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

            GachaItemSlotUI slotUI = slotObj.GetComponent<GachaItemSlotUI>();
            if (slotUI != null && resultEntries != null && i < resultEntries.Count)
            {
                // Sprite 대신 GachaEntryData 전달!
                slotUI.SetItem(resultEntries[i]);
            }

            // 시차 팝업 연출 딜레이 공식: T_delay = T_emerge * 0.5 + (0.04 * i)
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

    public void CloseGachaUI()
    {
        ResetAllTweens();
        _isAnimating = false;

        if (panelCanvasGroup != null) panelCanvasGroup.blocksRaycasts = true;

        if (resultPopupPanel != null && resultPopupPanel.transform is RectTransform resultRect)
        {
            resultRect.anchoredPosition = Vector2.zero;
            resultRect.localScale = Vector3.one;
        }

        if (resultPopupPanel != null) resultPopupPanel.SetActive(false);
        if (gachaPanel != null) gachaPanel.SetActive(false);

        if (imageBoxClosed != null) imageBoxClosed.SetActive(false);
        if (imageBoxOpen != null) imageBoxOpen.SetActive(false);
    }

    private void ResetAllTweens()
    {
        if (_gachaSequence != null && _gachaSequence.IsActive())
        {
            _gachaSequence.Kill();
            _gachaSequence = null;
        }

        if (imageBoxTransform != null) imageBoxTransform.DOKill();
        if (resultPopupPanel != null) resultPopupPanel.transform.DOKill();
    }

    //예외상황용 더미 GachaEntryData 생성 리스트
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

    private void OnDisable() => ResetAllTweens();
    private void OnDestroy() => ResetAllTweens();
}