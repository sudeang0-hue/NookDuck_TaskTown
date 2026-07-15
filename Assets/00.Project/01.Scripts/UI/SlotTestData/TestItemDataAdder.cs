using Animal.Data;
using Tool.Data;
using TaskTown.KDH;
using UI;
using UnityEngine;

namespace Test
{

    public class TestItemDataAdder : MonoBehaviour
    {
        [Header("µ¿¹° È¹µæ")]
        [SerializeField] private InventoryManager_Animal animalinventory;
        [SerializeField] private UIController_AnimalInv animalinventoryui;
        [SerializeField] private AnimalDataSO[] testAnimals;

        [Header("µµ±¸ È¹µæ")]
        [SerializeField] private InventoryManager_Tool toolinventory;
        [SerializeField] private UIController_ToolInv toolinventoryui;
        [SerializeField] private ToolDataSO[] testTools;


        private void Start()
        {
            if (animalinventory == null)
            {
                animalinventory = InventoryManager_Animal.Instance;
            }
            if (toolinventory == null)
            {
                toolinventory = InventoryManager_Tool.Instance;
            }

            PrintKeyInfo();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                AddSlot();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                AddSlot();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                AddSlot();
            }

            else if (Input.GetKeyDown(KeyCode.F1))
            {
                AddSlot();
            }
            else if (Input.GetKeyDown(KeyCode.F2))
            {
                AddSlot();
            }
            else if(Input.GetKeyDown(KeyCode.F3))
            {
                AddSlot();
            }
            else
            {
                return;
            }
            
        }

        private void PrintKeyInfo()
        {
            Debug.Log(
                "µ¿¹° È¹µæ 3Á¾ : 1¹øÅ°, 2¹øÅ°, 3¹øÅ° / µµ±¸ È¹µæ 3Á¾ : F1, F2, F3");
        }

        private void AddSlot()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                animalinventory.AddAnimalSlot(testAnimals[0]);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                animalinventory.AddAnimalSlot(testAnimals[1]);
            }
            else if(Input.GetKeyDown(KeyCode.Alpha3))
            {
                animalinventory.AddAnimalSlot(testAnimals[2]);
            }

            else if(Input.GetKeyDown(KeyCode.F1))
            {
                toolinventory.AddToolSlot(testTools[0]);
            }
            else if (Input.GetKeyDown(KeyCode.F2))
            {
                toolinventory.AddToolSlot(testTools[1]);
            }
            else
            {
                toolinventory.AddToolSlot(testTools[2]);
            }
        }


    }
}