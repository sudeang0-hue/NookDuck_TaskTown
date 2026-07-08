using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class GachaDirector : MonoBehaviour
{
    [Header("가챠 연출 UI 요소들")]
    public GameObject gachaPanel;       // 가챠 전체 패널
    public RectTransform imageBox;      // 상자 이미지
    public RectTransform imageItem;     // 도구 이미지
    public RectTransform effectGlow;    // 뒤에 빛나는 배경 이펙트

    [Header("테스트용 아이템 이미지")]
    public Sprite testItemSprite; // 인스펙터에서 테스트할 도구 이미지를 넣어두는 곳

    // 재생 중인 시퀀스를 제어하기 위한 참조 변수
    private Sequence gachaSeq;

    public void StartGachaAnimation(Sprite rewardItemSprite)
    {
        // 연출 시작 전, 기존에 혹시 돌아가고 있던 트윈이 있다면 깔끔하게 청소
        ResetAllTweens();

        // 0. 초기 세팅 (모든 요소를 대기 상태로)
        gachaPanel.SetActive(true);

        imageItem.GetComponent<Image>().sprite = rewardItemSprite; // 보상 이미지 교체
        imageItem.localScale = Vector3.zero;                       // 아이템은 숨김
        if (effectGlow) effectGlow.localScale = Vector3.zero;       // 빛도 숨김

        // 상자를 화면 저 위쪽 높은 곳으로 배치
        imageBox.localScale = Vector3.one;
        imageBox.anchoredPosition = new Vector2(0f, 1200f);

        gachaSeq = DOTween.Sequence();

        // 1단계: 하늘에서 쿵 떨어지기 (바닥인 Y=0 으로, 통통 튀며 등장)
        gachaSeq.Append(imageBox.DOAnchorPosY(0f, 0.6f).SetEase(Ease.OutBounce));

        // 떨어지고 나서 0.2초간 긴장감 조성 대기
        gachaSeq.AppendInterval(0.2f);

        // 2단계: 2.5초 동안 상자가 부들부들 떨리기 (지진 효과)
        gachaSeq.Append(imageBox.DOShakePosition(2.5f, new Vector3(20f, 0f, 0f), 30));

        // 3단계: 팡! 하고 상자 파괴 및 도구 등장
        gachaSeq.AppendCallback(() => {
            imageBox.localScale = Vector3.zero;
        });

        // 4단계: 안에서 도구가 튀어나오기 (크기가 0에서 1로 튕기며 커짐)
        gachaSeq.Append(imageItem.DOScale(1f, 0.4f).SetEase(Ease.OutBack));

        // 5단계: (선택사항) 뒤에 후광 이펙트가 스윽 커지면서 은은하게 돌기
        if (effectGlow != null)
        {
            gachaSeq.Join(effectGlow.DOScale(1f, 0.5f).SetEase(Ease.OutQuad));
            // 빛무리는 무한히 뱅글뱅글 돌기
            effectGlow.DORotate(new Vector3(0, 0, 360), 8f, RotateMode.FastBeyond360)
                      .SetLoops(-1, LoopType.Incremental)
                      .SetEase(Ease.Linear);
        }
    }

    // 가챠 UI를 닫고 진행 중인 모든 트윈을 안전하게 끄는 함수
    public void CloseGachaUI()
    {
        ResetAllTweens();
        gachaPanel.SetActive(false);
    }

    // 가챠 관련 오브젝트들의 DOTween 연출을 초기화/제거하는 헬퍼 함수
    private void ResetAllTweens()
    {
        // 실행 중인 시퀀스가 있다면 처분
        if (gachaSeq != null && gachaSeq.IsActive())
        {
            gachaSeq.Kill();
        }

        // 각 컴포넌트에 개별적으로 걸려있는 트윈(특히 무한 루프 회전)도 강제 종료
        imageBox.DOKill();
        imageItem.DOKill();
        if (effectGlow != null)
        {
            effectGlow.DOKill();
        }
    }

    void Update()
    {
        // 1. 스페이스바를 누르면 가챠 연출 시작
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (testItemSprite != null)
            {
                StartGachaAnimation(testItemSprite);
            }
            else
            {
                Debug.LogWarning("testItemSprite에 이미지를 넣어주세요!");
            }
        }

        // 2. ESC를 누르면 가챠 UI 즉시 종료
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // UI 패널이 켜져 있을 때만 작동하도록 방어 코드를 넣어두면 좋음
            if (gachaPanel.activeSelf)
            {
                CloseGachaUI();
            }
        }
    }
}