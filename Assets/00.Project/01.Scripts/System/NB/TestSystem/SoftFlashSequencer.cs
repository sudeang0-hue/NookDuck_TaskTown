using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 눈이 피로하지 않은 샴페인 골드 방사형 플래시 연출 클래스
/// </summary>
public class SoftFlashSequencer : MonoBehaviour
{
    [Header("Target UI References")]
    [SerializeField] private Image flashImage;           // 방사형 Gradient 마스크가 적용된 UI Image
    [SerializeField] private Transform cameraTransform;   // 살짝 쿵 연출을 줄 카메라 Transform

    [Header("Soft Flash Settings")]
    // [핵심] 순백색 대신 눈이 편안한 warm 계열 샴페인 골드 색상 사용
    [SerializeField] private Color flashColor = new Color(1.0f, 0.95f, 0.8f, 0.85f);
    [SerializeField] private float flashInDuration = 0.03f;  // 번쩍이는 시간 (매우 짧게)
    [SerializeField] private float flashOutDuration = 0.25f; // 빛이 식는 시간

    [Header("Camera Impact Settings")]
    [SerializeField] private Vector3 punchScaleVector = new Vector3(0.08f, 0.08f, 0f); // 순간 확대 반동

    /// <summary>
    /// 눈이 편안하면서도 타격감 있는 플래시 연출 실행
    /// </summary>
    public void TriggerSoftFlash(Action onComplete = null)
    {
        if (flashImage == null) return;

        // 1. 초기 상태 세팅
        flashImage.gameObject.SetActive(true);
        flashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f); // Alpha 0에서 시작

        Sequence flashSeq = DOTween.Sequence();

        // 2. 카메라 순간 펀치 (시각적 임팩트 분산)
        if (cameraTransform != null)
        {
            cameraTransform.DOKill();
            cameraTransform.DOPunchScale(punchScaleVector, 0.2f, vibrato: 5, elasticity: 0.5f);
        }

        // 3. 소프트 플래시 페이드 인/아웃 (OutExpo로 빛이 자연스럽게 스르륵 사라짐)
        flashSeq.Append(flashImage.DOFade(flashColor.a, flashInDuration).SetEase(Ease.OutQuad));
        flashSeq.Append(flashImage.DOFade(0f, flashOutDuration).SetEase(Ease.OutExpo));

        flashSeq.OnComplete(() =>
        {
            flashImage.gameObject.SetActive(false);
            onComplete?.Invoke();
        });
    }
}