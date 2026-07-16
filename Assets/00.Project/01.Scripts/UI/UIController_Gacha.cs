using System;
using TaskTown.Gacha;
using UnityEngine;
using UnityEngine.UI;

public class UIController_Gacha : MonoBehaviour
{
    [Header("�̱� �ý���")]
    [SerializeField] private GachaManagerBase animalGachaManager;
    [SerializeField] private GachaManagerBase toolGachaManager;

    [Tooltip("ICoinWallet을 구현한 컴포넌트(CoinManager)를 연결합니다. 비워두면 코인 확인 없이 뽑기를 진행합니다.")]
    [SerializeField] private MonoBehaviour coinWalletSource;
    private ICoinWallet CoinWallet => coinWalletSource as ICoinWallet;

    private const int MultiRollCount = 10;

    [Header("���� �̱� ��ư")]
    [SerializeField] private Button animalOnePickButton;  // ���� 1ȸ �̱� ��ư
    [SerializeField] private Button animalTenPickButton;  // ���� 10ȸ �̱� ��ư
                                                          
    [Header("���� �̱� ��ư")]
    [SerializeField] private Button toolOnePickButton;    // ���� 1ȸ �̱� ��ư
    [SerializeField] private Button toolTenPickButton;    // ���� 10ȸ �̱� ��ư


    private void Awake()
    {
        if(animalGachaManager == null)
        {

        }

        if(toolGachaManager == null)
        {

        }
    }
    private void Start()
    {
        Subscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }
    
    /// <summary>
    /// Ŭ�� �̺�Ʈ ����
    /// </summary>
    private void Subscribe()
    {

        if (animalOnePickButton != null)
            animalOnePickButton.onClick.AddListener(OnClickPickUpAnimalOneTime);

        if (animalTenPickButton != null)
            animalTenPickButton.onClick.AddListener(OnClickPickUpAnimalTenTime);

        if(toolOnePickButton != null)
            toolOnePickButton.onClick.AddListener (OnClickPickUpToolOneTime);

        if (toolTenPickButton != null)
            toolTenPickButton.onClick.AddListener(OnClickPickUpToolTenTime);
    }

    /// <summary>
    /// Ŭ�� �̺�Ʈ ���� ����
    /// </summary>
    private void Unsubscribe()
    {
        if (animalOnePickButton != null)
            animalOnePickButton.onClick.RemoveListener(OnClickPickUpAnimalOneTime);

        if (animalTenPickButton != null)
            animalTenPickButton.onClick.RemoveListener(OnClickPickUpAnimalTenTime);

        if (toolOnePickButton != null)
            toolOnePickButton.onClick.RemoveListener(OnClickPickUpToolOneTime);

        if (toolTenPickButton != null)
            toolTenPickButton.onClick.RemoveListener(OnClickPickUpToolTenTime);
    }

    private void OnClickPickUpAnimalOneTime()
    {
        if (animalGachaManager == null) return;
        if (!TrySpendCost(animalGachaManager.GetCost(1))) return;

        animalGachaManager.Roll();
    }


    private void OnClickPickUpAnimalTenTime()
    {
        if (animalGachaManager == null) return;
        if (!TrySpendCost(animalGachaManager.GetCost(MultiRollCount))) return;

        animalGachaManager.RollMulti(MultiRollCount);
    }

    private void OnClickPickUpToolOneTime()
    {
        if (toolGachaManager == null) return;
        if (!TrySpendCost(toolGachaManager.GetCost(1))) return;

        toolGachaManager.Roll();
    }

    private void OnClickPickUpToolTenTime()
    {
        if (toolGachaManager == null) return;
        if (!TrySpendCost(toolGachaManager.GetCost(MultiRollCount))) return;

        toolGachaManager.RollMulti(MultiRollCount);
    }

    // 코인이 부족하면 뽑기를 진행하지 않습니다. coinWalletSource가 비어있으면 코인 확인 없이 진행합니다.
    private bool TrySpendCost(long cost)
    {
        if (CoinWallet == null)
        {
            Debug.LogWarning("[UIController_Gacha] ICoinWallet이 연결되지 않아 코인 확인 없이 뽑기를 진행합니다.");
            return true;
        }

        if (!CoinWallet.TrySpend(cost))
        {
            Debug.Log($"[UIController_Gacha] 코인이 부족합니다. (필요: {cost}, 보유: {CoinWallet.Balance})");
            return false;
        }

        return true;
    }
}
