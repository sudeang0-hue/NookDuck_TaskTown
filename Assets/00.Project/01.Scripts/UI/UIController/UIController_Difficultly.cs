using TMPro;
using UnityEngine;

/// <summary>
/// 엔딩 후 난이도 선택 씬의 타이틀/설명 UI를 갱신합니다.
/// 배열 순서: 0 Easy / 1 Normal / 2 Hard / 3 Very Hard
/// </summary>
public class UIController_Difficultly : MonoBehaviour
{
    // 0: Easy / 1: Normal / 2: Hard / 3: Very Hard
    private const int DifficultyCount = 4;

    [Header("0: Easy / 1: Normal / 2: Hard / 3: Very Hard")]

    [Header("난이도 타이틀")]
    [SerializeField] private TMP_Text[] difficultlyTitle;

    [Header("난이도 설명")]
    [SerializeField] private TMP_Text[] difficultlyDescription;

    [Header("난이도 타이틀 텍스트")]
    [SerializeField] private string[] difficultlyTitleText;

    [Header("난이도 설명글 텍스트")]
    [SerializeField, TextArea(2, 4)] private string[] difficultlyDescriptionText;

    private void Start()
    {
        RefreshDifficultyTexts();
    }

    /// <summary>
    /// 인스펙터 문자열 배열을 TMP_Text 배열에 반영합니다.
    /// 비어 있는 참조/배열은 skip합니다.
    /// </summary>
    private void RefreshDifficultyTexts()
    {
        ApplyTexts(difficultlyTitle, difficultlyTitleText);
        ApplyTexts(difficultlyDescription, difficultlyDescriptionText);
    }

    private static void ApplyTexts(TMP_Text[] targets, string[] sources)
    {
        // 비워둔 상태 허용: 해당 항목만 skip하고 다음 기능 계속
        if (targets == null || sources == null || targets.Length == 0 || sources.Length == 0)
            return;

        int count = Mathf.Min(DifficultyCount, Mathf.Min(targets.Length, sources.Length));
        for (int i = 0; i < count; i++)
        {
            if (targets[i] == null)
                continue;

            targets[i].text = sources[i] ?? string.Empty;
        }
    }
}
