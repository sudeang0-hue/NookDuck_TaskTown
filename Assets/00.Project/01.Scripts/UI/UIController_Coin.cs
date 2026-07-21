using TMPro;
using UnityEngine;

public class UIController_Coin : MonoBehaviour
{

    [Header("ȹ�� ���� �ؽ�Ʈ")]
    [SerializeField] private TMP_Text allCoinText;   // ���� ���� ����
    [SerializeField] private TMP_Text autoCoinText;  // �ð��� ȹ�淮

    [Header("Text Format")]
    [SerializeField] private string prefix = " /h";

    private bool isSubscribed;

    private void Start()
    {
        SubscribeCoinEvent();
    }


    private void OnEnable()
    {
        SubscribeCoinEvent();
    }

    private void Update()
    {
        long coinPerHour = TaskTown.KDH.RealProductionTicker.Instance != null
            ? (long)(TaskTown.KDH.RealProductionTicker.Instance.CurrentCoinPerSecond * 3600f)
            : 0;

        UpdateAutoCoinText(coinPerHour);
    }


    private void OnDisable()
    {
        if (!isSubscribed)
            return;

        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.OnCoinChanged -= UpdateAllCoinText;
        }

        isSubscribed = false;
    }

    private void SubscribeCoinEvent()
    {
        if (isSubscribed)
            return;

        if (CoinManager.Instance == null)
        {
            Debug.LogWarning("[UIController_Coin] CoinManager.Instance�� �����ϴ�.");
            return;
        }

        CoinManager.Instance.OnCoinChanged += UpdateAllCoinText;
        isSubscribed = true;

        // ���� �� ��� �ݿ�
        UpdateAllCoinText(CoinManager.Instance.totalCoin);

        // �ڵ� ������ ���� ���� ����
        UpdateAutoCoinText(0);
    }


    /// <summary>
    /// ���� ���� ���� UIController_AnimalInvPage
    /// </summary>
    private void UpdateAllCoinText(long coinAmount)
    {
        if (allCoinText == null)
        {
            Debug.LogWarning("[UIController_Coin] allCoinText�� ������� �ʾҽ��ϴ�.");
            return;
        }

        allCoinText.text = coinAmount.ToString("N0");
    }


    /// <summary>
    /// �ð��� ���귮 UIController_AnimalInvPage
    /// </summary>
    private void UpdateAutoCoinText(long coinPerHour)
    {
        if (autoCoinText == null)
        {
            Debug.LogWarning("[UIController_Coin] autoCoinText�� ������� �ʾҽ��ϴ�.");
            return;
        }

        autoCoinText.text = $"{coinPerHour:N0}{prefix}";
    }


}
