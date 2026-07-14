using System.Collections;
using UnityEngine;

namespace TaskTown.SceneFlow
{
    public abstract class StartupLoadStepSO : ScriptableObject
    {
        [Header("Execution")]
        [SerializeField] private StartupLoadPhase phase;
        [SerializeField] private int order;
        [SerializeField, Min(0.01f)] private float weight = 1f;
        [SerializeField] private string displayName = "Loading";

        public StartupLoadPhase Phase => phase;
        public int Order => order;
        public float Weight => Mathf.Max(0.01f, weight);
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;

        internal IEnumerator Execute(StartupLoadStepContext context)
        {
            return ExecuteStep(context);
        }

        protected abstract IEnumerator ExecuteStep(StartupLoadStepContext context);
    }
}
