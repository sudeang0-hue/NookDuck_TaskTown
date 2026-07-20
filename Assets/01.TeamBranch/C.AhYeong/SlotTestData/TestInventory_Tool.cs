using System;
using System.Collections.Generic;
using TaskTown.Gacha;
using Tool.Data;
using UnityEngine;

namespace Test
{
    /// <summary>
    /// 실제 도구 인벤토리가 완성되기 전, UIController_AnimalInvPage 갱신을 확인하기 위한 임시 테스트 클래스
    /// </summary>
    public class TestInventory_Tool : MonoBehaviour
    {
        [Header("테스트용 보유 도구")]
        [SerializeField] private List<SlotData_Tool> toolSlots = new List<SlotData_Tool>();
        private Dictionary<string, SlotData_Tool> toolSlotsDic = new Dictionary<string, SlotData_Tool>();

        [Header("순차 획득 테스트")]
        [SerializeField]
        private List<ToolDataSO> testTools = new List<ToolDataSO>();

        [SerializeField] private int nextTestIndex;

        // 새로운 도구을 최초 획득했을 때 발생
        public event Action<string> OnToolAdded;
        // 이미 보유한 도구의 수량 등 데이터가 변경됐을 때 발생합니다.
        public event Action<string> OnToolChanged;
        // ui 초기화
        public event Action OnInventoryCleared;
        public IReadOnlyList<SlotData_Tool> ToolSlots => toolSlots;



        private void Awake()
        {
            InitializeExistingSlots();
        }

        /// <summary>
        /// 이미 리스트에 존재하는 도구 슬롯의
        /// 레벨업 요구 수량을 현재 레벨 기준으로 초기화합니다.
        /// </summary>
        private void InitializeExistingSlots()
        {
            toolSlotsDic.Clear();

            for (int i = toolSlots.Count - 1; i >= 0; i--)
            {
                SlotData_Tool slot = toolSlots[i];

                if (slot == null)
                {
                    toolSlots.RemoveAt(i);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(slot.ToolId))
                {
                    Debug.LogWarning("[TestInventory_Tool] ToolId가 비어 있는 슬롯을 제거합니다.");

                    toolSlots.RemoveAt(i);
                    continue;
                }

                if (toolSlotsDic.ContainsKey(slot.ToolId))
                {
                    Debug.LogWarning($"[TestInventory_Tool] 중복된 도구 슬롯을 제거합니다: {slot.ToolId}");

                    toolSlots.RemoveAt(i);
                    continue;
                }

                RefreshSlotGrowthData(slot);
                toolSlotsDic.Add(slot.ToolId, slot);
            }
        }


        /// <summary>
        /// 디버깅용 도구 순차 획득.
        /// </summary>
        public void DebugAddNextTool()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[TestInventory_Tool] Play Mode에서 실행해야 합니다.");
                return;
            }


            if (testTools == null || testTools.Count == 0)
            {
                Debug.LogWarning("[TestInventory_Tool] 테스트 도구 목록이 비어 있습니다.");
                return;
            }

            if (nextTestIndex < 0 || nextTestIndex >= testTools.Count)
            {
                Debug.Log("[TestInventory_Tool] 모든 테스트 도구을 추가했습니다.");
                return;
            }

            ToolDataSO toolData = testTools[nextTestIndex];

            nextTestIndex++;

