using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using TaskTown.Gacha; // 팀원분이 만드신 GachaEntryData 참조용

// [ECHO TD] 네임스페이스 충돌 방지를 위해 글로벌 네임스페이스로 배치되었습니다.
[RequireComponent(typeof(CanvasGroup))]
public class AnimalGachaDirector : MonoBehaviour
{
    [Header("★ 1. Glow Controller Reference ★")]
    [Tooltip("에셋 없이 빛무리 효과를 제어하는 GachaLightGlow 컴포넌트를 연결하세요.")]
    [SerializeField] private GachaLightGlow lightGlowController;

    [Header("★ 2. Camera & Flash Elements ★")]
    [SerializeField] private RectTransform cameraRect;
    [SerializeField] private CanvasGroup flashCanvasGroup; // 화면 전체 발광 패널

    [Header("★ 3. Polaroid Printing Elements ★")]
    [SerializeField] private RectTransform polaroidCardRect; // 인화되어 나오는 폴라로이드 사진 틀
    [SerializeField] private Image animalPhotoImage;         // 사진 내부 동물 일러스트
    [SerializeField] private CanvasGroup photoCanvasGroup;   // 인화 서스펜스용 (Alpha 0 -> 1)
    [SerializeField] private TextMeshProUGUI animalNameText; // 사진 하단 동물 이름 텍스트

    [Header("★ 4. Result Popup Grid ★")]
    [SerializeField] private GameObject resultPopupPanel;
    [SerializeField] private Transform itemGridParent;
    [SerializeField] private GameObject itemSlotPrefab; // 폴라로이드 스타일의 GachaItemSlotUI 프리팹

    [Header("★ Animation Timing Settings ★")]
    [SerializeField] private float cameraEmergeDuration = 0.5f;
    [SerializeField] private float flashDuration = 0.2f;
    [SerializeField] private float photoEjectDuration = 0.6f;
    [SerializeField] private float photoDevelopDuration = 1.0f;

    // DOTween 시퀀스 및 메모리 풀링 캐시 (GC Alloc 방지)
    private Sequence _mainGachaSequence;
    private bool _isAnimating;
    private readonly List<GameObject> _spawnedSlotPool = new List<GameObject>();

    private void Awake()
    {
        ResetAllUIStates();
    }

    /// <summary>
    /// 동물 가챠 폴라로이드 연출 메인 진입점
    /// </summary>
    /// <param name="drawCount">뽑기 횟수</param>
    /// <param name="resultEntries">획득한 동물 데이터 리스트</param>
    public void StartAnimalGachaSequence(int drawCount, List<GachaEntryData> resultEntries)
    {
        if (_isAnimating) return;
        _isAnimating = true;

        ResetTweens();
        ResetAllUIStates();

        gameObject.SetActive(true);

        _mainGachaSequence = DOTween.Sequence();

        // =========================================================================
        // [STEP 1] 빛무리(Glow) 효과 터뜨리기
        // =========================================================================
        if (lightGlowController != null)
        {
            _mainGachaSequence.AppendCallback(() =>
            {
                // GachaLightGlow의 Burst 연출 실행!
                lightGlowController.PlayGlowBurst(); //[cite: 11]
            });
        }

        // =========================================================================
        // [STEP 2] 빛무리 속에서 폴라로이드 카메라 등장 (Scale & Slide)
        // =========================================================================
        _mainGachaSequence.AppendInterval(0.2f); // Glow 발광 피크 타임에 맞춤
        _mainGachaSequence.AppendCallback(() => cameraRect.gameObject.SetActive(true));
        _mainGachaSequence.Append(cameraRect.DOAnchorPosY(0f, cameraEmergeDuration).SetEase(Ease.OutBack));
        _mainGachaSequence.Join(cameraRect.DOScale(Vector3.one, cameraEmergeDuration).SetEase(Ease.OutBack));

        // 카메라 기웃거리는 유쾌한 반동 연출 (Visual Polish)
        _mainGachaSequence.Append(cameraRect.DORotate(new Vector3(0f, 0f, -5f), 0.15f));
        _mainGachaSequence.Append(cameraRect.DORotate(new Vector3(0f, 0f, 0f), 0.15f));

        // =========================================================================
        // [STEP 3] 찰칵! 셔터 플래시 발광 (Screen Flash)
        // =========================================================================
        _mainGachaSequence.AppendCallback(() => flashCanvasGroup.gameObject.SetActive(true));
        _mainGachaSequence.Append(flashCanvasGroup.DOFade(1f, flashDuration * 0.3f).SetEase(Ease.OutQuad));

        // 찰칵 순간 카메라 살짝 뒤로 튕김 (셔터 반동)
        _mainGachaSequence.Join(cameraRect.DOPunchScale(new Vector3(0.15f, 0.15f, 0f), flashDuration, vibrato: 5));

        _mainGachaSequence.Append(flashCanvasGroup.DOFade(0f, flashDuration * 0.7f).SetEase(Ease.InQuad));

        // =========================================================================
        // [STEP 4] 폴라로이드 사진 배출 (Printing) & 서서히 인화 (Developing)
        // =========================================================================
        _mainGachaSequence.AppendCallback(() =>
        {
            if (resultEntries != null && resultEntries.Count > 0)
            {
                // 첫 번째 동물의 데이터 바인딩
                GachaEntryData firstEntry = resultEntries[0];
                if (animalPhotoImage != null) animalPhotoImage.sprite = firstEntry.Icon;
                if (animalNameText != null) animalNameText.text = firstEntry.DisplayName;
            }

            polaroidCardRect.gameObject.SetActive(true);
        });

        // 카메라 슬롯 아래로 사진 슬라이딩
        _mainGachaSequence.Append(polaroidCardRect.DOAnchorPosY(-250f, photoEjectDuration).SetEase(Ease.OutCubic));
        _mainGachaSequence.Join(polaroidCardRect.DOScale(Vector3.one, photoEjectDuration).SetEase(Ease.OutBack));

        // 사진 속 동물이 검은 서스펜스에서 점점 선명하게 인화됨 (Alpha 0 -> 1)
        _mainGachaSequence.Append(photoCanvasGroup.DOFade(1f, photoDevelopDuration).SetEase(Ease.Linear));

        // =========================================================================
        // [STEP 5] 최종 결과 팝업창으로 자연스러운 전환
        // =========================================================================
        _mainGachaSequence.AppendInterval(0.5f);
        _mainGachaSequence.AppendCallback(() =>
        {
            ShowFinalResultPopup(drawCount, resultEntries);
        });

        _mainGachaSequence.OnComplete(() =>
        {
            _isAnimating = false;
            Debug.Log("<color=cyan>[AnimalGachaDirector]</color> 폴라로이드 입주 연출이 완료되었습니다.");
        });
    }

