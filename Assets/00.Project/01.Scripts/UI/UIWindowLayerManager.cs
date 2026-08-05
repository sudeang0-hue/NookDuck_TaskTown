using UnityEngine;

/// <summary>
/// ���� �ٸ� Canvas�� ��ġ�� UI â(�� ������ ��)�� ǥ�� ������ �����մϴ�.
/// </summary>
public class UIWindowLayerManager : MonoBehaviour
{
    public static UIWindowLayerManager Instance { get; private set; }

    [Header("â ���� ����")]
    [SerializeField] private int baseSortingOrder = 10;

    private int currentSortingOrder;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[UIWindowLayerManager] �ߺ��� ���̾� �����ڰ� �����Ͽ� �����մϴ�.");

            Destroy(gameObject);
            return;
        }

        Instance = this;
        currentSortingOrder = baseSortingOrder;
    }

    /// <summary>
    /// ���޹��� Canvas�� ���� UI â �� ���� ������ �̵��մϴ�.
    /// </summary>
    public void BringToFront(Canvas targetCanvas)
    {
        if (targetCanvas == null)
        {
            Debug.LogWarning("[UIWindowLayerManager] ������ �̵��� Canvas�� �����ϴ�.");

            return;
        }

        currentSortingOrder++;

        targetCanvas.overrideSorting = true;
        targetCanvas.sortingOrder = currentSortingOrder;
    }
}
