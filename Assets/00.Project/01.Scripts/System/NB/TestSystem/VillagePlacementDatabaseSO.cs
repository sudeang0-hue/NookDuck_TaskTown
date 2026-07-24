//NB

using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct VillageAnimalEntry
{
    [Tooltip("동물 ID와 동일하게 입력 (예: CAT_01)")]
    public string animalID;

    [Tooltip("마을에 스폰될 관상용 3D 프리팹")]
    public GameObject visualPrefab;
}

[CreateAssetMenu(fileName = "VillagePlacementDB", menuName = "Village/Placement Database")]
public class VillagePlacementDatabaseSO : ScriptableObject
{
    public List<VillageAnimalEntry> entries = new List<VillageAnimalEntry>();

    // ID로 프리팹을 찾아주는 함수
    public GameObject GetVisualPrefab(string id)
    {
        foreach (var entry in entries)
        {
            if (entry.animalID == id)
                return entry.visualPrefab;
        }
        return null;
    }
}