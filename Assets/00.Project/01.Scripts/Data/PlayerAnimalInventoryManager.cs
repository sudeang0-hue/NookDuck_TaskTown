using Animal.Data;
using UnityEngine;

namespace Animal
{

    public class PlayerAnimalInventoryManager : MonoBehaviour
    {
        public static PlayerAnimalInventoryManager Instance { get; private set; }

        [Header("동물 데이터 베이스")]
        [SerializeField] private AnimalDatabase animalDatabase;


    }
}