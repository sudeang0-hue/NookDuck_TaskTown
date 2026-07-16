using System.Collections.Generic;
using TaskTown.Gacha;
using Tool.Data;
using UnityEngine;

public class ToolDatabaseList : MonoBehaviour
{
    [Header("도구 데이터베이스")]
    [Tooltip("실제 데이터 원본")]
    [SerializeField] private ToolDatabase toolDatabase;

    [Header("전체 도구 리스트")]
    [Tooltip("ToolDatabase의 등록 상태를 Inspector에서 확인하기 위한 검증용 리스트\n게임에서 사용되는 모든 도구 데이터를 저장합니다.")]
    [SerializeField] private List<ToolDataSO> allToolList = new();


    [Header("등급별 도구 리스트\n각 등급에 어떤 도구가 포함되어 있는지 확인합니다")]
    [SerializeField] private List<ToolDataSO> gradeTool_Normal = new List<ToolDataSO>();
    [SerializeField] private List<ToolDataSO> gradeTool_Rare = new List<ToolDataSO>();
    [SerializeField] private List<ToolDataSO> gradeTool_Epic = new List<ToolDataSO>();
    [SerializeField] private List<ToolDataSO> gradeTool_Unique = new List<ToolDataSO>();
    [SerializeField] private List<ToolDataSO> gradeTool_Legendary = new List<ToolDataSO>();

    public IReadOnlyList<ToolDataSO> AllToolList => allToolList;
    public IReadOnlyList<ToolDataSO> GradeToolNormal => gradeTool_Normal;
    public IReadOnlyList<ToolDataSO> GradeToolRare => gradeTool_Rare;
    public IReadOnlyList<ToolDataSO> GradeToolEpic => gradeTool_Epic;
    public IReadOnlyList<ToolDataSO> GradeToolUnique => gradeTool_Unique;
    public IReadOnlyList<ToolDataSO> GradeToolLegendary => gradeTool_Legendary;

    private void Awake()
    {
        RefreshDatabaseList();
    }


    /// <summary>
    /// ToolDatabase의 전체 목록을 가져와 등급별 리스트로 분류합니다.
    /// Inspector의 컴포넌트 메뉴에서도 실행할 수 있습니다.
    /// </summary>
    [ContextMenu("도구 데이터베이스 목록 갱신")]
    private void RefreshDatabaseList()
    {
        ClearLists();

        if (toolDatabase == null)
        {
            Debug.LogWarning("[ToolDatabaseList] ToolDatabase 가 연결되지 않았습니다.", this);
            return;
        }

        IReadOnlyList<ToolDataSO> databaseTools = toolDatabase.Tools;

        if (databaseTools == null)
        {
            Debug.LogWarning("[ToolDatabaseList] ToolDatabase 의 도구 목록이 없습니다.", this);
            return;
        }

        foreach (ToolDataSO tool in databaseTools)
        {
            if (tool == null)
                continue;

            allToolList.Add(tool);
            AddToolByGrade(tool);

        }

        Debug.Log(
                $"[ToolDatabaseList] 목록 갱신 완료 / " +
                $"전체: {allToolList.Count}, " +
                $"Normal: {gradeTool_Normal.Count}, " +
                $"Rare: {gradeTool_Rare.Count}, " +
                $"Epic: {gradeTool_Epic.Count}, " +
                $"Unique: {gradeTool_Unique.Count}, " +
                $"Legend: {gradeTool_Legendary.Count}",
                this
            );
    }

    /// <summary>
    /// 등급별 도구 리스트에 추가
    /// </summary>
    private void AddToolByGrade(ToolDataSO tool)
    {
        switch (tool.Grade)
        {
            case ItemGrade.Normal:
                gradeTool_Normal.Add(tool);
                break;

            case ItemGrade.Rare:
                gradeTool_Rare.Add(tool);
                break;

            case ItemGrade.Epic:
                gradeTool_Epic.Add(tool);
                break;

            case ItemGrade.Unique:
                gradeTool_Unique.Add(tool);
                break;

            case ItemGrade.Legendary:
                gradeTool_Legendary.Add(tool);
                break;

            default:
                Debug.LogWarning(
                    $"[ToolDatabaseList] 분류되지 않은 등급입니다. " +
                    $"ID: {tool.Id}, Grade: {tool.Grade}",
                    tool
                );
                break;
        }
    }


    /// <summary>
    /// 리스트 초기화
    /// </summary>
    private void ClearLists()
    {
        allToolList.Clear();
        gradeTool_Normal.Clear();
        gradeTool_Rare.Clear();
        gradeTool_Epic.Clear();
        gradeTool_Unique.Clear();
        gradeTool_Legendary.Clear();
    }
}
