using System;
using System.Reflection;
using NUnit.Framework;
using TaskTown.Gacha;
using TaskTown.KDH;
using UI;
using UnityEngine.UI;

namespace TaskTown.EditorTests.Tutorial
{
    public class TutorialEventContractTests
    {
        [TestCase(typeof(EarnProcessor), nameof(EarnProcessor.ManualCoinGranted), typeof(Action<int>))]
        [TestCase(typeof(GachaManagerBase), nameof(GachaManagerBase.OnGachaResolved), typeof(Action<GachaResult>))]
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
    }
}
