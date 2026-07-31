using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TaskTown.Gacha;

/// <summary>
/// [ECHO TD] pure C# 및 유니티 기본 UI 전용 폴라로이드 카메라 가챠 연출 디렉터.
/// (스페이스바 단축키 전용 테스트 버전)
/// </summary>
public class PolaroidGachaDirector : MonoBehaviour
{
    [Header("★ UI Visual Components ★")]
    [SerializeField] private CanvasGroup flashCanvasGroup;
    [SerializeField] private RectTransform cameraContainer;
    [SerializeField] private RectTransform printedPhotoUI;
    [SerializeField] private RectTransform resultWindowPanel;
    [SerializeField] private CanvasGroup resultWindowCanvasGroup;

    [Header("★ Result UI Data Binding ★")]
    [SerializeField] private Image resultItemIcon;
    [SerializeField] private TextMeshProUGUI resultItemNameText;

    private Vector2 _photoOriginalAnchoredPos;
    private Vector2 _resultWindowOriginalPos;

    private void Awake()
    {
        if (printedPhotoUI != null)
            _photoOriginalAnchoredPos = printedPhotoUI.anchoredPosition;

        if (resultWindowPanel != null)
            _resultWindowOriginalPos = resultWindowPanel.anchoredPosition;

        InitVisualState();
    }

    private void Update()
    {
        // 주인님 요청: 스페이스바 누르면 곧바로 연출 실행!
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartGachaSequence(1, null);
        }
    }

    /// <summary>
    /// 연출 시작 전 UI 초기화
    /// </summary>
    public void InitVisualState()
    {
        if (flashCanvasGroup != null) flashCanvasGroup.alpha = 0f;

        if (cameraContainer != null)
        {
            cameraContainer.localScale = Vector3.zero;
            cameraContainer.gameObject.SetActive(false);
        }

        if (printedPhotoUI != null)
        {
            printedPhotoUI.anchoredPosition = _photoOriginalAnchoredPos;
            printedPhotoUI.localRotation = Quaternion.identity;
            printedPhotoUI.gameObject.SetActive(false);
        }

        if (resultWindowPanel != null)
        {
            resultWindowPanel.anchoredPosition = new Vector2(_resultWindowOriginalPos.x, 1500f);
            resultWindowPanel.gameObject.SetActive(false);
        }

        if (resultWindowCanvasGroup != null)
        {
            resultWindowCanvasGroup.blocksRaycasts = false;
        }
    }

    /// <summary>
    /// 연출 시작 진입점
    /// </summary>
    public void StartGachaSequence(int count, List<GachaEntryData> results)
    {
        // 전달된 데이터가 있을 때만 UI 텍스트/아이콘 바인딩 (null 예외 처리)
        if (results != null && results.Count > 0 && results[0] != null)
        {
            if (resultItemNameText != null) resultItemNameText.text = results[0].DisplayName;
            if (resultItemIcon != null) resultItemIcon.sprite = results[0].Icon;
        }

        StopAllCoroutines();
        StartCoroutine(CoPlayPolaroidSequence());
    }

    /// <summary>
    /// 메인 연출 시퀀스 코루틴
    /// </summary>
    private IEnumerator CoPlayPolaroidSequence()
    {
        InitVisualState();

        // [Phase 1] 카메라 등장 (EaseOutBack 적용)
        cameraContainer.gameObject.SetActive(true);
        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // $S(t) = 1 + 2.70158(t-1)^3 + 1.70158(t-1)^2$
            float scale = 1f + 2.70158f * Mathf.Pow(t - 1f, 3) + 1.70158f * Mathf.Pow(t - 1f, 2);
            cameraContainer.localScale = Vector3.one * scale;
            yield return null;
        }
        cameraContainer.localScale = Vector3.one;

        yield return new WaitForSeconds(0.2f);

        // [Phase 2] 플래시 효과
        duration = 0.15f;
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            flashCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            yield return null;
        }

        printedPhotoUI.gameObject.SetActive(true);

        duration = 0.25f;
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            flashCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            yield return null;
        }
        flashCanvasGroup.alpha = 0f;

        // [Phase 3] 사진 인화 (SmoothStep)
        duration = 0.8f;
        elapsed = 0f;
        Vector2 startPos = _photoOriginalAnchoredPos;
        Vector2 ejectTargetPos = startPos + new Vector2(0f, -250f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // $S(t) = 3t^2 - 2t^3$
            float smoothT = t * t * (3f - 2f * t);

            printedPhotoUI.anchoredPosition = Vector2.Lerp(startPos, ejectTargetPos, smoothT);
            yield return null;
        }

        yield return new WaitForSeconds(0.3f);

        // [Phase 4] 사진 휘리릭 화면 밖으로 날아가기
        duration = 0.6f;
        elapsed = 0f;
        Vector2 flyStartPos = printedPhotoUI.anchoredPosition;
        Vector2 flyTargetPos = flyStartPos + new Vector2(1200f, 800f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easeT = t * t;

            printedPhotoUI.anchoredPosition = Vector2.Lerp(flyStartPos, flyTargetPos, easeT);
            float currentAngle = Mathf.Lerp(0f, -360f, easeT);
            printedPhotoUI.localRotation = Quaternion.Euler(0f, 0f, currentAngle);

            yield return null;
        }

        cameraContainer.gameObject.SetActive(false);

        // [Phase 5] 결과창 스르륵 내려오기 (EaseOutCubic)
        resultWindowPanel.gameObject.SetActive(true);
        duration = 0.7f;
        elapsed = 0f;

        Vector2 topHidePos = new Vector2(_resultWindowOriginalPos.x, 1500f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // $E(t) = 1 - (1 - t)^3$
            float easeOutT = 1f - Mathf.Pow(1f - t, 3f);

            resultWindowPanel.anchoredPosition = Vector2.Lerp(topHidePos, _resultWindowOriginalPos, easeOutT);
            yield return null;
        }

        resultWindowPanel.anchoredPosition = _resultWindowOriginalPos;
        if (resultWindowCanvasGroup != null)
        {
            resultWindowCanvasGroup.blocksRaycasts = true;
        }
    }
}