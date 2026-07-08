using UnityEngine;

[CreateAssetMenu(fileName = "NewAnimalData", menuName = "Gacha/Animal Data")]
public class AnimalData : ScriptableObject
{
    public string animalName;     // 동물 이름
    public Sprite animalSprite;   // 동물 이미지
    public int rarityStars;       // 별 개수 (1~5개)

    [TextArea(3, 5)]
    public string description;    // 동물 도감 설명글
    public int goldPerSecond;     // 초당 골드 생산량
}