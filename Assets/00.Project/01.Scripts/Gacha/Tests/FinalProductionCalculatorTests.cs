using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TaskTown.Gacha.Tests
{
    public class FinalProductionCalculatorTests
    {
        private static TestAnimalEntry CreateAnimal(string id, float baseCoinPerSecond, float levelBonusRatePerLevel = 0.2f)
        {
            TestAnimalEntry data = ScriptableObject.CreateInstance<TestAnimalEntry>();
            SerializedObject so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("baseCoinPerSecond").floatValue = baseCoinPerSecond;
            so.FindProperty("levelBonusRatePerLevel").floatValue = levelBonusRatePerLevel;
            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }

        private static TestToolEntry CreateTool(float baseCoinPerSecond, float levelBonusRatePerLevel, string specialAnimalId, float bonusRate)
        {
            TestToolEntry data = ScriptableObject.CreateInstance<TestToolEntry>();
            SerializedObject so = new SerializedObject(data);
            so.FindProperty("baseCoinPerSecond").floatValue = baseCoinPerSecond;
            so.FindProperty("levelBonusRatePerLevel").floatValue = levelBonusRatePerLevel;
            so.FindProperty("specialAnimalId").stringValue = specialAnimalId;
            so.FindProperty("specialAnimalBonusRate").floatValue = bonusRate;
            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }

        private static DifficultyProductionTable CreateDifficultyTable(DifficultyType difficulty, float multiplier)
        {
            DifficultyProductionTable table = ScriptableObject.CreateInstance<DifficultyProductionTable>();
            SerializedObject so = new SerializedObject(table);
            SerializedProperty entries = so.FindProperty("entries");
            entries.ClearArray();
            entries.InsertArrayElementAtIndex(0);
            SerializedProperty entry = entries.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("difficulty").enumValueIndex = (int)difficulty;
            entry.FindPropertyRelative("productionMultiplier").floatValue = multiplier;
            so.ApplyModifiedPropertiesWithoutUndo();
            return table;
        }

        [Test]
        public void CalculateCoinPerSecond_모든_보정값이_기본일때_동물생산량과_도구생산량을_더한다()
        {
            TestAnimalEntry animal = CreateAnimal("A_CAT", 2f);
            TestToolEntry tool = CreateTool(3f, 0.2f, string.Empty, 0f);

            // 동물 2 + 도구 3 (레벨1, 특화없음) = 5
            float result = FinalProductionCalculator.CalculateCoinPerSecond(
                animal, tool, animalLevel: 1, toolLevel: 1, difficulty: DifficultyType.Normal, difficultyTable: null, townUpgradeMultiplier: 1f);

            Assert.AreEqual(5f, result, 0.0001f);
        }

        [Test]
        public void CalculateCoinPerSecond_동물레벨이_오르면_동물생산량이_증가한다()
        {
            TestAnimalEntry animal = CreateAnimal("A_CAT", 2f);
            TestToolEntry tool = CreateTool(3f, 0.2f, string.Empty, 0f);

            // 동물 2 * (1 + 2*0.2 = 1.4) + 도구 3 = 2.8 + 3 = 5.8
            float result = FinalProductionCalculator.CalculateCoinPerSecond(
                animal, tool, animalLevel: 3, toolLevel: 1, difficulty: DifficultyType.Normal, difficultyTable: null, townUpgradeMultiplier: 1f);

            Assert.AreEqual(5.8f, result, 0.0001f);
        }

        [Test]
        public void CalculateCoinPerSecond_도구레벨이_오르면_도구생산량이_증가한다()
        {
            TestAnimalEntry animal = CreateAnimal("A_CAT", 2f);
            TestToolEntry tool = CreateTool(3f, 0.2f, string.Empty, 0f);

            // 동물 2 + 도구 3 * (1 + 2*0.2 = 1.4) = 2 + 4.2 = 6.2
            float result = FinalProductionCalculator.CalculateCoinPerSecond(
                animal, tool, animalLevel: 1, toolLevel: 3, difficulty: DifficultyType.Normal, difficultyTable: null, townUpgradeMultiplier: 1f);

            Assert.AreEqual(6.2f, result, 0.0001f);
        }

        [Test]
        public void CalculateCoinPerSecond_특화_동물이면_동물과_도구_합산생산량_전체에_보너스가_붙는다()
        {
            TestAnimalEntry animal = CreateAnimal("A_CAT", 2f);
            TestToolEntry tool = CreateTool(3f, 0.2f, "A_CAT", 0.5f);

            // (동물 2 + 도구 3) * 1.5(특화 보너스) = 7.5
            float result = FinalProductionCalculator.CalculateCoinPerSecond(
                animal, tool, animalLevel: 1, toolLevel: 1, difficulty: DifficultyType.Normal, difficultyTable: null, townUpgradeMultiplier: 1f);

            Assert.AreEqual(7.5f, result, 0.0001f);
        }

        [Test]
        public void CalculateCoinPerSecond_난이도_배율이_합산된_전체_생산량에_적용된다()
        {
            TestAnimalEntry animal = CreateAnimal("A_CAT", 2f);
            TestToolEntry tool = CreateTool(3f, 0.2f, string.Empty, 0f);
            DifficultyProductionTable table = CreateDifficultyTable(DifficultyType.Hard, 0.5f);

            // (동물 2 + 도구 3) * 0.5(난이도) = 2.5
            float result = FinalProductionCalculator.CalculateCoinPerSecond(
                animal, tool, animalLevel: 1, toolLevel: 1, difficulty: DifficultyType.Hard, difficultyTable: table, townUpgradeMultiplier: 1f);

            Assert.AreEqual(2.5f, result, 0.0001f);
        }

        [Test]
        public void CalculateCoinPerSecond_마을업그레이드_배율이_합산_결과_전체에_반영된다()
        {
            TestAnimalEntry animal = CreateAnimal("A_CAT", 2f);
            TestToolEntry tool = CreateTool(3f, 0.2f, string.Empty, 0f);

            // (동물 2 + 도구 3) * 2(마을업그레이드 배율) = 10
            float result = FinalProductionCalculator.CalculateCoinPerSecond(
                animal, tool, animalLevel: 1, toolLevel: 1, difficulty: DifficultyType.Normal, difficultyTable: null, townUpgradeMultiplier: 2f);

            Assert.AreEqual(10f, result, 0.0001f);
        }

        [Test]
        public void CalculateCoinPerSecond_모든_보정값이_함께_적용된다()
        {
            TestAnimalEntry animal = CreateAnimal("A_CAT", 2f);
            TestToolEntry tool = CreateTool(3f, 0.2f, "A_CAT", 0.5f);
            DifficultyProductionTable table = CreateDifficultyTable(DifficultyType.Hard, 0.5f);

            // 동물 2*1.4=2.8, 도구 3*1.4=4.2, 합 7 * 1.5(특화) = 10.5 * 0.5(난이도) * 2(마을업그레이드) = 10.5
            float result = FinalProductionCalculator.CalculateCoinPerSecond(
                animal, tool, animalLevel: 3, toolLevel: 3, difficulty: DifficultyType.Hard, difficultyTable: table, townUpgradeMultiplier: 2f);

            Assert.AreEqual(10.5f, result, 0.0001f);
        }

        [Test]
        public void CalculateCoinPerSecond_도구가_없어도_동물_생산량만으로_값을_반환한다()
        {
            TestAnimalEntry animal = CreateAnimal("A_CAT", 2f);

            // 도구 없이 동물 생산량만: 2 * 1(레벨1) = 2
            float result = FinalProductionCalculator.CalculateCoinPerSecond(
                animal, null, animalLevel: 1, toolLevel: 1, difficulty: DifficultyType.Normal, difficultyTable: null, townUpgradeMultiplier: 1f);

            Assert.AreEqual(2f, result, 0.0001f);
        }

        [Test]
        public void CalculateCoinPerSecond_도구가_없어도_동물레벨_난이도_마을업그레이드는_반영된다()
        {
            TestAnimalEntry animal = CreateAnimal("A_CAT", 2f);
            DifficultyProductionTable table = CreateDifficultyTable(DifficultyType.Hard, 0.5f);

            // 동물 2 * 1.4(레벨3) = 2.8 * 0.5(난이도) * 2(마을업그레이드) = 2.8
            float result = FinalProductionCalculator.CalculateCoinPerSecond(
                animal, null, animalLevel: 3, toolLevel: 1, difficulty: DifficultyType.Hard, difficultyTable: table, townUpgradeMultiplier: 2f);

            Assert.AreEqual(2.8f, result, 0.0001f);
        }

        [Test]
        public void CalculateCoinPerSecond_동물이_없어도_도구_생산량만으로_값을_반환한다()
        {
            TestToolEntry tool = CreateTool(3f, 0.2f, string.Empty, 0f);

            // 동물 없이 도구 생산량만: 3 * 1(레벨1) = 3
            float result = FinalProductionCalculator.CalculateCoinPerSecond(
                null, tool, animalLevel: 1, toolLevel: 1, difficulty: DifficultyType.Normal, difficultyTable: null, townUpgradeMultiplier: 1f);

            Assert.AreEqual(3f, result, 0.0001f);
        }

        [Test]
        public void CalculateCoinPerSecond_동물이_없어도_도구레벨_난이도_마을업그레이드는_반영된다()
        {
            TestToolEntry tool = CreateTool(3f, 0.2f, string.Empty, 0f);
            DifficultyProductionTable table = CreateDifficultyTable(DifficultyType.Hard, 0.5f);

            // 도구 3 * 1.4(레벨3) = 4.2 * 0.5(난이도) * 2(마을업그레이드) = 4.2
            float result = FinalProductionCalculator.CalculateCoinPerSecond(
                null, tool, animalLevel: 1, toolLevel: 3, difficulty: DifficultyType.Hard, difficultyTable: table, townUpgradeMultiplier: 2f);

            Assert.AreEqual(4.2f, result, 0.0001f);
        }

        [Test]
        public void CalculateCoinPerSecond_동물과_도구가_둘다_없으면_0을_반환한다()
        {
            float result = FinalProductionCalculator.CalculateCoinPerSecond(
                null, null, animalLevel: 1, toolLevel: 1, difficulty: DifficultyType.Normal, difficultyTable: null, townUpgradeMultiplier: 1f);

            Assert.AreEqual(0f, result, 0.0001f);
        }
    }
}
