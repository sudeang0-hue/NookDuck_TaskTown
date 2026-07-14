using System;
using System.Collections.Generic;
using UnityEngine;

namespace TaskTown.SceneFlow
{
    [CreateAssetMenu(fileName = "SceneCatalog", menuName = "TaskTown/Scene Flow/Scene Catalog")]
    public sealed class SceneCatalogSO : ScriptableObject
    {
        [Serializable]
        private sealed class SceneEntry
        {
            [SerializeField] private SceneId id;
            [SerializeField] private string sceneName;

            public SceneId Id => id;
            public string SceneName => sceneName;
        }

        [SerializeField] private List<SceneEntry> entries = new();

        public bool TryGetSceneName(SceneId id, out string sceneName)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                SceneEntry entry = entries[i];
                if (entry == null || entry.Id != id)
                {
                    continue;
                }

                sceneName = entry.SceneName;
                return !string.IsNullOrWhiteSpace(sceneName);
            }

            sceneName = string.Empty;
            return false;
        }

        public bool TryValidate(out string errorMessage)
        {
            HashSet<SceneId> registeredIds = new();
            HashSet<string> registeredNames = new(StringComparer.Ordinal);

            for (int i = 0; i < entries.Count; i++)
            {
                SceneEntry entry = entries[i];
                if (entry == null)
                {
                    errorMessage = $"Scene Catalog의 {i}번 항목이 비어 있습니다.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(entry.SceneName))
                {
                    errorMessage = $"{entry.Id}에 연결된 Scene 이름이 비어 있습니다.";
                    return false;
                }

                if (!registeredIds.Add(entry.Id))
                {
                    errorMessage = $"Scene ID가 중복 등록되었습니다: {entry.Id}";
                    return false;
                }

                if (!registeredNames.Add(entry.SceneName))
                {
                    errorMessage = $"Scene 이름이 중복 등록되었습니다: {entry.SceneName}";
                    return false;
                }
            }

            Array sceneIds = Enum.GetValues(typeof(SceneId));
            if (registeredIds.Count != sceneIds.Length)
            {
                errorMessage = "SceneCatalogSO에 모든 SceneId가 등록되지 않았습니다.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }
    }
}
