using UnityEngine;

/// <summary>
/// 마을 성장 시스템 전용 테스트 샌드박스 컨트롤러
/// </summary>
public class TownTestHarness : MonoBehaviour
{
    [Header("테스트 대상 시스템 참조")]
    [SerializeField] private TownLevelManager levelManager;

    [Header("디버그 옵션")]
    [SerializeField] private bool showDebugGUI = true;

    private void Update()
    {
        if (levelManager == null) return;

        // [U] 키: 단일 레벨업 실행
        if (Input.GetKeyDown(KeyCode.U))
        {
            Debug.Log("[ECHO_TD] Hotkey 'U' 입력: 레벨업을 실행합니다.");
            levelManager.LevelUp();
        }
    }

    private void OnGUI()
    {
        if (!showDebugGUI) return;

        // 샌드박스 디버그 버튼 박스 구성
        GUILayout.BeginArea(new Rect(20, 20, 220, 300), "마을 성장 테스트 샌드박스", GUI.skin.window);

        if (GUILayout.Button("레벨업 실행 (Hotkey: U)", GUILayout.Height(40)))
        {
            if (levelManager != null) levelManager.LevelUp();
        }

        GUILayout.Space(10);
        GUILayout.Label("--- Quick Level Test ---");

        if (GUILayout.Button("Lv.5 테스트 (2단계 섬 전환)"))
        {
            // 5레벨 연출 테스트
            levelManager.LevelUp();
        }

        if (GUILayout.Button("Lv.10 테스트 (3단계 섬 전환)"))
        {
            // 10레벨 연출 테스트
            levelManager.LevelUp();
        }

        GUILayout.EndArea();
    }
}