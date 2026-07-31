using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// [ECHO TD Architecture]
/// 에셋 없이 UI 스케일, 알파, 회전만으로 연출하는 최적화된 빛무리(Glow) 효과 컨트롤러.
/// </summary>
[RequireComponent(typeof(Image))]
[RequireComponent(typeof(CanvasGroup))]
public class GachaLightGlow : MonoBehaviour
{
    [Header("★ UI Component References ★")]
    [SerializeField] private RectTransform glowRect;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("★ Animation Settings ★")]
    [Tooltip("빛무리가 퍼지는 연출 시간(초)")]
    [SerializeField] private float duration = 0.8f;

    [Tooltip("빛무리가 회전하는 속도 (deg/sec)")]
    [SerializeField] private float rotationSpeed = 90f;

    private Sequence _glowSequence;
    private Tween _rotationTween;

    private void Awake()
    {
        if (glowRect == null) glowRect = GetComponent<RectTransform>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

        // 초기 상태: 완전히 숨김
        ResetGlowState();
    }

    private void Update()
    {
        // 켜져 있을 때 잔잔한 Z축 무한 회전 연출 (Visual Polish)
        if (canvasGroup.alpha > 0.01f)
        {
            glowRect.Rotate(Vector3.forward * (rotationSpeed * Time.deltaTime));
        }
    }

    /// <summary>
    /// 빛무리가 팡! 터지며 나타났다 사라지는 시퀀스 연출 실행
    /// </summary>
    public void PlayGlowBurst(System.Action onComplete = null)
    {
        // 기존 진행 중인 트윈 안전 종료 (GC 및 중복 방지)
        _glowSequence?.Kill();

        ResetGlowState();
        gameObject.SetActive(true);

        // DOTween Sequence 생성
        _glowSequence = DOTween.Sequence();

        // 1. 빠른 알파 팝업 (Fade In) & 스케일 확장 (Scale Up)
        _glowSequence.Append(canvasGroup.DOFade(1f, duration * 0.3f).SetEase(Ease.OutQuad));
        _glowSequence.Join(glowRect.DOScale(Vector3.one * 1.5f, duration * 0.4f).SetEase(Ease.OutBack));

        // 2. 부드러운 소멸 (Fade Out) & 스케일 디스퍼전 (Scale Expansion)
        _glowSequence.Append(canvasGroup.DOFade(0f, duration * 0.6f).SetEase(Ease.InQuad));
        _glowSequence.Join(glowRect.DOScale(Vector3.one * 2.5f, duration * 0.6f).SetEase(Ease.InQuad));

        // 3. 종료 후 정리
        _glowSequence.OnComplete(() =>
        {
            ResetGlowState();
            onComplete?.Invoke();
        });
    }

    /// <summary>
    /// 연출 초기화 함수
    /// </summary>
    public void ResetGlowState()
    {
        _glowSequence?.Kill();
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        if (glowRect != null) glowRect.localScale = Vector3.one * 0.2f;
    }

    private void OnDisable()
    {
        _glowSequence?.Kill();
    }
}