            AddTool(toolData);
        }



        public void DebugDeleteTool()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[TestInventory_Tool] Play Mode에서 실행해야 합니다.");
                return;
            }

            toolSlots.Clear();
            nextTestIndex = 0;

            OnInventoryCleared?.Invoke();

            Debug.Log("[TestInventory_Tool] 테스트용 도구 인벤토리를 비웠습니다.");
        }


        /// <summary>
        /// 도구을 획득합니다.
        /// 최초 획득이면 슬롯을 생성하고, 중복 획득이면 기존 슬롯의 수량을 증가시킵니다.
        /// </summary>
        public void AddTool(ToolDataSO toolData, int amount = 1)
        {
            if (toolData == null)
            {
                Debug.LogWarning("[TestInventory_Tool] 추가할 도구 데이터가 없습니다.");
                return;
            }

            if (string.IsNullOrEmpty(toolData.Id))
            {
                Debug.LogWarning("[TestInventory_Tool] 도구 ID가 비어 있습니다.");
                return;
            }

            if (amount <= 0)
            {
                Debug.LogWarning("[TestInventory_Tool] 획득 수량은 1 이상이어야 합니다.");
                return;
            }

            if (TryGetToolSlot(toolData.Id, out SlotData_Tool slotData))
            {
                slotData.AddCount(amount);

                OnToolChanged?.Invoke(toolData.Id);

                Debug.Log($"[TestInventory_Tool] 중복 도구 획득: {toolData.DisplayName} / 현재 수량: {slotData.CurrentCount}");
                return;
            }

            SlotData_Tool newSlot = new SlotData_Tool(toolData, 1, amount);

            RefreshSlotGrowthData(newSlot);

            toolSlots.Add(newSlot);
            toolSlotsDic.Add(toolData.Id, newSlot);

            OnToolAdded?.Invoke(toolData.Id);

            Debug.Log($"[TestInventory_Tool] 신규 도구 획득: {toolData.DisplayName} / 수량: {amount}");
        }

        /// <summary>
        /// 해당 ID의 도구 슬롯을 찾습니다.
        /// </summary>
        public bool TryGetToolSlot(
            string toolId,
            out SlotData_Tool slotData)
        {
            slotData = null;

            if (string.IsNullOrEmpty(toolId))
                return false;

            for (int i = 0; i < toolSlots.Count; i++)
            {
                SlotData_Tool currentSlot = toolSlots[i];

                if (currentSlot == null)
                    continue;

                if (currentSlot.ToolId != toolId)
                    continue;

                slotData = currentSlot;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 현재 도구 레벨을 기준으로 다음 레벨업에 필요한
        /// 총 보유 수량을 계산하여 런타임 슬롯에 적용
        /// </summary>
        private void RefreshSlotGrowthData(SlotData_Tool slot)
        {
            if (slot == null)
                return;

            int requiredCount = LevelUpRequirementCalculator.GetRequiredDuplicateCount(slot.Level);

            slot.ApplyGrowthData(requiredCount, slot.LevelUpCost, false);
        }



        ///// <summary>
        ///// 도감에서 사용할 획득 여부 확인입니다.
        ///// </summary>
        //public bool HasAcquiredTool(string toolId)
        //{
        //    return TryGetToolSlot(toolId, out _);
        //}

        public bool TryLevelUpTool(string toolId)
        {
            Debug.Log("[TestInventory_Tool] 도구 레벨업 실행. 레벨업이 성공하면 슬롯 갱신.");

            #region 레벨업으로 인한 도구 소모 계산 테스트 코드

            if (string.IsNullOrWhiteSpace(toolId))
            {
                Debug.LogWarning("[TestInventory_Tool] 레벨업할 ToolId가 비어 있습니다.");
                return false;
            }

            if (!TryGetToolSlot(toolId, out SlotData_Tool slotData))
            {
                Debug.LogWarning($"[TestInventory_Tool] 레벨업할 도구 슬롯을 찾지 못했습니다: {toolId}");

                return false;
            }

            if (!slotData.CanLevelUp())
            {
                Debug.Log($"[TestInventory_Tool] 레벨업 조건 부족: " +
                    $"{toolId} / " +
                    $"현재 수량 {slotData.CurrentCount} / " +
                    $"필요 수량 {slotData.RequiredUpgradeCount}");

                return false;
            }

            int previousLevel = slotData.Level;
            int previousCount = slotData.CurrentCount;

            if (!slotData.TryConsumeForLevelUp())
            {
                Debug.LogWarning($"[TestInventory_Tool] 레벨업 수량 소비에 실패했습니다: {toolId}");
                return false;
            }

            slotData.IncreaseLevel();

            // 레벨이 변경되었으므로 다음 레벨 요구 수량을 다시 계산
            RefreshSlotGrowthData(slotData);

            Debug.Log(
                $"[TestInventory_Tool] 도구 레벨업 성공: " +
                $"{toolId} / " +
                $"Lv.{previousLevel} → Lv.{slotData.Level} / " +
                $"수량 {previousCount} → {slotData.CurrentCount} / " +
                $"다음 필요 수량 {slotData.RequiredUpgradeCount}");

            #endregion

            // 데이터 변경 완료 후 UIController_AnimalInvPage 갱신 요청
            OnToolChanged?.Invoke(toolId);
            return true;
        }
    }
}
