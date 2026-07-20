using UnityEngine;

/// <summary>
/// 서로 다른 Canvas에 배치된 UIController_AnimalInvPage 창의 표시 순서를 관리합니다.
/// </summary>
public class UIWindowLayerManager : MonoBehaviour
{
    public static UIWindowLayerManager Instance { get; private set; }

    [Header("창 정렬 순서")]
    [SerializeField] private int baseSortingOrder = 10;

    private int currentSortingOrder;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[UIWindowLayerManager] 중복된 레이어 관리자가 존재하여 제거합니다.");

            Destroy(gameObject);
            return;
        }

        Instance = this;
        currentSortingOrder = baseSortingOrder;
    }

    /// <summary>
    /// 전달받은 Canvas를 현재 UIController_AnimalInvPage 창 중 가장 앞으로 이동합니다.
    /// </summary>
    public void BringToFront(Canvas targetCanvas)
    {
        if (targetCanvas == null)
        {
            Debug.LogWarning("[UIWindowLayerManager] 앞으로 이동할 Canvas가 없습니다.");

            return;
        }

        currentSortingOrder++;

        targetCanvas.overrideSorting = true;
        targetCanvas.sortingOrder = currentSortingOrder;
    }
}