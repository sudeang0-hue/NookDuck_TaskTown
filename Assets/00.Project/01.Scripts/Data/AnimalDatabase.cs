// 모든 동물 AnimalData 를 모아두는 SO

using System.Collections.Generic;
using TaskTown.Gacha;
using UnityEngine;

namespace Animal.Data
{
    [CreateAssetMenu(menuName = "Inventory/Animal/Animal Database", fileName = "AnimalDatabase")]
    public class AnimalDatabase : ScriptableObject
    {
        [Header("전체 동물 데이터 목록")]
        [SerializeField] private List<AnimalDataSO> animals = new List<AnimalDataSO>();

        private Dictionary<string, AnimalDataSO> animalMap;

        public IReadOnlyList<AnimalDataSO> Animals => animals;

        
        public void Initialize()
        {
            animalMap = new Dictionary<string, AnimalDataSO>();

            foreach (AnimalDataSO animal in animals)
            {
                if (animal == null) continue;

                if(string.IsNullOrEmpty(animal.Id))
                {
                    Debug.LogWarning("[AnimalDatabase] ID가 비어있는 AnimalData 가 있습니다.");
                    continue;
                }

                if(animalMap.ContainsKey(animal.Id))
                {
                    Debug.LogWarning($"[AnimalDatabase] 중복 Animal ID 가 있습니다 : {animal.Id}");
                    continue;
                }

                animalMap.Add(animal.Id, animal);
            }
        }

        public bool Contains(string animalId)
        {
            return GetAnimalData(animalId) != null;
        }

        public AnimalDataSO GetAnimalData(string animalId)
        {
            if(animalMap ==  null)
            {
                Initialize();
            }

            if(string.IsNullOrEmpty(animalId))
                return null;

            animalMap.TryGetValue(animalId, out AnimalDataSO animalData);

            return animalData;
        }

        /// <summary>
        /// 등급별 조회 함수
        /// </summary>
        public List<AnimalDataSO> GetAnimalsByGrade(ItemGrade grade)
        {
            if (animalMap == null)
            {
                Initialize();
            }

            List<AnimalDataSO> result = new List<AnimalDataSO>();

            foreach (AnimalDataSO animal in animals)
            {
                if (animal == null)
                    continue;

                if (animal.Grade == grade)
                {
                    result.Add(animal);
                }
            }

            return result;
        }

    }
}