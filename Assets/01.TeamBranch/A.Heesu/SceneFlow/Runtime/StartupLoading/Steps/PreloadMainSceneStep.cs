using System.Collections;
using UnityEngine;

namespace TaskTown.SceneFlow
{
    [CreateAssetMenu(fileName = "PreloadMainSceneStep", menuName = "TaskTown/Scene Flow/Steps/Preload Main Scene")]
    public sealed class PreloadMainSceneStep : StartupLoadStepSO
    {
        protected override IEnumerator ExecuteStep(StartupLoadStepContext context)
        {
            SceneFlowManager manager = SceneFlowManager.EnsureInstance();
            context.ReportProgress(0f);

            // 새 Scene의 Start가 이전 LoadSceneAsync 완료 처리보다 먼저 호출될 수 있습니다.
            // 이전 전환이 완전히 정리된 뒤 Main Scene 사전 로드를 시작합니다.
            while (manager.State == SceneLoadState.Loading ||
                   manager.State == SceneLoadState.Activating)
            {
                yield return null;
            }

            if (manager.State == SceneLoadState.Failed)
            {
                context.Fail(manager.LastError);
                yield break;
            }

            if (!manager.PreloadScene(context.Shared.TargetScene))
            {
                context.Fail(manager.LastError);
                yield break;
            }

            while (manager.State == SceneLoadState.Loading)
            {
                context.ReportProgress(manager.Progress);
                yield return null;
            }

            if (manager.State != SceneLoadState.ReadyToActivate)
            {
                context.Fail(string.IsNullOrWhiteSpace(manager.LastError)
                    ? "Main Scene이 활성화 준비 상태에 도달하지 못했습니다."
                    : manager.LastError);
                yield break;
            }

            context.ReportProgress(1f);
        }
    }
}
