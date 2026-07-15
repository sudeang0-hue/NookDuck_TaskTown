using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TaskTown.Gacha.Tests
{
    public class SpecialBonusCalculatorTests
    {
        private static TestAnimalEntry CreateAnimal(string id)
        {
            TestAnimalEntry data = ScriptableObject.CreateInstance<TestAnimalEntry>();
            SerializedObject so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }

        private static TestToolEntry CreateTool(float baseCoinPerSecond, string specialAnimalId, float bonusRate)
        {
            TestToolEntry data = ScriptableObject.CreateInstance<TestToolEntry>();
            SerializedObject so = new SerializedObject(data);
            so.FindProperty("baseCoinPerSecond").floatValue = baseCoinPerSecond;
            so.FindProperty("specialAnimalId").stringValue = specialAnimalId;
            so.FindProperty("specialAnimalBonusRate").floatValue = bonusRate;
            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }

        [Test]
        public void IsSpecialMatch_동물ID가_도구의_특화ID와_같으면_true()
        {
            TestAnimalEntry cat = CreateAnimal("A_CAT");
            TestToolEntry tool = CreateTool(3f, "A_CAT", 0.5f);

            Assert.IsTrue(SpecialBonusCalculator.IsSpecialMatch(tool, cat));
        }

        [Test]
        public void IsSpecialMatch_동물ID가_다르면_false()
        {
            TestAnimalEntry duck = CreateAnimal("A_DUCK");
            TestToolEntry tool = CreateTool(3f, "A_CAT", 0.5f);

            Assert.IsFalse(SpecialBonusCalculator.IsSpecialMatch(tool, duck));
        }

        [Test]
        public void IsSpecialMatch_배치된_동물이_없으면_false()
        {
            TestToolEntry tool = CreateTool(3f, "A_CAT", 0.5f);

            Assert.IsFalse(SpecialBonusCalculator.IsSpecialMatch(tool, null));
        }

        [Test]
        public void CalculateCoinPerSecond_특화_동물이면_보너스가_적용된다()
        {
            TestAnimalEntry cat = CreateAnimal("A_CAT");
            TestToolEntry tool = CreateTool(3f, "A_CAT", 0.5f);

            Assert.AreEqual(4.5f, SpecialBonusCalculator.CalculateCoinPerSecond(tool, cat), 0.0001f);
        }

        [Test]
        public void CalculateCoinPerSecond_특화_동물이_아니면_기본_생산량만_적용된다()
        {
            TestAnimalEntry duck = CreateAnimal("A_DUCK");
            TestToolEntry tool = CreateTool(3f, "A_CAT", 0.5f);

            Assert.AreEqual(3f, SpecialBonusCalculator.CalculateCoinPerSecond(tool, duck), 0.0001f);
        }

        [Test]
        public void CalculateCoinPerSecond_동물이_배치되지_않으면_기본_생산량만_적용된다()
        {
            TestToolEntry tool = CreateTool(3f, "A_CAT", 0.5f);

            Assert.AreEqual(3f, SpecialBonusCalculator.CalculateCoinPerSecond(tool, null), 0.0001f);
        }
    }
}
