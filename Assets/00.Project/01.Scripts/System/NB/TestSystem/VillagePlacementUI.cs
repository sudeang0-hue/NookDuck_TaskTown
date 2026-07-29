//NB

using Animal.Data;
using UnityEngine;
using UnityEngine.UI;

public class VillagePlacementUI : MonoBehaviour
{
    [SerializeField] private VillagerPlacementDirector placementDirector;
    [SerializeField] private Button placementButton;

    private void Update()
    {
        // [UX 꿀팁] 버스 연출 중일 때는 버튼 클릭을 비활성화하여 유저 착오 방지
        if (placementButton != null && placementDirector != null)
        {
            placementButton.interactable = !placementDirector.IsBusSummoning;
        }
    }

    // UI 버튼의 OnClick() 이벤트에 이 함수를 연결합니다.
    public void OnClick_PlaceSelectedAnimal(AnimalDataSO selectedAnimal)
    {
        if (placementDirector != null)
        {
            placementDirector.StartBusSummon(selectedAnimal);
        }
    }
}