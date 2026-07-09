// 모든 동물 AnimalData 를 모아두는 SO

using UnityEngine;
using AnimalData = TaskTown.Gacha.AnimalData;
using System.Collections.Generic;

namespace KAY.Inventory
{
    [CreateAssetMenu(menuName = "Inventory/Animal/Animal Database", fileName = "AnimalDatabase")]
    public class AnimalDatabase : ScriptableObject
    {

        [Header("전체 동물 데이터 목록")]
        [SerializeField] private List<TaskTown.Gacha.AnimalData> animals = new List<TaskTown.Gacha.AnimalData>();


    }
}