    /// <summary>
    /// 최종 결과 팝업창 슬롯 바인딩 및 생성
    /// </summary>
    private void ShowFinalResultPopup(int drawCount, List<GachaEntryData> resultEntries)
    {
        if (resultPopupPanel != null) resultPopupPanel.SetActive(true);

        // 기존 생성된 슬롯 풀 비활성화 (오브젝트 재활용 최적화)
        for (int i = 0; i < _spawnedSlotPool.Count; i++)
        {
            _spawnedSlotPool[i].SetActive(false);
        }

        int targetCount = (resultEntries != null && resultEntries.Count > 0) ? resultEntries.Count : drawCount;

        for (int i = 0; i < targetCount; i++)
        {
            GameObject slotObj = (i < _spawnedSlotPool.Count)
                ? _spawnedSlotPool[i]
                : Instantiate(itemSlotPrefab, itemGridParent);

            if (!_spawnedSlotPool.Contains(slotObj)) _spawnedSlotPool.Add(slotObj);

            slotObj.SetActive(true);

            GachaItemSlotUI slotUI = slotObj.GetComponent<GachaItemSlotUI>();
            if (slotUI != null && resultEntries != null && i < resultEntries.Count)
            {
                slotUI.SetItem(resultEntries[i]);
            }

            // 시차 팝업 연출: T_delay(i) = 0.1s + (0.05s * i)
            slotObj.transform.localScale = Vector3.zero;
            slotObj.transform.DOScale(Vector3.one, 0.3f)
                   .SetDelay(0.1f + (0.05f * i))
                   .SetEase(Ease.OutBack);
        }
    }

    /// <summary>
    /// UI 초기 상태 정단화 (Reset)
    /// </summary>
    private void ResetAllUIStates()
    {
        if (lightGlowController != null) lightGlowController.ResetGlowState(); //[cite: 11]

        if (cameraRect != null)
        {
            cameraRect.anchoredPosition = new Vector2(0f, 600f); // 화면 상단 대기 위치
            cameraRect.localScale = Vector3.one * 0.3f;
            cameraRect.rotation = Quaternion.identity;
            cameraRect.gameObject.SetActive(false);
        }

        if (flashCanvasGroup != null)
        {
            flashCanvasGroup.alpha = 0f;
            flashCanvasGroup.gameObject.SetActive(false);
        }

        if (polaroidCardRect != null)
        {
            polaroidCardRect.anchoredPosition = Vector2.zero; // 카메라 내부 위치
            polaroidCardRect.localScale = Vector3.one * 0.5f;
            polaroidCardRect.gameObject.SetActive(false);
        }

        if (photoCanvasGroup != null) photoCanvasGroup.alpha = 0f;
        if (resultPopupPanel != null) resultPopupPanel.SetActive(false);
    }

    /// <summary>
    /// 진행 중인 모든 DOTween 시퀀스 및 트윈 안전 종료
    /// </summary>
    private void ResetTweens()
    {
        _mainGachaSequence?.Kill();
        if (cameraRect != null) cameraRect.DOKill();
        if (flashCanvasGroup != null) flashCanvasGroup.DOKill();
        if (polaroidCardRect != null) polaroidCardRect.DOKill();
        if (photoCanvasGroup != null) photoCanvasGroup.DOKill();
    }

    private void OnDisable() => ResetTweens();
    private void OnDestroy() => ResetTweens();
}