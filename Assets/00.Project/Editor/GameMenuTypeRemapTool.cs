#if UNITY_EDITOR
using System;
using UI;
using UnityEditor;
using UnityEngine;

namespace TaskTown.EditorTools
{
    /// <summary>
    /// UIPanelWindow.gameMenuType을 확정 GameMenuType으로 맞춥니다.
    /// 오브젝트/프리팹 이름 휴리스틱을 쓰므로 재실행해도 안전합니다.
    /// </summary>
    public static class GameMenuTypeRemapTool
    {
        [MenuItem("Tools/TaskTown/UI/Remap GameMenuType Prefabs")]
        public static void RemapAllUiPanelWindows()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/00.Project/02.Prefabs/UI" });
            var paths = new System.Collections.Generic.HashSet<string>();
            for (int i = 0; i < guids.Length; i++)
                paths.Add(AssetDatabase.GUIDToAssetPath(guids[i]));

            // FindAssets 누락 대비 강제 포함
            paths.Add("Assets/00.Project/02.Prefabs/UI/ALL_UI_Connect_root_Edit.prefab");
            paths.Add("Assets/00.Project/02.Prefabs/UI/ALL_UI_Connect_root.prefab");

            int changedCount = 0;
            int scannedCount = 0;
            string[] pathList = new string[paths.Count];
            paths.CopyTo(pathList);

            try
            {
                for (int i = 0; i < pathList.Length; i++)
                {
                    string path = pathList[i];
                    if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
                        continue;

                    EditorUtility.DisplayProgressBar(
                        "GameMenuType Remap",
                        path,
                        pathList.Length == 0 ? 1f : (float)i / pathList.Length);

                    if (RemapPrefab(path))
                        changedCount++;

                    scannedCount++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[GameMenuTypeRemapTool] 스캔 {scannedCount}개 프리팹, 변경 {changedCount}개");
        }

        private static bool RemapPrefab(string prefabPath)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
                return false;

            bool changed = false;
            try
            {
                UIPanelWindow[] windows = root.GetComponentsInChildren<UIPanelWindow>(true);
                for (int i = 0; i < windows.Length; i++)
                {
                    if (RemapWindow(windows[i], prefabPath))
                        changed = true;
                }

                if (changed)
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return changed;
        }

        private static bool RemapWindow(UIPanelWindow window, string prefabPath)
        {
            if (window == null)
                return false;

            if (!TryResolveMenuType(window, prefabPath, out GameMenuType resolved))
                return false;

            SerializedObject so = new SerializedObject(window);
            SerializedProperty typeProp = so.FindProperty("gameMenuType");
            if (typeProp == null)
                return false;

            int current = typeProp.intValue;
            int next = (int)resolved;
            if (current == next)
                return false;

            Undo.RecordObject(window, "Remap GameMenuType");
            typeProp.intValue = next;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(window);

            Debug.Log(
                $"[GameMenuTypeRemapTool] {prefabPath} / {window.name}: {(GameMenuType)current}({current}) → {resolved}({next})",
                window);
            return true;
        }

        private static bool TryResolveMenuType(UIPanelWindow window, string prefabPath, out GameMenuType menuType)
        {
            string key = $"{prefabPath}/{window.name}".ToLowerInvariant();

            if (Contains(key, "_page") || Contains(key, "page_"))
            {
                menuType = GameMenuType.None;
                return true;
            }

            if (Contains(key, "villageinfo"))
            {
                menuType = GameMenuType.None;
                return true;
            }

            if (Contains(key, "popup") || Contains(key, "levelup"))
            {
                menuType = GameMenuType.LevelUpPopup;
                return true;
            }

            if (Contains(key, "gacha"))
            {
                menuType = GameMenuType.Gacha;
                return true;
            }

            if (Contains(key, "option"))
            {
                menuType = GameMenuType.Option;
                return true;
            }

            if (Contains(key, "dex"))
            {
                menuType = GameMenuType.Dex;
                return true;
            }

            if (Contains(key, "village") || Contains(key, "animalset") || Contains(key, "animal_set"))
            {
                menuType = GameMenuType.Village;
                return true;
            }

            if (Contains(key, "inv") || Contains(key, "inventory") || Contains(key, "aniaml"))
            {
                menuType = GameMenuType.Inventory;
                return true;
            }

            menuType = GameMenuType.None;
            return false;
        }

        private static bool Contains(string source, string token)
        {
            return source.IndexOf(token, StringComparison.Ordinal) >= 0;
        }
    }
}
#endif
