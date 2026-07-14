// 모든 도구 ToolDataSO 를 모아두는 SO

using System.Collections.Generic;
using TaskTown.Gacha;
using UnityEngine;

namespace Tool.Data
{
    [CreateAssetMenu(menuName = "Inventory/Tool/Tool Database SO", fileName = "ToolDatabaseSO")]
    public class ToolDatabase : ScriptableObject
    {
        [Header("전체 도구 데이터 목록")]
        [SerializeField] private List<ToolDataSO> tools = new List<ToolDataSO>();

        private Dictionary<string, ToolDataSO> toolMap;

        public IReadOnlyList<ToolDataSO> Tools => tools;

        public void Initialize()
        {
            toolMap = new Dictionary<string, ToolDataSO>();

            foreach (ToolDataSO tool in tools)
            {
                if (tool == null) continue;

                if (string.IsNullOrEmpty(tool.Id))
                {
                    Debug.LogWarning("[ToolDatabase] ID가 비어있는 toolData 가 있습니다.");
                    continue;
                }

                if (toolMap.ContainsKey(tool.Id))
                {
                    Debug.LogWarning($"[ToolDatabase] 중복 Tool ID 가 있습니다 : {tool.Id}");
                    continue;
                }

                toolMap.Add(tool.Id, tool);
            }
        }

        public bool Contains(string toolId)
        {
            return GetToolData(toolId) != null;
        }

        public ToolDataSO GetToolData(string toolId)
        {
            if (toolMap == null)
            {
                Initialize();
            }

            if (string.IsNullOrEmpty(toolId))
                return null;

            toolMap.TryGetValue(toolId, out ToolDataSO toolData);

            return toolData;
        }

        /// <summary>
        /// 등급별 조회 함수
        /// </summary>
        public List<ToolDataSO> GetToolsByGrade(ItemGrade grade)
        {
            if (toolMap == null)
            {
                Initialize();
            }

            List<ToolDataSO> result = new List<ToolDataSO>();

            foreach (ToolDataSO tool in tools)
            {
                if (tool == null)
                    continue;

                if (tool.Grade == grade)
                {
                    result.Add(tool);
                }
            }

            return result;
        }
    }
}

