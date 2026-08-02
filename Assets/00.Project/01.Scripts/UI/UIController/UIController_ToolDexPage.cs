using Animal.Data;
using TMPro;
using Tool.Data;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UIController_ToolDexPage : MonoBehaviour
{
    [Header("도구 상세 페이지 패널")]
    [FormerlySerializedAs("toolListPagePanel")]
    [SerializeField] private GameObject toolDexPagePanel;

    [Header("도구 상세 정보")]
    [SerializeField] private Image toolIconImage;
    [SerializeField] private TMP_Text toolNameText;
    [SerializeField] private TMP_Text toolDescriptionText;

    private ToolDataSO currentToolData;

    public ToolDataSO CurrentToolData => currentToolData;

    public string CurrentToolId => currentToolData != null ? currentToolData.Id : string.Empty;

    /// <summary>
    /// 상세 페이지 패널이 현재 열려 있는지 여부
    /// </summary>
    public bool IsOpen => toolDexPagePanel != null && toolDexPagePanel.activeSelf;

    private void Awake()
    {
        if (toolDexPagePanel != null)
        {
            toolDexPagePanel.SetActive(false);
        }
    }

    /// <summary>
    /// 도감 슬롯 클릭 시 선택한 동물의 정보로 상세 페이지를 갱신하고 패널을 엽니다.
    /// </summary>
    public void OpenToolDexPage(ToolDataSO toolData)
    {
        if (toolData == null)
        {
            Debug.LogWarning("[UIController_AnimalDexPage] 표시할 동물 데이터가 없습니다.");
            return;
        }

        if (toolDexPagePanel == null)
        {
            Debug.LogWarning("[UIController_AnimalDexPage] animalDexPagePanel이 연결되지 않았습니다.");
            return;
        }

        currentToolData = toolData;

        RefreshToolDexPage();

        toolDexPagePanel.SetActive(true);
    }

    /// <summary>
    /// 현재 선택된 동물 데이터로 상세 페이지를 갱신합니다.
    /// </summary>
    private void RefreshToolDexPage()
    {
        if (currentToolData == null)
            return;

        if (toolIconImage != null)
        {
            toolIconImage.sprite = currentToolData.Icon;
            toolIconImage.enabled = currentToolData.Icon != null;
        }

        if (toolNameText != null)
        {
            toolNameText.text = currentToolData.DisplayName;
        }

        if (toolDescriptionText != null)
        {
            toolDescriptionText.text = currentToolData.ToolDescription;
        }
    }

    /// <summary>
    /// 상세 페이지 닫기 버튼용.
    /// 선택 데이터도 함께 초기화합니다.
    /// </summary>
    public void CloseToolDexPage()
    {
        currentToolData = null;

        if (toolDexPagePanel == null)
            return;

        toolDexPagePanel.SetActive(false);
    }
}
