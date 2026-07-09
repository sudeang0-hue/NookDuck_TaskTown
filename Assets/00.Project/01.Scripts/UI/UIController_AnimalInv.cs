using UnityEngine;
using UnityEngine.UI;

public class UIController_AnimalInv : MonoBehaviour
{

    [SerializeField] private GameObject animalSlotPrefab;   // 슬롯 프리팹
    [SerializeField] private Transform animalSlotContentRoot;  // 해당 슬롯을 추가하는 위치


    private void Start()
    {
        if (animalSlotPrefab == null)
        {
            Debug.LogWarning("[UIController_AnimalInv] animalSlotPrefab 이 없습니다.");
            return;
        }

        if (animalSlotContentRoot == null)
        {
            Debug.LogWarning("[UIController_AnimalInv] animalSlotContentRoot 이 없습니다.");
            return;
        }
    }

    private void Update()
    {
        // 디버깅용: 숫자키 1을 누르면 동물 슬롯 추가
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            AddAnimalSlot();
        }
    }


    private void AddAnimalSlot()
    {

        if (animalSlotPrefab == null)
        {
            Debug.LogWarning("[UIController_AnimalInv] animalSlotPrefab이 연결되지 않았습니다.");
            return;
        }

        if (animalSlotContentRoot == null)
        {
            Debug.LogWarning("[UIController_AnimalInv] animalSlotContentRoot가 연결되지 않았습니다.");
            return;
        }

        Instantiate(animalSlotPrefab, animalSlotContentRoot);

        Debug.Log("[UIController_AnimalInv] 새로운 동물을 획득하면 슬롯 추가");

    }


}
