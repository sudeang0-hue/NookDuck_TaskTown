using UnityEngine;
using TaskTown.Gacha;

// 뽑기 시스템(TaskTown.Gacha.Demo)은 어셈블리 경계상 이 클래스를 직접 참조할 수 없어서
// ICoinWallet을 통해 연결합니다. 자세한 이유는 ICoinWallet 주석 참고.
public class CoinManager : MonoBehaviour, ICoinWallet
{
    public static CoinManager Instance { get; private set; }

    [Header("Current Coin")]
    public long totalCoin { get; private set; }

    public event System.Action<long> OnCoinChanged;

    public long Balance => totalCoin;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ICoinWallet 구현. 기존 AddCoin/RemoveCoin(int)은 EarnProcessor 등 기존 코드가 쓰고 있어 그대로 둡니다.
    public void Add(long amount)
    {
        if (amount <= 0) return;
        totalCoin += amount;
        OnCoinChanged?.Invoke(totalCoin);
    }

    public bool TrySpend(long amount)
    {
        if (amount <= 0) return true;
        if (totalCoin < amount) return false;

        totalCoin -= amount;
        OnCoinChanged?.Invoke(totalCoin);
        return true;
    }

    public void AddCoin(int amount)
    {
        if (amount <= 0) return;
        totalCoin += amount;

        OnCoinChanged?.Invoke(totalCoin);

        Debug.Log($"���� ��ȭ: {totalCoin}");
    }

    public void RemoveCoin(int amount)
    {
        if (amount <= 0) return;
        if (totalCoin < amount) return;

        totalCoin -= amount;

        OnCoinChanged?.Invoke(totalCoin);

        Debug.Log($"���� ��ȭ: {totalCoin}");
    }
}
