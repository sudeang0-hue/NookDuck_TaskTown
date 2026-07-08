using UnityEngine;

namespace TaskTown.Gacha
{
    public class UnityRandomProvider : IRandomProvider
    {
        public float NextFloat01()
        {
            return Random.value;
        }
    }
}
