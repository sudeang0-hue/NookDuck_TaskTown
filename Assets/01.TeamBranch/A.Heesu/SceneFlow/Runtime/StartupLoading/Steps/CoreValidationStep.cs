using System.Collections;
using UnityEngine;

namespace TaskTown.SceneFlow
{
    [CreateAssetMenu(fileName = "CoreValidationStep", menuName = "TaskTown/Scene Flow/Steps/Core Validation")]
    public sealed class CoreValidationStep : StartupLoadStepSO
    {
        protected override IEnumerator ExecuteStep(StartupLoadStepContext context)
        {
            context.ReportProgress(0f);

            SceneFlowManager manager = SceneFlowManager.EnsureInstance();
            if (!manager.CanLoadScene(context.Shared.TargetScene, out string errorMessage))
            {
                context.Fail(errorMessage);
                yield break;
            }

            context.ReportProgress(1f);
        }
    }
}
