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

    //-----------------26.08.05 KDH-------------------------
    private void OnDestroy()
    {
        // 씬 전환 후 파괴된 CoinManager를 Instance가 계속 가리키지 않게 합니다.
        if (Instance == this)
            Instance = null;
    }
    //----------------------------------------

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

        Debug.Log($"Current Coin: {totalCoin}");
    }

    public void RemoveCoin(int amount)
    {
        if (amount <= 0) return;
        if (totalCoin < amount) return;

        totalCoin -= amount;

        OnCoinChanged?.Invoke(totalCoin);

        Debug.Log($"Current Coin: {totalCoin}");
    }

    // 디버그/치트용: 보유 코인을 원하는 값으로 덮어씁니다.
    // Add/Remove와 달리 증감이 아니라 절대값 설정이라 Update에서 쓰지 말고 버튼/이벤트에서만 호출하세요.
    public void SetCoin(long value)
    {
        // 음수 코인은 허용하지 않음
        totalCoin = value < 0 ? 0 : value;
        OnCoinChanged?.Invoke(totalCoin);
    }
}
