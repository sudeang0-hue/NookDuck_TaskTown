using System;
using UnityEngine;

namespace TaskTown.SceneFlow
{
    /// <summary>
    /// 실행 중인 Step의 진행률과 결과를 Pipeline에 전달합니다.
    /// ScriptableObject Step 자체에는 Runtime 상태를 저장하지 않습니다.
    /// </summary>
    public sealed class StartupLoadStepContext
    {
        private readonly Action<float> progressReporter;

        internal StartupLoadStepContext(StartupLoadContext shared, Action<float> progressReporter)
        {
            Shared = shared;
            this.progressReporter = progressReporter;
        }

        public StartupLoadContext Shared { get; }
        public LoadStepResult Result { get; private set; } = LoadStepResult.Success;
        public string Message { get; private set; } = string.Empty;

        public void ReportProgress(float progress)
        {
            progressReporter?.Invoke(Mathf.Clamp01(progress));
        }

        public void Recover(string message)
        {
            if (Result == LoadStepResult.Fatal)
            {
                return;
            }

            Result = LoadStepResult.Recovered;
            Message = message ?? string.Empty;
        }

        public void Fail(string message)
        {
            Result = LoadStepResult.Fatal;
            Message = string.IsNullOrWhiteSpace(message) ? "알 수 없는 로딩 오류가 발생했습니다." : message;
        }
    }
}
