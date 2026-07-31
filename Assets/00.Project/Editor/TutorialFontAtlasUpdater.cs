#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace TaskTown.EditorTools.Tutorial
{
    /// <summary>
    /// 튜토리얼 문구 파일의 글자를 기존 TMP Font Asset에 증분 추가합니다.
    /// 원본 폰트 파일과 Runtime 코드는 변경하지 않습니다.
    /// </summary>
    public static class TutorialFontAtlasUpdater
    {
        private const string CharacterFilePath =
            "Assets/01.TeamBranch/A.Heesu/Resources/TutorialCharacters.txt";
        private const string FontAssetPath =
            "Assets/01.TeamBranch/A.Heesu/Resources/Griun_Fromsol-Rg_tutorial.asset";

        [MenuItem("Tools/TaskTown/Tutorial/Update Tutorial Font Atlas")]
        public static void UpdateTutorialFontAtlas()
        {
            TextAsset characterFile = AssetDatabase.LoadAssetAtPath<TextAsset>(
                CharacterFilePath);
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                FontAssetPath);

            if (characterFile == null || fontAsset == null)
            {
                Debug.LogError(
                    "[TutorialFontAtlasUpdater] 튜토리얼 문자 파일 또는 TMP Font Asset을 찾지 못했습니다.");
                return;
            }

            HashSet<uint> existingCharacters = new HashSet<uint>();
            foreach (TMP_Character character in fontAsset.characterTable)
            {
                if (character != null)
                    existingCharacters.Add(character.unicode);
            }

            StringBuilder charactersToAdd = new StringBuilder();
            HashSet<uint> requestedCharacters = new HashSet<uint>();
            foreach (char character in characterFile.text)
            {
                uint unicode = character;
                if (char.IsControl(character) ||
                    existingCharacters.Contains(unicode) ||
                    !requestedCharacters.Add(unicode))
                {
                    continue;
                }

                charactersToAdd.Append(character);
            }

            if (charactersToAdd.Length == 0)
            {
                Debug.Log(
                    "[TutorialFontAtlasUpdater] 튜토리얼 TMP Font Atlas가 이미 최신 상태입니다.",
                    fontAsset);
                return;
            }

            Undo.RegisterCompleteObjectUndo(fontAsset, "Update Tutorial Font Atlas");
            Texture2D[] existingAtlasTextures = fontAsset.atlasTextures;
            if (existingAtlasTextures != null)
            {
                foreach (Texture2D atlasTexture in existingAtlasTextures)
                {
                    if (atlasTexture != null)
                    {
                        Undo.RegisterCompleteObjectUndo(
                            atlasTexture,
                            "Update Tutorial Font Atlas");
                    }
                }
            }

            AtlasPopulationMode previousMode = fontAsset.atlasPopulationMode;

            fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            bool addedAllCharacters = fontAsset.TryAddCharacters(
                charactersToAdd.ToString(),
                out string missingCharacters);
            fontAsset.atlasPopulationMode = previousMode;

            EditorUtility.SetDirty(fontAsset);
            Texture2D[] atlasTextures = fontAsset.atlasTextures;
            if (atlasTextures != null)
            {
                foreach (Texture2D atlasTexture in atlasTextures)
                {
                    if (atlasTexture != null)
                        EditorUtility.SetDirty(atlasTexture);
                }
            }

            AssetDatabase.SaveAssets();

            if (!addedAllCharacters && !string.IsNullOrEmpty(missingCharacters))
            {
                Debug.LogWarning(
                    $"[TutorialFontAtlasUpdater] 원본 폰트에서 찾지 못한 문자: {missingCharacters}",
                    fontAsset);
                return;
            }

            Debug.Log(
                "[TutorialFontAtlasUpdater] 튜토리얼 TMP Font Atlas 갱신을 완료했습니다.",
                fontAsset);
        }
    }
}
#endif
