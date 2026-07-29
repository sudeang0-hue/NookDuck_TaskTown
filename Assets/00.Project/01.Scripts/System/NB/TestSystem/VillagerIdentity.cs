//NB

using UnityEngine;

// 3D 월드에 스폰된 동물 캐릭터가 자신의 Unique Animal ID를 기억하도록 돕는 식별용 컴포넌트
public class VillagerIdentity : MonoBehaviour
{
    [SerializeField] private string animalId;

    //동물의 고유 ID (Read-Only)</summary>
    public string AnimalId => animalId;

    // 동물이 스폰될 때 ID를 주입
    public void Init(string id)
    {
        animalId = id;
    }
}
