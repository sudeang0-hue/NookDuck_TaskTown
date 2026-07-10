using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TaskTown.Gacha.Tests
{
    public class ToolDataTests
    {
        private static ToolData CreateTool(float baseCoinPerSecond, float levelBonusRatePerLevel)
        {
            ToolData data = ScriptableObject.CreateInstance<ToolData>();
            SerializedObject so = new SerializedObject(data);
            so.FindProperty("baseCoinPerSecond").floatValue = baseCoinPerSecond;
            so.FindProperty("levelBonusRatePerLevel").floatValue = levelBonusRatePerLevel;
            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }

        [Test]
        public void CalculateLevelMultiplier_레벨1이면_배율1이다()
        {
            ToolData tool = CreateTool(3f, 0.2f);

            Assert.AreEqual(1f, tool.CalculateLevelMultiplier(1), 0.0001f);
        }

        [Test]
        public void CalculateLevelMultiplier_레벨이_오를수록_배율이_증가한다()
        {
            ToolData tool = CreateTool(3f, 0.2f);

            // 레벨 3 = 1 + (3-1)*0.2 = 1.4
            Assert.AreEqual(1.4f, tool.CalculateLevelMultiplier(3), 0.0001f);
        }
    }
}
