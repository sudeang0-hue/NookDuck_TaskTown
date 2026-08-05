using UnityEngine;

[CreateAssetMenu(fileName = "NewIslandData", menuName = "Scriptable Objects/Island Data")]
public class IslandData : ScriptableObject
{
    [Header("섬 정보")]
    public int levelRequirement = 1;      // 해당 섬이 활성화되는 레벨
    public string islandName = "기본 섬";  // 섬 이름
    public GameObject islandPrefab;       // Tripo3D로 만든 섬 프리팹

    [Header("변환 오프셋")]
    public Vector3 spawnPositionOffset = Vector3.zero; // 위치 보정값
    public Vector3 spawnRotationOffset = Vector3.zero; // 회전 보정값
}
