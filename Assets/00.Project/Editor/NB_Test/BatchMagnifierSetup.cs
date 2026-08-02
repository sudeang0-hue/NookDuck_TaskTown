#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 49개 이상의 동물 프리팹에 돋보기 아이콘 및 관련 컴포넌트를 일괄 자동 생성하는 에디터 툴
/// </summary>
public class BatchMagnifierSetup : EditorWindow
{
    private DefaultAsset targetFolder;   // 프리팹들이 모여있는 폴더
    private Sprite magnifierSprite;       // 머리 위에 띄울 돋보기 이미지
    private float headHeightOffset = 2.0f;// 머리 위 기본 높이 Y축 값
    private Vector3 iconScale = new Vector3(0.5f, 0.5f, 0.5f); // [추가] 돋보기 아이콘 크기 제어

    [MenuItem("Tools/Animal Prefab Setup Tool")]
    public static void ShowWindow()
    {
        GetWindow<BatchMagnifierSetup>("동물 프리팹 일괄 세팅 툴");
    }

    private void OnGUI()
    {
        GUILayout.Label(" 49개 동물 프리팹 일괄 자동 세팅 (TD Special)", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        targetFolder = (DefaultAsset)EditorGUILayout.ObjectField("동물 프리팹 폴더", targetFolder, typeof(DefaultAsset), false);
        magnifierSprite = (Sprite)EditorGUILayout.ObjectField("돋보기 Sprite 텍스처", magnifierSprite, typeof(Sprite), false);
        headHeightOffset = EditorGUILayout.FloatField("머리 위 Y축 오프셋 위치", headHeightOffset);
        iconScale = EditorGUILayout.Vector3Field("돋보기 아이콘 스케일(크기)", iconScale); // 스케일 조절 필드 추가

        EditorGUILayout.Space();

        if (GUILayout.Button("1-Click 작업 시작 (모든 프리팹 자동 개조)", GUILayout.Height(40)))
        {
            ProcessBatchSetup();
        }
    }

    private void ProcessBatchSetup()
    {
        if (targetFolder == null || magnifierSprite == null)
        {
            EditorUtility.DisplayDialog("경고", "폴더와 돋보기 Sprite를 모두 할당해 주세요,", "확인");
            return;
        }

        // 폴더 에셋 경로 추출
        string folderPath = AssetDatabase.GetAssetPath(targetFolder);

        // 해당 폴더 내의 모든 프리팹 GUID 수집
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });

        int processCount = 0;

        foreach (string guid in prefabGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);

            // 프리팹 내용을 메모리에 로드
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);

            // 1. 이미 MagnifierIcon 자식이 있는지 검사 (멱등성 보장)
            Transform existingIcon = prefabRoot.transform.Find("MagnifierIcon");
            GameObject iconObj;

            if (existingIcon == null)
            {
                // 없으면 새 자식 오브젝트 생성
                iconObj = new GameObject("MagnifierIcon");
                iconObj.transform.SetParent(prefabRoot.transform, false);
            }
            else
            {
                iconObj = existingIcon.gameObject;
            }

            // [핵심 해결책] 스케일과 로컬 위치를 강제로 정확하게 세팅 (크게 뜨는 버그 완벽 수정!)
            iconObj.transform.localPosition = new Vector3(0, headHeightOffset, 0);
            iconObj.transform.localScale = iconScale;

            // 2. SpriteRenderer 컴포넌트 추가 및 텍스처 설정
            if (!iconObj.TryGetComponent<SpriteRenderer>(out var spriteRenderer))
            {
                spriteRenderer = iconObj.AddComponent<SpriteRenderer>();
            }
            spriteRenderer.sprite = magnifierSprite;
            // 다른 3D 메쉬보다 앞에 그려지도록 Sorting Order 지정
            spriteRenderer.sortingOrder = 10;

            // 3. BillboardIcon 컴포넌트 추가 (카메라 정면 바라보기 컴포넌트가 있다면)
            if (System.Type.GetType("BillboardIcon") != null)
            {
                if (iconObj.GetComponent("BillboardIcon") == null)
                {
                    iconObj.AddComponent(System.Type.GetType("BillboardIcon"));
                }
            }

            // 4. 최상위에 AnimalHoverHandler 컴포넌트 주입 (클래스명 수정 완료! 🛠️)
            if (!prefabRoot.TryGetComponent<AnimalHoverHandler>(out var hoverScript))
            {
                hoverScript = prefabRoot.AddComponent<AnimalHoverHandler>();
            }

            // [자동 연결] SerializedObject를 사용하여 AnimalHoverHandler의 magnifierIcon 필드에 자동 할당
            SerializedObject serializedHover = new SerializedObject(hoverScript);
            SerializedProperty propIcon = serializedHover.FindProperty("magnifierIcon");
            if (propIcon != null)
            {
                propIcon.objectReferenceValue = iconObj;
                serializedHover.ApplyModifiedProperties();
            }

            // 초기 상태는 비활성화
            iconObj.SetActive(false);

            // 변경된 프리팹을 파일로 저장하고 메모리 해제
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);

            processCount++;
        }

        // 에셋 데이터베이스 갱신
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("완료!", $"총 {processCount}개의 동물 프리팹 개조 성공!\n스케일과 호버 컴포넌트가 정상 연결되었습니다,", "확인");
    }
}
#endif