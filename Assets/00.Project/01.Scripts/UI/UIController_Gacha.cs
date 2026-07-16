using System;
using TaskTown.Gacha;
using UnityEngine;
using UnityEngine.UI;

public class UIController_Gacha : MonoBehaviour
{
    [Header("뽑기 시스템")]
    [SerializeField] private GachaManagerBase animalGachaManager;
    [SerializeField] private GachaManagerBase toolGachaManager;

    [Header("동물 뽑기 버튼")]
    [SerializeField] private Button animalOnePickButton;  // 동물 1회 뽑기 버튼
    [SerializeField] private Button animalTenPickButton;  // 동물 10회 뽑기 버튼
                                                          
    [Header("도구 뽑기 버튼")]
    [SerializeField] private Button toolOnePickButton;    // 도구 1회 뽑기 버튼
    [SerializeField] private Button toolTenPickButton;    // 도구 10회 뽑기 버튼


    private void Awake()
    {
        if(animalGachaManager == null)
        {

        }

        if(toolGachaManager == null)
        {

        }
    }
    private void Start()
    {
        Subscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }
    
    /// <summary>
    /// 클릭 이벤트 구독
    /// </summary>
    private void Subscribe()
    {

        if (animalOnePickButton != null)
            animalOnePickButton.onClick.AddListener(OnClickPickUpAnimalOneTime);

        if (animalTenPickButton != null)
            animalTenPickButton.onClick.AddListener(OnClickPickUpAnimalTenTime);

        if(toolOnePickButton != null)
            toolOnePickButton.onClick.AddListener (OnClickPickUpToolOneTime);

        if (toolTenPickButton != null)
            toolTenPickButton.onClick.AddListener(OnClickPickUpToolTenTime);
    }

    /// <summary>
    /// 클릭 이벤트 구독 해제
    /// </summary>
    private void Unsubscribe()
    {
        if (animalOnePickButton != null)
            animalOnePickButton.onClick.RemoveListener(OnClickPickUpAnimalOneTime);

        if (animalTenPickButton != null)
            animalTenPickButton.onClick.RemoveListener(OnClickPickUpAnimalTenTime);

        if (toolOnePickButton != null)
            toolOnePickButton.onClick.RemoveListener(OnClickPickUpToolOneTime);

        if (toolTenPickButton != null)
            toolTenPickButton.onClick.RemoveListener(OnClickPickUpToolTenTime);
    }

    private void OnClickPickUpAnimalOneTime()
    {
        Debug.Log("[UIController_Gacha] 동물 1회 뽑기 로직 실행 요청");
        // 가챠 시스템의 동물 1회 뽑기와 연결
        animalGachaManager.Roll();
    }


    private void OnClickPickUpAnimalTenTime()
    {
        Debug.Log("[UIController_Gacha] 동물 10회 뽑기 로직 실행 요청");
        // 가챠 시스템의 동물 10회 뽑기와 연결
        animalGachaManager.RollMulti(10);
    }

    private void OnClickPickUpToolOneTime()
    {
        Debug.Log("[UIController_Gacha] 도구 1회 뽑기 로직 실행 요청");
        // 가챠 시스템의 도구 1회 뽑기와 연결
        toolGachaManager.Roll();
    }

    private void OnClickPickUpToolTenTime()
    {
        Debug.Log("[UIController_Gacha] 도구 10회 뽑기 로직 실행 요청");
        // 가챠 시스템의 도구 10회 뽑기와 연결
        toolGachaManager.RollMulti(10);
    }



}
