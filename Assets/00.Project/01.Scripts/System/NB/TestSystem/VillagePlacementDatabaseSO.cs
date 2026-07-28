//NB

using UnityEngine;
using System.Collections.Generic;
using Animal.Data;     // AnimalDataSO 네임스페이스
using TaskTown.Gacha;  // GachaEntryData 네임스페이스

[System.Serializable]
public struct VillageAnimalEntry
{
    [Tooltip("데이터 팀이 작성한 동물 데이터 에셋 (AnimalDataSO)")]
    public AnimalDataSO animalData;

    [Tooltip("마을에 스폰될 관상용 3D 프리팹")]
    public GameObject visualPrefab;
}

[CreateAssetMenu(fileName = "VillagePlacementDB", menuName = "Village/Placement Database")]
public class VillagePlacementDatabaseSO : ScriptableObject
{
    public List<VillageAnimalEntry> entries = new List<VillageAnimalEntry>();

    // 1. AnimalDataSO 에셋 직접 비교로 프리팹 찾기 (가장 안전하고 빠른 방식)
    public GameObject GetVisualPrefab(AnimalDataSO data)
    {
        if (data == null) return null;

        foreach (var entry in entries)
        {
            if (entry.animalData == data)
                return entry.visualPrefab;
        }
        return null;
    }

    // 2. ID(string) 문자열로 프리팹 찾기 (GachaEntryData의 Id 프로퍼티 활용)
    public GameObject GetVisualPrefab(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        foreach (var entry in entries)
        {
            // AnimalDataSO가 GachaEntryData를 상속받았으므로 .Id 로 접근 가능
            if (entry.animalData != null && entry.animalData.Id == id)
                return entry.visualPrefab;
        }
        return null;
    }
}