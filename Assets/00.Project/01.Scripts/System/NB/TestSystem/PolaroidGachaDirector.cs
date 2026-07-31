using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class PolaroidGachaDirector : MonoBehaviour
{
    [Header("★ Camera & Mask References ★")]
    [SerializeField] private RectTransform cameraTransform; // 카메라 RectTransform
    [SerializeField] private RectMask2D slotMask;           // 슬롯 구멍 마스크 컴포넌트
    [SerializeField] private RectTransform photoCardRect;   // 인화될 사진 카드 RectTransform
    [SerializeField] private CanvasGroup photoCanvasGroup;   // 사진 카드 CanvasGroup

    [Header("★ Result Popup References ★")]
    [SerializeField] private GameObject resultPopupPanel;   // 결과창 패널
    [SerializeField] private RectTransform resultCenterTarget; // 결과창 중앙 목표 위치

    [Header("★ Photo Card Inner Data ★")]
    [SerializeField] private Image animalPhotoImage;         // 카드 안 동물 이미지
    [SerializeField] private TextMeshProUGUI animalNameText; // 카드 안 동물 이름

    // 연출 제어용 시퀀스 객체
    private Sequence _polaroidSequence;

    /// <summary>
    /// 폴라로이드 가챠 연출 시작 함수
    /// </summary>
    /// <param name="animalSprite">뽑힌 동물 이미지</param>
    /// <param name="animalName">뽑힌 동물 이름</param>
    /// <param name="onComplete">연출 종료 후 콜백</param>
    public void PlayPolaroidEjectSequence(Sprite animalSprite, string animalName, Action onComplete = null)
    {
        // 1. 이전 트윈 안전하게 제거 및 데이터 바인딩
        KillSequence();

        if (animalPhotoImage != null) animalPhotoImage.sprite = animalSprite;
        if (animalNameText != null) animalNameText.text = animalName;

        // 2. 초기 상태 셋팅 (슬롯 구멍 안쪽에 은밀히 배치)
        slotMask.enabled = true; // 마스크 활성화 (구멍 밖으로 나가는 부분만 보이게)

        // 마스크 기준 슬롯 내부 시작 위치 설정 (Y축으로 위쪽에 숨겨둠)
        photoCardRect.anchoredPosition = new Vector2(0f, 150f);
        photoCardRect.localRotation = Quaternion.identity;
        photoCardRect.localScale = Vector3.one * 0.8f; // 약간 작은 크기
        photoCanvasGroup.alpha = 1f;

        // 3. DOTween Sequence 연출 조립
        _polaroidSequence = DOTween.Sequence();

        // ----------------------------------------------------
        // [Phase 1: 쭈욱~ 슬롯에서 인화되어 내려오기]
        // ----------------------------------------------------
        _polaroidSequence.Append(
            photoCardRect.DOAnchorPosY(-80f, 0.8f) // 슬롯 밖으로 쑥 빠져나옴
                .SetEase(Ease.OutCubic)
        );
        _polaroidSequence.AppendInterval(0.15f); // 잠깐 멈칫하는 폴라로이드 감성 딜레이

        // ----------------------------------------------------
        // [Phase 2: 바깥으로 휘리릭~ 날아가기 (마스크 탈출 & 공중 회전)]
        // ----------------------------------------------------
        _polaroidSequence.AppendCallback(() =>
        {
            // ★ 핵심 트릭: 마스크 제약을 풀어서 화면 어디든 자유롭게 날아다니게 만듭니다!
            slotMask.enabled = false;
        });

        // [★ CS1503 오류 수정 지점 ★]
        // Vector2를 Vector3로 변환하여 DOPath()의 Vector3[] 인수 요구사항을 완벽히 충족시킵니다.
        Vector3 p0 = photoCardRect.anchoredPosition; // Vector2 -> Vector3 암시적 형변환
        Vector3 p1 = new Vector3(p0.x + 400f, p0.y + 350f, 0f); // 우측 상단 공중 궤적 정점
        Vector3 p2 = Vector3.zero; // 화면 중앙 도착점

        // DOTween DOPath 사양에 맞춘 Vector3[] 배열 선언
        Vector3[] pathPoints = new Vector3[] { p0, p1, p2 };

        // 베지에 곡선 이동 + 360도 회전 + 스케일 팝업을 동시 진행(Join)
        _polaroidSequence.Append(
            photoCardRect.DOPath(pathPoints, 0.75f, PathType.CatmullRom)
                .SetEase(Ease.OutQuad)
        );
        _polaroidSequence.Join(
            photoCardRect.DORotate(new Vector3(0f, 0f, -360f), 0.75f, RotateMode.FastBeyond360)
                .SetEase(Ease.OutCubic)
        );
        _polaroidSequence.Join(
            photoCardRect.DOScale(1.3f, 0.75f) // 공중에서 커졌다가
                .SetEase(Ease.OutBack)
        );

        // ----------------------------------------------------
        // [Phase 3: 화면 위에서 가운데로 스르륵 내려오며 결과창 바인딩]
        // ----------------------------------------------------
        _polaroidSequence.Append(
            photoCardRect.DOScale(1.0f, 0.3f) // 정크기 복원
                .SetEase(Ease.InQuad)
        );
        _polaroidSequence.Join(
            photoCardRect.DORotate(Vector3.zero, 0.3f) // 회전 정방향 정렬
        );

        // 결과창 패널 켜기 및 최종 바운스
        _polaroidSequence.AppendCallback(() =>
        {
            if (resultPopupPanel != null) resultPopupPanel.SetActive(true);
        });

        _polaroidSequence.Append(
            photoCardRect.DOPunchScale(new Vector3(0.15f, 0.15f, 0f), 0.35f, vibrato: 5, elasticity: 0.5f)
        );

        // 연출 완료 처리
        _polaroidSequence.OnComplete(() =>
        {
            Debug.Log("<color=lime>[PolaroidSequence]</color> 사진 인화 및 안착 연출 완료!");
            onComplete?.Invoke();
        });
    }

    private void KillSequence()
    {
        if (_polaroidSequence != null && _polaroidSequence.IsActive())
        {
            _polaroidSequence.Kill();
            _polaroidSequence = null;
        }
    }

    private void OnDisable() => KillSequence();
    private void OnDestroy() => KillSequence();
}