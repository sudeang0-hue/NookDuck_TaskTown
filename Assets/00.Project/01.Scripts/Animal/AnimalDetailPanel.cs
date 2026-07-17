//NB
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class AnimalDetailPanel : MonoBehaviour
{
    [Header("상세 정보 UIController_AnimalInvPage 요소들")]
    public Image profileFrameImage;  //동물 이미지 전용
    public Text nameText;            // 이름 텍스트
    public Text descriptionText;     // 설명란 텍스트
    public Text speedText;           // 생산량 등 추가 스펙 텍스트
    public Image[] starImages;       // 별 오브젝트들을 담아둘 배열 (최대 5개)

    [Header("백그라운드 차단 (에러 방지용)")]
    public Image notepadBackground;

    private void Awake()
    {
        // 시작할 때 상세창을 잠시 꺼둠
        gameObject.SetActive(false);
    }
    // 카드를 클릭했을 때 데이터를 전달받아 화면을 갱신하는 함수
    public void UpdateAndShowDetails(AnimalData data)
    {
        if (data == null) return;

        // 상세창 켜기
        gameObject.SetActive(true);

        //텍스트 및 프로필 이미지 데이터 매핑
        if (profileFrameImage != null) profileFrameImage.sprite = data.animalSprite;
        if (nameText != null) nameText.text = data.animalName;
        if (descriptionText != null) descriptionText.text = data.description;
        if (speedText != null) speedText.text = $"초당 생산량: {data.goldPerSecond} Gold";

        // 별 개수 UIController_AnimalInvPage 처리
        for (int i = 0; i < starImages.Length; i++)
        {
            if (i < data.rarityStars)
                starImages[i].gameObject.SetActive(true);  // 별 켜기
            else
                starImages[i].gameObject.SetActive(false); // 남는 별 끄기
        }

        //노트가 켜질 때 살짝 '통통' 튀는 효과
        transform.DOKill(); // 기존 트윈 초기화
        transform.localScale = Vector3.one * 0.95f; // 살짝 작아졌다가
        transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack); // 뿅 하고 커짐
    }
}