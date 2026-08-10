using System;
using System.Reflection;
using NUnit.Framework;
using TaskTown.Gacha;
using TaskTown.KDH;
using TaskTown.Tutorial;
using UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TaskTown.EditorTests.Tutorial
{
    public class TutorialEventContractTests
    {
        [TestCase(typeof(EarnProcessor), nameof(EarnProcessor.ManualCoinGranted), typeof(Action<int>))]
        [TestCase(typeof(GachaManagerBase), nameof(GachaManagerBase.OnGachaResolved), typeof(Action<GachaResult>))]
        [TestCase(typeof(GachaPortalController), nameof(GachaPortalController.ResultConfirmed), typeof(Action))]
        [TestCase(typeof(GachaDirector), nameof(GachaDirector.ResultOpened), typeof(Action))]
        [TestCase(typeof(GachaDirector), nameof(GachaDirector.ResultConfirmed), typeof(Action))]
        [TestCase(typeof(UIController_Gacha), nameof(UIController_Gacha.PanelOpened), typeof(Action))]
        [TestCase(typeof(InventoryManager_Tool), nameof(InventoryManager_Tool.OnToolSlotChanged), typeof(Action<SlotData_Tool>))]
        [TestCase(typeof(RealProductionTicker), nameof(RealProductionTicker.ProductionCoinGranted), typeof(Action<int>))]
        [TestCase(typeof(VillageAnimalSetUI_Manager), nameof(VillageAnimalSetUI_Manager.OnVillagePlacementChanged), typeof(Action))]
        [TestCase(typeof(VillageInfoUI_Manager), nameof(VillageInfoUI_Manager.PanelOpened), typeof(Action))]
        [TestCase(typeof(TownUpgradeManager), nameof(TownUpgradeManager.UpgradePurchased), typeof(Action))]
        public void TutorialEventBridge가사용하는이벤트_공개계약을유지한다(
            Type sourceType,
            string eventName,
            Type expectedHandlerType)
        {
            EventInfo eventInfo = sourceType.GetEvent(eventName, BindingFlags.Public | BindingFlags.Instance);

            Assert.NotNull(eventInfo, $"{sourceType.Name}.{eventName} 이벤트가 필요합니다.");
            Assert.AreEqual(expectedHandlerType, eventInfo.EventHandlerType);
        }

        [TestCase(typeof(UIController_Menu), nameof(UIController_Menu.GachaButton))]
        [TestCase(typeof(UIController_Gacha), nameof(UIController_Gacha.AnimalOnePickButton))]
        [TestCase(typeof(UIController_Gacha), nameof(UIController_Gacha.ToolOnePickButton))]
        [TestCase(typeof(GachaPortalController), nameof(GachaPortalController.ConfirmButton))]
        [TestCase(typeof(GachaDirector), nameof(GachaDirector.ConfirmButton))]
        public void TutorialButtonHighlight가사용하는버튼_읽기전용계약을유지한다(
            Type sourceType,
            string propertyName)
        {
            PropertyInfo property = sourceType.GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.Instance);

            Assert.IsNotNull(property);
            Assert.AreEqual(typeof(Button), property.PropertyType);
            Assert.IsTrue(property.CanRead);
            Assert.IsFalse(property.CanWrite);
        }

        [Test]
        public void 자동생산강조가사용하는텍스트_읽기전용계약을유지한다()
        {
            PropertyInfo property = typeof(UIController_Coin).GetProperty(
                nameof(UIController_Coin.AutoCoinText),
                BindingFlags.Public | BindingFlags.Instance);

            Assert.IsNotNull(property);
            Assert.AreEqual(typeof(TMP_Text), property.PropertyType);
            Assert.IsTrue(property.CanRead);
            Assert.IsFalse(property.CanWrite);
        }

        [Test]
        public void 도구뽑기결과확인전에는_다음단계를일시정지한다()
        {
            GameObject tutorialObject = new(
                "ToolResultTutorialFlowTest",
                typeof(TutorialManager),
                typeof(TutorialEventBridge));
            GameObject directorObject = new(
                "ToolGachaDirectorTest",
                typeof(CanvasGroup),
                typeof(GachaDirector));

            try
            {
                TutorialManager manager =
                    tutorialObject.GetComponent<TutorialManager>();
                TutorialEventBridge bridge =
                    tutorialObject.GetComponent<TutorialEventBridge>();
                GachaDirector director = directorObject.GetComponent<GachaDirector>();

                SetPrivateField(
                    manager,
                    "stateMachine",
                    new TutorialStateMachine(new TutorialSaveData
                    {
                        currentStep = TutorialStep.DrawTool
                    }));
                SetPrivateField(bridge, "tutorialManager", manager);
                SetPrivateField(bridge, "toolGachaDirector", director);

                InvokePrivate(
                    bridge,
                    "HandleToolDrawn",
                    new object[] { null });

                Assert.AreEqual(TutorialStep.AssignAnimal, manager.CurrentStep);
                Assert.IsTrue(manager.IsPaused);
                Assert.IsTrue(GetPrivateField<bool>(
                    bridge,
                    "isWaitingForToolResultConfirmation"));

                InvokePrivate(bridge, "HandleToolResultConfirmed");

                Assert.IsFalse(manager.IsPaused);
                Assert.IsFalse(GetPrivateField<bool>(
                    bridge,
                    "isWaitingForToolResultConfirmation"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tutorialObject);
                UnityEngine.Object.DestroyImmediate(directorObject);
            }
        }

        private static object InvokePrivate(
            object target,
            string methodName,
            params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            return method.Invoke(target, arguments);
        }

        private static void SetPrivateField(
            object target,
            string fieldName,
            object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field);
            field.SetValue(target, value);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field);
            return (T)field.GetValue(target);
        }
    }
}
