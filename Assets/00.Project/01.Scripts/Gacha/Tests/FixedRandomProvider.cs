using System.Collections.Generic;

namespace TaskTown.Gacha.Tests
{
    // 테스트에서 GachaSystem의 결과를 예측 가능하게 만들기 위한 가짜 난수 제공자입니다.
    // 지정한 순서대로 값을 반환하고, 값을 다 쓰면 마지막 값을 계속 반환합니다.
    public class FixedRandomProvider : IRandomProvider
    {
        private readonly Queue<float> values;
        private float lastValue;

        public FixedRandomProvider(params float[] sequence)
        {
            values = new Queue<float>(sequence);
        }

        public float NextFloat01()
        {
            if (values.Count > 0)
            {
                lastValue = values.Dequeue();
            }

            return lastValue;
        }
    }
}
