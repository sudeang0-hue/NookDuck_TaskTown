using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace TaskTown.Gacha.Demo
{
    // 유니티 Play 모드에서 버튼으로 실제 뽑기를 눌러볼 수 있게 해주는 테스트용 UI입니다.
    // 실제 서비스용 UI(김아영 담당)가 만들어지면 이 스크립트는 참고용/테스트용으로만 남습니다.
    public class GachaDemoUI : MonoBehaviour
    {
        [Header("Gacha")]
        [SerializeField] private AnimalGachaManager animalGachaManager;
        [SerializeField] private ToolGachaManager toolGachaManager;
        [SerializeField] private DemoTownLevelProvider townLevelProvider;

        [Header("Buttons")]
        [SerializeField] private Button animalGachaButton;
        [SerializeField] private Button toolGachaButton;
        [SerializeField] private Button animalGachaX10Button;
        [SerializeField] private Button toolGachaX10Button;
        [SerializeField] private Button increaseTownLevelButton;

        [Header("Texts")]
        [SerializeField] private Text resultText;
        [SerializeField] private Text costText;
        [SerializeField] private Text townLevelText;

        private const int MultiRollCount = 10;

        private void Awake()
        {
            if (animalGachaButton != null) animalGachaButton.onClick.AddListener(RollAnimal);
            if (toolGachaButton != null) toolGachaButton.onClick.AddListener(RollTool);
            if (animalGachaX10Button != null) animalGachaX10Button.onClick.AddListener(RollAnimalMulti);
            if (toolGachaX10Button != null) toolGachaX10Button.onClick.AddListener(RollToolMulti);
            if (increaseTownLevelButton != null) increaseTownLevelButton.onClick.AddListener(IncreaseTownLevel);

            if (animalGachaManager != null) animalGachaManager.OnGachaResolved += HandleAnimalResult;
            if (toolGachaManager != null) toolGachaManager.OnGachaResolved += HandleToolResult;
        }

        private void Start()
        {
            RefreshInfoTexts();
        }

        private void OnDestroy()
        {
            if (animalGachaManager != null) animalGachaManager.OnGachaResolved -= HandleAnimalResult;
            if (toolGachaManager != null) toolGachaManager.OnGachaResolved -= HandleToolResult;
        }

        private void RollAnimal()
        {
            if (animalGachaManager == null) return;
            animalGachaManager.Roll();
            RefreshInfoTexts();
        }

        private void RollTool()
        {
            if (toolGachaManager == null) return;
            toolGachaManager.Roll();
            RefreshInfoTexts();
        }

        private void RollAnimalMulti()
        {
            if (animalGachaManager == null) return;
            List<GachaResult> results = animalGachaManager.RollMulti(MultiRollCount);
            ShowMultiResult("Animal Gacha", results);
            RefreshInfoTexts();
        }

        private void RollToolMulti()
        {
            if (toolGachaManager == null) return;
            List<GachaResult> results = toolGachaManager.RollMulti(MultiRollCount);
            ShowMultiResult("Tool Gacha", results);
            RefreshInfoTexts();
        }

        private void IncreaseTownLevel()
        {
            if (townLevelProvider == null) return;
            townLevelProvider.IncreaseLevel();
            RefreshInfoTexts();
        }

        private void HandleAnimalResult(GachaResult result)
        {
            ShowResult("Animal Gacha", result);
        }

        private void HandleToolResult(GachaResult result)
        {
            ShowResult("Tool Gacha", result);
        }

        private void ShowResult(string gachaName, GachaResult result)
        {
            if (resultText == null) return;

            string entryName = result.Entry != null ? result.Entry.DisplayName : "(No entry registered for this grade)";
            resultText.text = $"{gachaName} Result : [{result.Grade}] {entryName}";
        }

        private void ShowMultiResult(string gachaName, List<GachaResult> results)
        {
            if (resultText == null) return;

            StringBuilder builder = new StringBuilder();
            builder.Append(gachaName).Append(" x").Append(results.Count).Append(" Result : ");
            for (int i = 0; i < results.Count; i++)
            {
                if (i > 0) builder.Append(", ");
                string entryName = results[i].Entry != null ? results[i].Entry.DisplayName : "(No entry)";
                builder.Append('[').Append(results[i].Grade).Append("] ").Append(entryName);
            }

            resultText.text = builder.ToString();
        }

        private void RefreshInfoTexts()
        {
            if (costText != null)
            {
                long animalCost = animalGachaManager != null ? animalGachaManager.CurrentCost : 0;
                long toolCost = toolGachaManager != null ? toolGachaManager.CurrentCost : 0;
                long animalCostX10 = animalGachaManager != null ? animalGachaManager.GetCost(MultiRollCount) : 0;
                long toolCostX10 = toolGachaManager != null ? toolGachaManager.GetCost(MultiRollCount) : 0;
                costText.text = $"Animal Gacha Cost : {animalCost} (x10: {animalCostX10})   /   Tool Gacha Cost : {toolCost} (x10: {toolCostX10})";
            }

            if (townLevelText != null && townLevelProvider != null)
            {
                townLevelText.text = $"Current Town Level : {townLevelProvider.CurrentTownLevel}";
            }
        }
    }
}
