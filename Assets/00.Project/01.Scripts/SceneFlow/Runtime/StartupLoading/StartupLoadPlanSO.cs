using System.Collections.Generic;
using UnityEngine;

namespace TaskTown.SceneFlow
{
    [CreateAssetMenu(fileName = "StartupLoadPlan", menuName = "TaskTown/Scene Flow/Startup Load Plan")]
    public sealed class StartupLoadPlanSO : ScriptableObject
    {
        [SerializeField] private List<StartupLoadStepSO> steps = new();

        public IReadOnlyList<StartupLoadStepSO> Steps => steps;
    }
}
