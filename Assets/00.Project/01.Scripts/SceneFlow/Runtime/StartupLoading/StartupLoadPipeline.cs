using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TaskTown.SceneFlow
{
    /// <summary>
    /// 설정, 저장 데이터, Runtime 적용, Scene 사전 로드를 정해진 순서로 실행합니다.
    /// </summary>
    public sealed class StartupLoadPipeline : MonoBehaviour
    {
        [SerializeField] private StartupLoadPlanSO loadPlan;
        [SerializeField] private SceneId targetScene = SceneId.TestMainGame;

        private Coroutine pipelineCoroutine;

        public bool IsRunning => pipelineCoroutine != null;

        public event Action<float, string> ProgressChanged;
        public event Action<StartupLoadContext> Completed;
        public event Action<string> Failed;

        public bool Run()
        {
            if (pipelineCoroutine != null)
            {
                Debug.LogWarning("[StartupLoadPipeline] Pipeline이 이미 실행 중입니다.", this);
                return false;
            }

            if (loadPlan == null)
            {
                FailPipeline("StartupLoadPlanSO가 연결되지 않았습니다.");
                return false;
            }

            pipelineCoroutine = StartCoroutine(RunPipelineRoutine());
            return true;
        }

        private IEnumerator RunPipelineRoutine()
        {
            List<StartupLoadStepSO> orderedSteps = BuildOrderedSteps();
            if (orderedSteps.Count == 0)
            {
                FailPipeline("Startup Load Step이 하나도 등록되지 않았습니다.");
                yield break;
            }

            WarnDuplicateOrders(orderedSteps);

            float totalWeight = 0f;
            for (int i = 0; i < orderedSteps.Count; i++)
            {
                totalWeight += orderedSteps[i].Weight;
            }

            StartupLoadContext sharedContext = new(targetScene);
            float completedWeight = 0f;

            for (int i = 0; i < orderedSteps.Count; i++)
            {
                StartupLoadStepSO step = orderedSteps[i];
                float stepStartWeight = completedWeight;
                StartupLoadStepContext stepContext = new(
                    sharedContext,
                    progress => ReportProgress(
                        (stepStartWeight + step.Weight * progress) / totalWeight,
                        step.DisplayName));

                ReportProgress(completedWeight / totalWeight, step.DisplayName);

                IEnumerator stepRoutine;
                try
                {
                    stepRoutine = step.Execute(stepContext);
                }
                catch (Exception exception)
                {
                    stepContext.Fail($"{step.DisplayName} 시작 중 예외가 발생했습니다. {exception.Message}");
                    stepRoutine = null;
                }

                if (stepRoutine != null)
                {
                    bool isRunning = true;
                    while (isRunning)
                    {
                        object yieldedObject = null;

                        try
                        {
                            isRunning = stepRoutine.MoveNext();
                            if (isRunning)
                            {
                                yieldedObject = stepRoutine.Current;
                            }
                        }
                        catch (Exception exception)
                        {
                            stepContext.Fail($"{step.DisplayName} 실행 중 예외가 발생했습니다. {exception.Message}");
                            isRunning = false;
                        }

                        if (isRunning)
                        {
                            yield return yieldedObject;
                        }
                    }
                }

                if (stepContext.Result == LoadStepResult.Fatal)
                {
                    FailPipeline($"{step.DisplayName}: {stepContext.Message}");
                    yield break;
                }

                if (stepContext.Result == LoadStepResult.Recovered)
                {
                    Debug.LogWarning($"[StartupLoadPipeline] {step.DisplayName}: {stepContext.Message}", this);
                }

                completedWeight += step.Weight;
                ReportProgress(completedWeight / totalWeight, step.DisplayName);
            }

            sharedContext.GameSession.MarkReady();
            GameSessionStore.Publish(sharedContext.GameSession);
            ReportProgress(1f, "로딩 완료");

            pipelineCoroutine = null;
            Completed?.Invoke(sharedContext);
        }

        private List<StartupLoadStepSO> BuildOrderedSteps()
        {
            List<StartupLoadStepSO> orderedSteps = new();
            IReadOnlyList<StartupLoadStepSO> sourceSteps = loadPlan.Steps;

            for (int i = 0; i < sourceSteps.Count; i++)
            {
                if (sourceSteps[i] != null)
                {
                    orderedSteps.Add(sourceSteps[i]);
                }
            }

            orderedSteps.Sort(CompareSteps);
            return orderedSteps;
        }

        private static int CompareSteps(StartupLoadStepSO left, StartupLoadStepSO right)
        {
            int phaseComparison = left.Phase.CompareTo(right.Phase);
            return phaseComparison != 0 ? phaseComparison : left.Order.CompareTo(right.Order);
        }

        private void WarnDuplicateOrders(IReadOnlyList<StartupLoadStepSO> orderedSteps)
        {
            for (int i = 1; i < orderedSteps.Count; i++)
            {
                StartupLoadStepSO previous = orderedSteps[i - 1];
                StartupLoadStepSO current = orderedSteps[i];

                if (previous.Phase == current.Phase && previous.Order == current.Order)
                {
                    Debug.LogWarning(
                        $"[StartupLoadPipeline] 동일한 실행 순서가 등록되었습니다. " +
                        $"Phase: {current.Phase}, Order: {current.Order}, " +
                        $"Steps: {previous.name}, {current.name}",
                        loadPlan);
                }
            }
        }

        private void ReportProgress(float progress, string stepName)
        {
            ProgressChanged?.Invoke(Mathf.Clamp01(progress), stepName);
        }

        private void FailPipeline(string errorMessage)
        {
            pipelineCoroutine = null;
            Debug.LogError($"[StartupLoadPipeline] {errorMessage}", this);
            Failed?.Invoke(errorMessage);
        }
    }
}
