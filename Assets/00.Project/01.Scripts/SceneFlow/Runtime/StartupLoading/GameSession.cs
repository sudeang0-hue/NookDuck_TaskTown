using System;
using System.Collections.Generic;

namespace TaskTown.SceneFlow
{
    /// <summary>
    /// 검증된 저장 데이터를 Runtime 시스템으로 전달하는 세션입니다.
    /// 팀별 Snapshot 타입을 Set/TryGet으로 연결할 수 있습니다.
    /// </summary>
    public sealed class GameSession
    {
        private readonly Dictionary<Type, object> runtimeData = new();

        public bool IsReady { get; private set; }

        public void Set<T>(T data) where T : class
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (IsReady)
            {
                throw new InvalidOperationException("준비가 끝난 GameSession에는 데이터를 추가할 수 없습니다.");
            }

            runtimeData[typeof(T)] = data;
        }

        public bool TryGet<T>(out T data) where T : class
        {
            if (runtimeData.TryGetValue(typeof(T), out object value) && value is T typedValue)
            {
                data = typedValue;
                return true;
            }

            data = null;
            return false;
        }

        internal void MarkReady()
        {
            IsReady = true;
        }
    }
}
