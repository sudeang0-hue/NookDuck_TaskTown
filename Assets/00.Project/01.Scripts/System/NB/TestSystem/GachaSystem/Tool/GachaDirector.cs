//NB

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TaskTown.Gacha;

// 상자 바운스 방식 가챠 연출 전담 View 디렉터
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
    [SerializeField] private Button btnConfirm;

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
    [SerializeField] private List<GachaEntryData> debugDummyEntries = new List<GachaEntryData>();

    public bool IsAnimating => _isAnimating;

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
        if (_isAnimating)
        {
            Debug.LogWarning("<color=yellow>[GachaDirector]</color> 가챠 연출 재생 중에는 메인 창을 다시 열 수 없습니다.");
            return;
        }

        ResetAllTweens();
        _isAnimating = false;

        if (gachaPanel != null) gachaPanel.SetActive(true);
        if (resultPopupPanel != null) resultPopupPanel.SetActive(false);
        if (imageBoxClosed != null) imageBoxClosed.SetActive(false);
        if (imageBoxOpen != null) imageBoxOpen.SetActive(false);

        if (panelCanvasGroup != null) panelCanvasGroup.blocksRaycasts = true;
    }

    public void StartGachaSequence(int drawCount, List<GachaEntryData> resultEntries)
    {
        if (_isAnimating) return;
        _isAnimating = true;

        ResetAllTweens();

        if (resultEntries == null || resultEntries.Count == 0)
        {
            resultEntries = GetDummyEntries(drawCount);
        }

        if (gachaPanel != null && !gachaPanel.activeSelf) gachaPanel.SetActive(true);
        if (resultPopupPanel != null) resultPopupPanel.SetActive(false);
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

        _gachaSequence.Append(imageBoxTransform.DOJumpAnchorPos(target1, jumpPower1, 1, duration1).SetEase(Ease.Linear));
        _gachaSequence.Join(imageBoxTransform.DORotate(new Vector3(0f, 0f, -360f), duration1, RotateMode.FastBeyond360).SetEase(Ease.Linear));

        _gachaSequence.Append(imageBoxTransform.DOJumpAnchorPos(target2, jumpPower2, 1, duration2).SetEase(Ease.Linear));
        _gachaSequence.Join(imageBoxTransform.DORotate(new Vector3(0f, 0f, -540f), duration2, RotateMode.FastBeyond360).SetEase(Ease.Linear));

        _gachaSequence.Append(imageBoxTransform.DOJumpAnchorPos(target3, jumpPower3, 1, duration3).SetEase(Ease.Linear));
        _gachaSequence.Join(imageBoxTransform.DORotate(Vector3.zero, duration3).SetEase(Ease.OutQuad));

        _gachaSequence.Append(imageBoxTransform.DOScale(new Vector3(1.35f, 0.65f, 1f), 0.08f).SetEase(Ease.OutQuad));
        _gachaSequence.Append(imageBoxTransform.DOScale(Vector3.one, 0.06f).SetEase(Ease.OutQuad));
        _gachaSequence.AppendInterval(0.1f);

        _gachaSequence.Append(imageBoxTransform.DOShakePosition(0.4f, strength: 18f, vibrato: 40));
        _gachaSequence.Join(imageBoxTransform.DOScale(1.15f, 0.4f).SetEase(Ease.InQuad));

        _gachaSequence.AppendCallback(() =>
        {
            if (imageBoxClosed != null) imageBoxClosed.SetActive(false);
            if (imageBoxOpen != null) imageBoxOpen.SetActive(true);
        });

        _gachaSequence.Append(imageBoxTransform.DOScale(new Vector3(0.9f, 1.25f, 1f), 0.12f).SetEase(Ease.OutBack));
        _gachaSequence.Append(imageBoxTransform.DOScale(Vector3.one, 0.1f).SetEase(Ease.OutQuad));

        _gachaSequence.AppendInterval(boxOpenWaitDelay);

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

            // GachaAnimalSlotUI와 GachaItemSlotUI 타입 모두 안전하게 바인딩
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
        _isAnimating = false;
        ResetAllTweens();
    }

    private void OnDestroy()
    {
        _isAnimating = false;
        ResetAllTweens();
    }
}