using System.Collections.Generic;
using Test.UI;
using UnityEngine;

namespace UI
{

    public class UIController_AnimalInv : MonoBehaviour
    {
        [Header("동물 인벤토리")]
        [SerializeField] TestInventory_Animal animalInventory;

        [SerializeField] private SlotUI_AnimalInv animalSlotPrefab;   // 슬롯 프리팹
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

        private void OnEnable()
        {
            if (animalInventory == null)
                return;

            animalInventory.OnAnimalAdded += AddSlot;
            animalInventory.OnAnimalChanged += RefreshSlot;
        }

        private void OnDisable()
        {
            if (animalInventory == null)
                return;

            animalInventory.OnAnimalAdded -= AddSlot;
            animalInventory.OnAnimalChanged -= RefreshSlot;
        }


        public void AddSlot(string animalId)
        {
            
        }

        private void RefreshSlot(string animalId)
        {
            
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


}
