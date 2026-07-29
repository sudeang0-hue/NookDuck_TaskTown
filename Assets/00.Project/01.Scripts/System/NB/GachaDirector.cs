//NB

using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(CanvasGroup))]
public class GachaDirector : MonoBehaviour
{
    [Header("UI Element References")]
    [SerializeField] private GameObject gachaPanel;
    [SerializeField] private RectTransform imageBox;      // 상자
    [SerializeField] private RectTransform imageItem;     // 도구
    [SerializeField] private RectTransform effectGlow;    // 후광
    [SerializeField] private CanvasGroup panelCanvasGroup;

    [Header("Test Asset")]
    [SerializeField] private Sprite testItemSprite;

    [Header("Simplified Bounce Settings")]
    [SerializeField] private float startXOffset = -1200f; // 왼쪽 화면 바깥 출발점
    [SerializeField] private float firstJumpPower = 600f; // 1차 바운스 높이
    [SerializeField] private float firstJumpDuration = 0.6f;// 1차 바운스 시간

    private Sequence _gachaSequence;
    private Tween _glowRotationTween;
    private bool _isAnimating;

    private void Awake()
    {
        // Null 조건 연산자 캐싱
        panelCanvasGroup ??= GetComponent<CanvasGroup>();

        // UI 연출의 정석: Pivot Center, Anchor Center 세팅
        if (imageBox != null)
        {
            imageBox.anchorMin = imageBox.anchorMax = imageBox.pivot = new Vector2(0.5f, 0.5f);
        }
    }

    public void StartGachaAnimation(Sprite rewardItemSprite)
    {
        if (_isAnimating) return; // 중복 실행 방어
        _isAnimating = true;

        ResetAllTweens();
        gachaPanel.SetActive(true);
        if (panelCanvasGroup != null) panelCanvasGroup.blocksRaycasts = false; // 연출 중 클릭 차단

        // 보상 아이템 세팅
        imageItem.GetComponent<Image>().sprite = rewardItemSprite;
        imageItem.localScale = Vector3.zero;
        if (effectGlow != null) effectGlow.localScale = Vector3.zero;

        // 왼쪽 화면 밖, 기울어진 상태로 출발
        imageBox.anchoredPosition = new Vector2(startXOffset, 200f); // 약간 위에서 던져짐
        imageBox.localScale = Vector3.one;
        imageBox.localRotation = Quaternion.Euler(0f, 0f, 30f); // 30도 기운 채 대기

        _gachaSequence = DOTween.Sequence();

        float pRatio = 0.5f; // 높이 감쇠
        float tRatio = 0.8f; // 시간 감쇠

        // --- 1차 바운스: 왼쪽 -> 중앙 지나침 (오른쪽 지점 착지) ---
        // '휘리릭' 던져지는 느낌을 위해 초반 회전 속도를 높임
        _gachaSequence.Append(imageBox.DOJumpAnchorPos(new Vector2(300f, 0f), firstJumpPower, 1, firstJumpDuration)
                                    .SetEase(Ease.OutQuad)); // 착지감은 OutQuad가 정석
        _gachaSequence.Join(imageBox.DORotate(new Vector3(0f, 0f, -360f), firstJumpDuration, RotateMode.FastBeyond360)
                                   .SetEase(Ease.Linear));

        // --- 2차 바운스: 오른쪽 -> 왼쪽으로 살짝 튕김 ---
        // 물리 감쇠 적용: $P_2 = P_1 \times 0.5$, $T_2 = T_1 \times 0.8$
        float p2 = firstJumpPower * pRatio;
        float t2 = firstJumpDuration * tRatio;
        _gachaSequence.Append(imageBox.DOJumpAnchorPos(new Vector2(-150f, 0f), p2, 1, t2)
                                    .SetEase(Ease.OutQuad));
        // 회전도 감쇠: 반바퀴만
        _gachaSequence.Join(imageBox.DORotate(new Vector3(0f, 0f, -180f), t2, RotateMode.FastBeyond360)
                                   .SetEase(Ease.Linear));

        // --- 3차 바운스: 왼쪽 -> 정중앙(0,0) 안착 ---
        // 최종 안착이므로 Power를 더 줄임. 정방향 회전.
        float p3 = p2 * pRatio * 0.8f; // 더 급격한 감쇠
        float t3 = t2 * tRatio;
        _gachaSequence.Append(imageBox.DOJumpAnchorPos(Vector2.zero, p3, 1, t3)
                                    .SetEase(Ease.OutQuad));
        _gachaSequence.Join(imageBox.DORotate(Vector3.zero, t3).SetEase(Ease.OutQuad)); // 정방향 원복

        // 착지 마무리 & 개봉 (짧고 스냅있게 수정)
        // 쿵! Squash & Stretch (시간 단축하여 탄성 강조)
        _gachaSequence.Append(imageBox.DOScale(new Vector3(1.3f, 0.7f, 1f), 0.08f).SetEase(Ease.OutQuad));
        _gachaSequence.Append(imageBox.DOScale(Vector3.one, 0.05f).SetEase(Ease.OutQuad));

        _gachaSequence.AppendInterval(0.1f); // 찰나의 정적

        // 긴장감 진동 (정신없지 않게 짧게 0.4초)
        _gachaSequence.Append(imageBox.DOShakePosition(0.4f, strength: 20f, vibrato: 40));
        _gachaSequence.Join(imageBox.DOScale(1.2f, 0.4f).SetEase(Ease.InQuad)); // 부풀어 오름

        // 퐝~! 상자 개봉
        _gachaSequence.AppendCallback(() => { imageBox.localScale = Vector3.zero; });

        // 도구 탄성 등장 (가장 도파민 터지는 순간)
        _gachaSequence.Append(imageItem.DOScale(1.0f, 0.6f).SetEase(Ease.OutElastic));

        // 후광 연출 (동시 진행)
        if (effectGlow != null)
        {
            _gachaSequence.Join(effectGlow.DOScale(1.1f, 0.4f).SetEase(Ease.OutBack));
            // 무한 회전은 별도 Tween으로 관리 (Kill Safe)
            _glowRotationTween = effectGlow.DORotate(new Vector3(0, 0, -360), 8f, RotateMode.FastBeyond360)
                                           .SetLoops(-1, LoopType.Incremental).SetEase(Ease.Linear);
        }

        // 연출 종료
        _gachaSequence.OnComplete(() =>
        {
            _isAnimating = false;
            if (panelCanvasGroup != null) panelCanvasGroup.blocksRaycasts = true; // 클릭 다시 허용
            Debug.Log("<color=lime></color> 심플 바운스 연출 완료!");
        });
    }

    public void CloseGachaUI() { ResetAllTweens(); _isAnimating = false; gachaPanel.SetActive(false); }
    private void ResetAllTweens()
    {
        if (_gachaSequence != null && _gachaSequence.IsActive()) { _gachaSequence.Kill(); _gachaSequence = null; }
        if (_glowRotationTween != null && _glowRotationTween.IsActive()) { _glowRotationTween.Kill(); _glowRotationTween = null; }
        imageBox.DOKill(); imageItem.DOKill(); if (effectGlow != null) effectGlow.DOKill();
    }
    private void Update() { if (Input.GetKeyDown(KeyCode.Space)) { if (testItemSprite != null) StartGachaAnimation(testItemSprite); } if (Input.GetKeyDown(KeyCode.Escape)) { if (gachaPanel.activeSelf) CloseGachaUI(); } }
    private void OnDestroy() { ResetAllTweens(); }
}