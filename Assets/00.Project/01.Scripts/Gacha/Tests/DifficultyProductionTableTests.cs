using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TaskTown.Gacha.Tests
{
    public class DifficultyProductionTableTests
    {
        private static DifficultyProductionTable CreateTable(params (DifficultyType difficulty, float multiplier)[] entries)
        {
            DifficultyProductionTable table = ScriptableObject.CreateInstance<DifficultyProductionTable>();
            SerializedObject so = new SerializedObject(table);
            SerializedProperty entriesProp = so.FindProperty("entries");
            entriesProp.ClearArray();

            for (int i = 0; i < entries.Length; i++)
            {
                entriesProp.InsertArrayElementAtIndex(i);
                SerializedProperty entry = entriesProp.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("difficulty").enumValueIndex = (int)entries[i].difficulty;
                entry.FindPropertyRelative("productionMultiplier").floatValue = entries[i].multiplier;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return table;
        }

        [Test]
        public void GetMultiplier_등록된_난이도면_해당_배율을_반환한다()
        {
            DifficultyProductionTable table = CreateTable(
                (DifficultyType.Normal, 1.5f),
                (DifficultyType.Hard, 0.7f));

            Assert.AreEqual(1.5f, table.GetMultiplier(DifficultyType.Normal), 0.0001f);
            Assert.AreEqual(0.7f, table.GetMultiplier(DifficultyType.Hard), 0.0001f);
        }

        [Test]
        public void GetMultiplier_등록되지_않은_난이도는_1을_반환한다()
        {
            DifficultyProductionTable table = CreateTable((DifficultyType.Normal, 1.5f));

            Assert.AreEqual(1f, table.GetMultiplier(DifficultyType.VeryHard), 0.0001f);
        }
    }
}
