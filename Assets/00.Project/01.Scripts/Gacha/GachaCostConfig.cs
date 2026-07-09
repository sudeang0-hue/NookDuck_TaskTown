using UnityEngine;

namespace TaskTown.Gacha
{
    // 뽑기 1회당 비용입니다. 뽑기 횟수/마을 레벨에 따라 증가하지 않는 고정값입니다.
    [System.Serializable]
    public class GachaCostConfig
    {
        [Min(0)] public long cost = 100;
    }
}
