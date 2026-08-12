//NB

#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 숨겨진 TMP 서브메쉬 오브젝트를 강제로 시각화하고 널 참조를 복구하는 TD 진단 툴
public class TMPDeepDiagnoser : EditorWindow
{
    [MenuItem("Tools/TEXT/TMP 숨은 참조 정밀 진단")]
    public static void DiagnoseAndFix()
    {
        // 씬 내의 비활성화된 오브젝트 및 숨겨진 오브젝트까지 모두 탐색
        TMP_SubMeshUI[] subMeshes = Resources.FindObjectsOfTypeAll<TMP_SubMeshUI>();
        int fixedCount = 0;
        int revealedCount = 0;

        foreach (var subMesh in subMeshes)
        {
            // 에디터 메인 씬에 존재하는 오브젝트인지 확인 (프리 에셋 제외)
            if (EditorUtility.IsPersistent(subMesh.transform.root.gameObject)) continue;

            // 1. HideFlags로 숨겨져 있던 서브메쉬 오브젝트를 계층 구조창(Hierarchy)에 노출
            if (subMesh.gameObject.hideFlags != HideFlags.None)
            {
                subMesh.gameObject.hideFlags = HideFlags.None;
                revealedCount++;
            }

            // 2. 머티리얼 또는 폰트 에셋 참조가 끊어진 서브메쉬 복구 / 정제
            if (subMesh.sharedMaterial == null || subMesh.fontAsset == null)
            {
                TextMeshProUGUI parentText = subMesh.GetComponentInParent<TextMeshProUGUI>();
                if (parentText == null)
                {
                    // 부모를 잃은 고아 서브메쉬 즉시 제거
                    DestroyImmediate(subMesh.gameObject);
                    fixedCount++;
                }
                else
                {
                    // 부모의 텍스트 구성요소를 재계산하여 머티리얼 재할당
                    parentText.SetAllDirty();
                    fixedCount++;
                }
            }
        }

        Debug.Log($"진단 완료! 숨겨진 서브메쉬 {revealedCount}개 노출, 손상된 참조 {fixedCount}개 복구 완료.");
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
}
#endif