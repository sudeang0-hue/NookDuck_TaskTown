using System;
using UnityEngine;
using UnityEngine.UI;

public class UIController_Gacha : MonoBehaviour
{
    [Header("동물 뽑기 버튼")]
    [SerializeField] private Button animalOnePickButton;  // 동물 1회 뽑기 버튼
    [SerializeField] private Button animalTenPickButton;  // 동물 10회 뽑기 버튼
                                                          
    [Header("도구 뽑기 버튼")]
    [SerializeField] private Button toolOnePickButton;    // 도구 1회 뽑기 버튼
    [SerializeField] private Button toolTenPickButton;    // 도구 10회 뽑기 버튼


    public event Action OnAnimalOnePick; // 동물 1회 뽑기
    public event Action OnAnimalTenPick; // 동물 10회 뽑기

    public event Action OnToolOnePick;  // 도구 1회 뽑기
    public event Action OnToolTenPick;  // 도구 10회 뽑기


    private void Awake()
    {
        animalOnePickButton.onClick.AddListener(HandleAnimalOneButton);
        animalTenPickButton.onClick.AddListener(HandleAnimalTenButton);
        toolOnePickButton.onClick.AddListener(HandleToolOneButton);
        toolTenPickButton.onClick.AddListener(HandleToolTenButton);
    }


    public void HandleAnimalOneButton()
    {
        Debug.Log("[UIController_Gacha] 동물 1회 뽑기 로직 실행");
        OnAnimalOnePick?.Invoke();
    }

    public void HandleAnimalTenButton()
    {
        Debug.Log("[UIController_Gacha] 동물 10회 뽑기 로직 실행");
        OnAnimalTenPick?.Invoke();
    }

    public void HandleToolOneButton()
    {
        Debug.Log("[UIController_Gacha] 도구 1회 뽑기 로직 실행");
        OnToolOnePick?.Invoke();
    }

    public void HandleToolTenButton()
    {
        Debug.Log("[UIController_Gacha] 도구 10회 뽑기 로직 실행");
        OnToolTenPick?.Invoke();
    }


}
