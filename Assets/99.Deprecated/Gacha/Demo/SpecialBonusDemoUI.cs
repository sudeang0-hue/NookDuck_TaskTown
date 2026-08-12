using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace TaskTown.Gacha.Demo
{
    // SpecialBonusCalculator 결과를 눈으로 확인하기 위한 테스트 전용 데모입니다.
    // 실제 서비스용 UI가 아니라, 등록해둔 도구/동물 조합의 특화 보너스 적용 여부와 생산량을 표로 보여줍니다.
    // tools/animals는 GachaEntryData로 받습니다(구체 타입은 Assembly-CSharp에 있어 이 어셈블리에서
    // 직접 참조할 수 없음). 인스펙터에서 ToolDataSO/AnimalDataSO 에셋을 그대로 드래그해서 넣으면 됩니다.
    public class SpecialBonusDemoUI : MonoBehaviour
    {
        [SerializeField] private GachaEntryData[] tools;
        [SerializeField] private GachaEntryData[] animals;
        [SerializeField] private Text resultText;
        [SerializeField] private Button refreshButton;

        private void Awake()
        {
            if (refreshButton != null) refreshButton.onClick.AddListener(RefreshResultText);
        }

        private void Start()
        {
            RefreshResultText();
        }

        private void RefreshResultText()
        {
            if (resultText == null) return;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Tool + Animal => Coin/s");
            foreach (GachaEntryData tool in tools)
            {
                if (tool == null) continue;

                foreach (GachaEntryData animal in animals)
                {
                    if (animal == null) continue;

                    bool isMatch = SpecialBonusCalculator.IsSpecialMatch(tool, animal);
                    float coinPerSecond = SpecialBonusCalculator.CalculateCoinPerSecond(tool, animal);
                    string tag = isMatch ? "(특화 적용)" : string.Empty;

                    sb.AppendLine($"{tool.DisplayName} + {animal.DisplayName} => {coinPerSecond:0.##} {tag}");
                }
            }

            resultText.text = sb.ToString();
        }
    }
}
