using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TaskTown.Gacha.Tests
{
    public class GachaSystemTests
    {
        private static AnimalData CreateAnimal(string id, string displayName, ItemGrade grade)
        {
            AnimalData data = ScriptableObject.CreateInstance<AnimalData>();
            SerializedObject so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("grade").enumValueIndex = (int)grade;
            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }

        private static GachaRateTableData CreateRateTable(params (int townLevelThreshold, (ItemGrade grade, float weight)[] weights)[] levels)
        {
            GachaRateTableData table = ScriptableObject.CreateInstance<GachaRateTableData>();
            SerializedObject so = new SerializedObject(table);
            SerializedProperty levelRates = so.FindProperty("levelRates");
            levelRates.ClearArray();

            for (int i = 0; i < levels.Length; i++)
            {
                levelRates.InsertArrayElementAtIndex(i);
                SerializedProperty levelEntry = levelRates.GetArrayElementAtIndex(i);
                levelEntry.FindPropertyRelative("townLevelThreshold").intValue = levels[i].townLevelThreshold;

                SerializedProperty gradeWeights = levelEntry.FindPropertyRelative("gradeWeights");
                gradeWeights.ClearArray();
                var weights = levels[i].weights;
                for (int w = 0; w < weights.Length; w++)
                {
                    gradeWeights.InsertArrayElementAtIndex(w);
                    SerializedProperty weightEntry = gradeWeights.GetArrayElementAtIndex(w);
                    weightEntry.FindPropertyRelative("grade").enumValueIndex = (int)weights[w].grade;
                    weightEntry.FindPropertyRelative("weight").floatValue = weights[w].weight;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return table;
        }

        private static GachaPoolData CreatePool(GachaRateTableData rateTable, Dictionary<ItemGrade, List<GachaEntryData>> entriesByGrade)
        {
            GachaPoolData pool = ScriptableObject.CreateInstance<GachaPoolData>();
            SerializedObject so = new SerializedObject(pool);
            so.FindProperty("rateTable").objectReferenceValue = rateTable;

            SerializedProperty groups = so.FindProperty("entryGroups");
            groups.ClearArray();

            int groupIndex = 0;
            foreach (KeyValuePair<ItemGrade, List<GachaEntryData>> pair in entriesByGrade)
            {
                groups.InsertArrayElementAtIndex(groupIndex);
                SerializedProperty group = groups.GetArrayElementAtIndex(groupIndex);
                group.FindPropertyRelative("grade").enumValueIndex = (int)pair.Key;

                SerializedProperty entries = group.FindPropertyRelative("entries");
                entries.ClearArray();
                for (int i = 0; i < pair.Value.Count; i++)
                {
                    entries.InsertArrayElementAtIndex(i);
                    entries.GetArrayElementAtIndex(i).objectReferenceValue = pair.Value[i];
                }

                groupIndex++;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return pool;
        }

        [Test]
        public void Roll_가중치가_높은_구간의_등급이_뽑힌다()
        {
            AnimalData normalAnimal = CreateAnimal("A_NORMAL", "오리", ItemGrade.Normal);
            AnimalData rareAnimal = CreateAnimal("A_RARE", "고양이", ItemGrade.Rare);

            GachaRateTableData rateTable = CreateRateTable(
                (1, new (ItemGrade, float)[] { (ItemGrade.Normal, 60f), (ItemGrade.Rare, 40f) }));

            GachaPoolData pool = CreatePool(rateTable, new Dictionary<ItemGrade, List<GachaEntryData>>
            {
                { ItemGrade.Normal, new List<GachaEntryData> { normalAnimal } },
                { ItemGrade.Rare, new List<GachaEntryData> { rareAnimal } },
            });

            // totalWeight = 100. 0.9 * 100 = 90 > Normal 누적치(60) → Rare 구간에 걸림
            GachaSystem system = new GachaSystem(new FixedRandomProvider(0.9f, 0f));

            GachaResult result = system.Roll(pool, townLevel: 1);

            Assert.AreEqual(ItemGrade.Rare, result.Grade);
            Assert.AreEqual(rareAnimal, result.Entry);
        }

        [Test]
        public void Roll_마을레벨_구간에_따라_확률테이블이_바뀐다()
        {
            AnimalData normalAnimal = CreateAnimal("A_NORMAL", "오리", ItemGrade.Normal);
            AnimalData epicAnimal = CreateAnimal("A_EPIC", "공룡", ItemGrade.Epic);

            GachaRateTableData rateTable = CreateRateTable(
                (1, new (ItemGrade, float)[] { (ItemGrade.Normal, 1f) }),
                (5, new (ItemGrade, float)[] { (ItemGrade.Epic, 1f) }));

            GachaPoolData pool = CreatePool(rateTable, new Dictionary<ItemGrade, List<GachaEntryData>>
            {
                { ItemGrade.Normal, new List<GachaEntryData> { normalAnimal } },
                { ItemGrade.Epic, new List<GachaEntryData> { epicAnimal } },
            });

            GachaSystem system = new GachaSystem(new FixedRandomProvider(0f));

            Assert.AreEqual(ItemGrade.Normal, system.Roll(pool, townLevel: 1).Grade);
            // 레벨 3은 5 미만이므로 여전히 레벨 1 구간(Normal)이 적용되어야 함
            Assert.AreEqual(ItemGrade.Normal, system.Roll(pool, townLevel: 3).Grade);
            Assert.AreEqual(ItemGrade.Epic, system.Roll(pool, townLevel: 5).Grade);
        }

        [Test]
        public void Roll_등급에_등록된_엔트리가_없으면_낮은등급으로_대체된다()
        {
            AnimalData normalAnimal = CreateAnimal("A_NORMAL", "오리", ItemGrade.Normal);

            GachaRateTableData rateTable = CreateRateTable(
                (1, new (ItemGrade, float)[] { (ItemGrade.Rare, 1f) }));

            // Rare 확률 100%지만 Rare 항목은 아직 등록되지 않음 → Normal로 대체되어야 함
            GachaPoolData pool = CreatePool(rateTable, new Dictionary<ItemGrade, List<GachaEntryData>>
            {
                { ItemGrade.Normal, new List<GachaEntryData> { normalAnimal } },
            });

            GachaSystem system = new GachaSystem(new FixedRandomProvider(0f));

            GachaResult result = system.Roll(pool, townLevel: 1);

            Assert.AreEqual(normalAnimal, result.Entry);
        }
    }
}
