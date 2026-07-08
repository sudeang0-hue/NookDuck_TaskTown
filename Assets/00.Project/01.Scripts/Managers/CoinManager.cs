using UnityEngine;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance { get; private set; }

    [Header("Current Coin")]
    public long totalCoin { get; private set; }

    public event System.Action<long> OnCoinChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AddCoin(int amount)
    {
        if (amount <= 0) return;
        totalCoin += amount;

        OnCoinChanged?.Invoke(totalCoin);

        Debug.Log($"현재 재화: {totalCoin}");
    }

    public void RemoveCoin(int amount)
    {
        if (amount <= 0) return;
        if (totalCoin < amount) return;

        totalCoin -= amount;

        OnCoinChanged?.Invoke(totalCoin);

        Debug.Log($"현재 재화: {totalCoin}");
    }
}
