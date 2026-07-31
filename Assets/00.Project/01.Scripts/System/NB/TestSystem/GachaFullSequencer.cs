using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 폴라로이드 카메라 동물 뽑기 6단계 연출 스크립트
/// </summary>
public class GachaFullSequencer : MonoBehaviour
{
    [Header("=== 1. Target References ===")]
    [SerializeField] private Transform cameraTransform;   // 폴라로이드 카메라
    [SerializeField] private Transform photoTransform;    // 인쇄될 사진 카드
    [SerializeField] private Image animalImage;           // 사진 속 동물 이미지
    [SerializeField] private CanvasGroup flashOverlay;    // 찰칵 시 화면 전체 플래시

    [Header("=== 2. Particle FX ===")]
    [SerializeField] private ParticleSystem auraParticle;      // 1&6단계: 카메라 빛무리
    [SerializeField] private ParticleSystem flashParticle;     // 3단계: 찰칵 플래시 빛
    [SerializeField] private ParticleSystem photoParticle;     // 5단계: 사진 완성 이펙트
    [SerializeField] private ParticleSystem disappearParticle; // 6단계: 소멸 이펙트

    [Header("=== 3. Positions & Scales ===")]
    [SerializeField] private Vector3 cameraStartPos = new Vector3(0, -500f, 0);
    [SerializeField] private Vector3 cameraCenterPos = Vector3.zero;
    [SerializeField] private Vector3 photoEjectLocalPos = new Vector3(0, -150f, 0); // 카메라 슬롯 아래
    [SerializeField] private Vector3 photoCenterPos = new Vector3(0, 50f, 0);       // 중앙 강조 위치

    private bool isPlaying = false;

#if UNITY_EDITOR
    private void Update()
    {
        // 테스트용 스페이스바 트 리거
        if (Input.GetKeyDown(KeyCode.Space) && !isPlaying)
        {
            StartCoroutine(PlayFullGachaSequence(null));
        }
    }
#endif

    /// <summary>
    /// 6단계 전체 연출 실행 코루틴
    /// </summary>
    public IEnumerator PlayFullGachaSequence(Sprite resultAnimalSprite)
    {
        isPlaying = true;
        InitState(resultAnimalSprite);

        // ==========================================
        // [Step 1] 반짝이는 빛과 함께 카메라 등장!
        // ==========================================
        if (auraParticle) auraParticle.Play();

        Sequence step1Seq = DOTween.Sequence();
        step1Seq.Join(cameraTransform.DOLocalMove(cameraCenterPos, 0.6f).SetEase(Ease.OutBack));
        step1Seq.Join(cameraTransform.DOScale(Vector3.one, 0.6f).SetEase(Ease.OutCubic));
        yield return step1Seq.WaitForCompletion();

        // ==========================================
        // [Step 2] 카메라가 주변을 살피며 초점 맞추기
        // ==========================================
        // 살짝 갸우뚱(Wobble)거리는 초점 연출
        yield return cameraTransform.DOShakeRotation(0.5f, new Vector3(0, 0, 10f), vibrato: 5, randomness: 20f).WaitForCompletion();
        yield return new WaitForSeconds(0.1f);

        // ==========================================
        // [Step 3] 찰칵! 사진 촬영 (플래시 폭발)
        // ==========================================
        if (flashParticle) flashParticle.Play();
        // 화면 순간 번쩍임 (Flash Overlay)
        if (flashOverlay)
        {
            flashOverlay.alpha = 1f;
            flashOverlay.DOFade(0f, 0.3f);
        }
        // 카메라가 순간 쿵 하고 살짝 반동
        cameraTransform.DOPunchScale(new Vector3(0.1f, 0.1f, 0), 0.2f);
        yield return new WaitForSeconds(0.25f);

        // ==========================================
        // [Step 4] 사진이 인쇄되어 나옴
        // ==========================================
        photoTransform.gameObject.SetActive(true);
        // 사진이 카메라 밑 슬롯에서 스르륵 인쇄되어 내려옴
        yield return photoTransform.DOLocalMove(photoEjectLocalPos, 0.7f).SetEase(Ease.OutQuad).WaitForCompletion();

        // ==========================================
        // [Step 5] 사진 완성 및 새로운 동물 등장!
        // ==========================================
        if (photoParticle) photoParticle.Play();

        Sequence step5Seq = DOTween.Sequence();
        // 사진이 화면 중앙으로 이동하며 약간 커짐
        step5Seq.Join(photoTransform.DOLocalMove(photoCenterPos, 0.6f).SetEase(Ease.OutBack));
        step5Seq.Join(photoTransform.DOScale(new Vector3(1.2f, 1.2f, 1f), 0.6f));
        // 실루엣/빈 이미지에서 뽑힌 동물 이미지로 Alpha Fade In
        if (animalImage) step5Seq.Join(animalImage.DOFade(1f, 0.5f));
        yield return step5Seq.WaitForCompletion();

        yield return new WaitForSeconds(0.3f); // 유저가 동물을 확인할 여운 시간

        // ==========================================
        // [Step 6] 카메라는 빛과 함께 사라짐!
        // ==========================================
        if (disappearParticle) disappearParticle.Play();
        if (auraParticle) auraParticle.Stop();

        Sequence step6Seq = DOTween.Sequence();
        step6Seq.Join(cameraTransform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack));
        step6Seq.Join(cameraTransform.GetComponent<CanvasGroup>()?.DOFade(0f, 0.5f));
        yield return step6Seq.WaitForCompletion();

        cameraTransform.gameObject.SetActive(false);
        isPlaying = false;
    }

    /// <summary>
    /// 연출 시작 전 초기화
    /// </summary>
    private void InitState(Sprite animalSprite)
    {
        // 카메라 초기화
        cameraTransform.gameObject.SetActive(true);
        cameraTransform.localPosition = cameraStartPos;
        cameraTransform.localScale = Vector3.zero;

        CanvasGroup cg = cameraTransform.GetComponent<CanvasGroup>();
        if (cg) cg.alpha = 1f;

        // 사진 초기화 (카메라의 자식으로 놓거나 슬롯 위치에 맞춤)
        photoTransform.gameObject.SetActive(false);
        photoTransform.SetParent(cameraTransform);
        photoTransform.localPosition = Vector3.zero; // 인쇄 시작점 (카메라 내부)
        photoTransform.localScale = Vector3.one;

        // 동물 이미지 설정 및 투명화 (Reveal 전)
        if (animalImage)
        {
            if (animalSprite != null) animalImage.sprite = animalSprite;
            Color c = animalImage.color;
            c.a = 0f; // 처음에 비어있다가 5단계에서 짠! 하고 나타남
            animalImage.color = c;
        }

        if (flashOverlay) flashOverlay.alpha = 0f;
    }
}