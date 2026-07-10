//NB
using UnityEngine;
using UnityEngine.UI;

public class AnimalCard : MonoBehaviour
{
    [Header("이 카드에 할당할 동물 데이터")]
    public AnimalData myAnimalData;

    [Header("상세창 UI 직접 연결 (에러 방지용)")]
    public AnimalDetailPanel detailPanel;

    [Header("카드 자체 UI 요소")]
    public Image cardIconImage;  // 책장에 보일 동물 얼굴 이미지

    void Start()
    {
        //카드 아이콘 이미지 초기화
        if (myAnimalData != null && cardIconImage != null)
        {
            cardIconImage.sprite = myAnimalData.animalSprite;
        }

    }
    // 카드가 클릭되었을 때 실행되는 함수
    public void OnCardClick()
    {
        if (detailPanel != null && myAnimalData != null)
        {
            // 상세창에 동물 데이터를 넘겨주며 열어달라고 요청
            detailPanel.UpdateAndShowDetails(myAnimalData);
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] 상세창 UI가 인스펙터에서 연결되지 않았거나 동물 데이터가 비어있습니다.");
        }
    }
}