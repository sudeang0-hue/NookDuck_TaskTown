using System;
using System.Collections.Generic;

namespace TaskTown.SceneFlow
{
    /// <summary>
    /// Step 사이에서 파일 원본, 저장 DTO, 검증 결과 등을 순서대로 전달합니다.
    /// 임시 데이터는 Set/TryGet을 사용하고 최종 Runtime 데이터는 GameSession에 기록합니다.
    /// </summary>
    public sealed class StartupLoadContext
    {
        private readonly Dictionary<Type, object> temporaryData = new();

        public StartupLoadContext(SceneId targetScene)
        {
            TargetScene = targetScene;
            GameSession = new GameSession();
        }

        public SceneId TargetScene { get; }
        public GameSession GameSession { get; }

        public void Set<T>(T data) where T : class
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            temporaryData[typeof(T)] = data;
        }

        public bool TryGet<T>(out T data) where T : class
        {
            if (temporaryData.TryGetValue(typeof(T), out object value) && value is T typedValue)
            {
                data = typedValue;
                return true;
            }

            data = null;
            return false;
        }
    }
}
