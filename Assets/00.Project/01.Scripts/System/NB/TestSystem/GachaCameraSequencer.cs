using System;
using UnityEngine;
using UnityEngine.UI; // UI Image 제어를 위해 추가
using DG.Tweening;

/// <summary>
/// 뽑기 연출 중 폴라로이드 카메라 등장 및 플래시 연출을 전담하는 클래스
/// </summary>
public class GachaCameraSequencer : MonoBehaviour
{
    [Header("Target UI/3D Object")]
    [SerializeField] private Transform cameraTransform;

    [Header("[New] Flash UI Settings")]
    // 화면 전체를 덮는 흰색 Image 오브젝트에 CanvasGroup을 붙여서 사용합니다.
    [SerializeField] private CanvasGroup flashOverlayCanvasGroup;
    [SerializeField] private float flashInDuration = 0.05f;  // 순식간에 밝아지는 시간
    [SerializeField] private float flashOutDuration = 0.4f;  // 서서히 어두워지는 시간

    [Header("Transform Settings")]
    [SerializeField] private Vector3 startPosition = new Vector3(0, -600f, 0);
    [SerializeField] private Vector3 targetPosition = Vector3.zero;
    [SerializeField] private Vector3 startScale = Vector3.zero;
    [SerializeField] private Vector3 targetScale = Vector3.one;

    [Header("FX Settings")]
    [SerializeField] private ParticleSystem auraParticle;  // 지속적인 빛무리
    [SerializeField] private ParticleSystem burstParticle; // 도착 시 터지는 일반 폭죽
    // [New] 카메라 렌즈 바로 앞에서 팡! 터지는 단발성 강력한 빛 파티클
    [SerializeField] private ParticleSystem flashParticle;

    [Header("Animation Curves & Timings")]
    [SerializeField] private float duration = 0.8f;
    [SerializeField] private Ease moveEase = Ease.OutBack;
    [SerializeField] private Ease scaleEase = Ease.OutCubic;

    private bool isPlaying = false;

#if UNITY_EDITOR
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isPlaying)
            {
                Debug.Log("[ECHO-Debug] 연출 중복 입력 감지 -> 카메라 리셋 후 재실행합니다.");
                ResetCamera();
            }

            Debug.Log("[ECHO-Debug] Spacebar 입력: 카메라 등장 + 플래시 연출 테스트 실행!");
            PlayAppearSequence(() =>
            {
                Debug.Log("[ECHO-Debug] 전체 연출 완료 콜백 수신!");
            });
        }
    }
#endif

    public void PlayAppearSequence(Action onComplete = null)
    {
        if (isPlaying) return;
        isPlaying = true;

        // 1. 초기 상태 세팅 (플래시 포함)
        InitAppearState();

        // 2. DOTween Sequence 구성을 통해 연출 동기화
        Sequence cameraSequence = DOTween.Sequence();
        cameraTransform.DOKill();

        // [New] Sequence 구조 변경: 이동/스케일 트윈 배치
        cameraSequence.Join(cameraTransform.DOLocalMove(targetPosition, duration).SetEase(moveEase));
        cameraSequence.Join(cameraTransform.DOScale(targetScale, duration).SetEase(scaleEase));

        // [New] 중요! 트윈이 완료되는 순간(Append) 플래시 트리거
        cameraSequence.AppendCallback(() =>
        {
            TriggerFlashEffect(); // 플래시 팡!
        });

        // 3. 전체 연출 완료 후 콜백 실행
        cameraSequence.OnComplete(() =>
        {
            isPlaying = false;
            onComplete?.Invoke();
        });
    }

    /// <summary>
    /// [New] 등장 연출 시작 전 초기 상태를 설정하는 메서드
    /// </summary>
    private void InitAppearState()
    {
        cameraTransform.localPosition = startPosition;
        cameraTransform.localScale = startScale;
        cameraTransform.gameObject.SetActive(true);

        // 플래시 오버레이 초기화 (투명하게)
        if (flashOverlayCanvasGroup != null)
        {
            flashOverlayCanvasGroup.alpha = 0f;
            flashOverlayCanvasGroup.gameObject.SetActive(false);
        }

        // 파티클 초기화 및 재생
        if (auraParticle != null)
        {
            auraParticle.Clear();
            auraParticle.Play();
        }
        if (burstParticle != null) burstParticle.Stop();
        if (flashParticle != null) flashParticle.Stop();
    }

    /// <summary>
    /// [New] 카메라가 도착했을 때 플래시 및 임팩트 파티클을 터트리는 로직
    /// </summary>
    private void TriggerFlashEffect()
    {
        // 1. 일반 도착 임팩트 파티클 재생
        if (burstParticle != null) burstParticle.Play();

        // 2. [New] 강력한 렌즈 플래시 파티클 재생
        if (flashParticle != null) flashParticle.Play();

        // 3. [New] 화면 오버레이 플래시 연출 (Tween)
        if (flashOverlayCanvasGroup != null)
        {
            flashOverlayCanvasGroup.DOKill(); // 기존 트윈 제거
            flashOverlayCanvasGroup.gameObject.SetActive(true);

            // 플래시 Sequence 구성
            Sequence flashSeq = DOTween.Sequence();
            // 순식간에 알파를 1로 ($0 \to 1$)
            flashSeq.Append(flashOverlayCanvasGroup.DOFade(1f, flashInDuration).SetEase(Ease.OutQuad));
            // 서서히 알파를 0으로 ($1 \to 0$)
            flashSeq.Append(flashOverlayCanvasGroup.DOFade(0f, flashOutDuration).SetEase(Ease.InQuad));

            flashSeq.OnComplete(() =>
            {
                flashOverlayCanvasGroup.gameObject.SetActive(false);
            });
        }
    }

    public void ResetCamera()
    {
        cameraTransform.DOKill();
        if (flashOverlayCanvasGroup != null) flashOverlayCanvasGroup.DOKill();

        cameraTransform.gameObject.SetActive(false);

        if (flashOverlayCanvasGroup != null)
        {
            flashOverlayCanvasGroup.alpha = 0f;
            flashOverlayCanvasGroup.gameObject.SetActive(false);
        }

        if (auraParticle != null) auraParticle.Stop();
        if (burstParticle != null) burstParticle.Stop();
        if (flashParticle != null) flashParticle.Stop();

        isPlaying = false;
    }

    private void OnDrawGizmosSelected()
    {
        // (기존 Gizmos 로직 유지)
        if (cameraTransform == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + startPosition, Vector3.one * 50f);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + targetPosition, Vector3.one * 50f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position + startPosition, transform.position + targetPosition);
    }
}
