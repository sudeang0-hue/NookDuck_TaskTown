
using UnityEngine;



public class UIController_AnimalPage : MonoBehaviour
{
    [Header("동물 상세 페이지 패널")]
    [SerializeField] private GameObject animalListPagePanel;

    [Header("슬롯이 생성되는 Content Root")]
    [SerializeField] private Transform slotsRoot;


    private void Awake()
    {
        if (animalListPagePanel != null)
            animalListPagePanel.SetActive(false);

        InitializeSlots();
    }

    /// <summary>
    /// 생성된 슬롯들을 배열에 추가
    /// </summary>
    private void InitializeSlots()
    {
        if (slotsRoot == null)
        {
            Debug.LogWarning("[UIController_AnimalPage] slotsRoot가 연결되지 않았습니다.");
            return;
        }

        SlotUI_AnimalList[] animalSlots =
            slotsRoot.GetComponentsInChildren<SlotUI_AnimalList>(true);

        foreach (SlotUI_AnimalList slot in animalSlots)
        {
            if (slot == null)
                continue;

            slot.Initialize(this);
        }

        Debug.Log($"[UIController_AnimalPage] 도감 슬롯 연결 완료: {animalSlots.Length}개");
    }

    /// <summary>
    /// 도감 슬롯 클릭 시 상세 페이지 열기
    /// </summary>
    public void OpenAnimalPage()
    {
        if (animalListPagePanel == null)
        {
            Debug.LogWarning("[UIController_AnimalPage] animalListPagePanel이 연결되지 않았습니다.");
            return;
        }

        animalListPagePanel.SetActive(true);
    }

    /// <summary>
    /// 상세 페이지 닫기 버튼용
    /// </summary>
    public void CloseAnimalPage()
    {
        if (animalListPagePanel == null)
            return;

        animalListPagePanel.SetActive(false);
    }

}
