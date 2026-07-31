//NB

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TaskTown.Gacha;
using TaskTown.KDH;
using Tool.Data;

public class GachaSystemBridge : MonoBehaviour
{
    [Header("Data Asset Reference")]
    [SerializeField] private GachaPoolData toolGachaPoolData;

    [Header("Teammate's System References")]
    [Tooltip("도구 가챠 전용 매니저 컴포넌트를 할당하세요.")]
    [SerializeField] private ToolGachaManager gachaManager;

    [Tooltip("ICoinWallet을 구현한 코인 관리자 컴포넌트 (예: CoinManager)")]
    [SerializeField] private MonoBehaviour coinWalletSource;
    private ICoinWallet CoinWallet => coinWalletSource as ICoinWallet;

    [Header("UI Window References")]
    [SerializeField] private GameObject teammateInitialWindow;
    [SerializeField] private Button btnOpenGachaWindow;
    [SerializeField] private Button btnDraw1;
    [SerializeField] private Button btnDraw10;

    [Header("Result Window Re-roll Buttons")]
    [SerializeField] private Button btnResultDraw1;
    [SerializeField] private Button btnResultDraw10;

    [Header("Director Reference")]
    [SerializeField] private GachaDirector gachaDirector;

    private bool _isOpen = false;
    private float _lastClickTime = 0f;
    private const float CLICK_THRESHOLD = 0.25f;

    private void Awake()
    {
        if (gachaDirector == null)
        {
            gachaDirector = FindAnyObjectByType<GachaDirector>(FindObjectsInactive.Include);
        }
    }

    private void OnEnable() => InitButtonListeners();

    // Start() 시점에 한 번 더 리스너를 정리하여 타 스크립트의 Start() 중복 바인딩을 방지
    private void Start()
    {
        InitButtonListeners();
    }

    private void OnDisable() => RemoveButtonListeners();

    private void InitButtonListeners()
    {
        // 외부 스크립트에서 C# 코드로 추가했을 수 있는 리스너까지 싹 제거 후 단일 바인딩
        if (btnOpenGachaWindow != null)
        {
            btnOpenGachaWindow.onClick.RemoveAllListeners();
            btnOpenGachaWindow.onClick.AddListener(ToggleGachaWindow);
        }

        if (btnDraw1 != null)
        {
            btnDraw1.onClick.RemoveAllListeners();
            btnDraw1.onClick.AddListener(() => OnClickedDrawGacha(1));
        }

        if (btnDraw10 != null)
        {
            btnDraw10.onClick.RemoveAllListeners();
            btnDraw10.onClick.AddListener(() => OnClickedDrawGacha(10));
        }

        if (btnResultDraw1 != null)
        {
            btnResultDraw1.onClick.RemoveAllListeners();
            btnResultDraw1.onClick.AddListener(() => OnClickedDrawGacha(1));
        }

        if (btnResultDraw10 != null)
        {
            btnResultDraw10.onClick.RemoveAllListeners();
            btnResultDraw10.onClick.AddListener(() => OnClickedDrawGacha(10));
        }
    }

    private void RemoveButtonListeners()
    {
        if (btnOpenGachaWindow != null) btnOpenGachaWindow.onClick.RemoveAllListeners();
        if (btnDraw1 != null) btnDraw1.onClick.RemoveAllListeners();
        if (btnDraw10 != null) btnDraw10.onClick.RemoveAllListeners();
        if (btnResultDraw1 != null) btnResultDraw1.onClick.RemoveAllListeners();
        if (btnResultDraw10 != null) btnResultDraw10.onClick.RemoveAllListeners();
    }

    public void ToggleGachaWindow()
    {
        _isOpen = !_isOpen;
        if (_isOpen) OpenAllGachaWindows();
        else CloseAllGachaWindows();
    }

    private void OpenAllGachaWindows()
    {
        if (teammateInitialWindow != null) teammateInitialWindow.SetActive(true);
        if (gachaDirector != null) gachaDirector.OpenGachaWindowOnly();
    }

    private void CloseAllGachaWindows()
    {
        if (teammateInitialWindow != null) teammateInitialWindow.SetActive(false);
        if (gachaDirector != null) gachaDirector.CloseGachaUI();
    }

    private void OnClickedDrawGacha(int drawCount)
    {
        if (Time.time - _lastClickTime < CLICK_THRESHOLD) return;
        _lastClickTime = Time.time;

        if (gachaManager == null)
        {
            Debug.LogError("<color=red>[GachaBridge]</color> GachaManagerBase 참조가 연결되지 않았습니다!");
            return;
        }

        long requiredCost = gachaManager.GetCost(drawCount);

        if (CoinWallet != null && CoinWallet.Balance < requiredCost)
        {
            Debug.LogWarning($"<color=red>[GachaBridge] 골드 부족!</color> (필요: {requiredCost} / 보유: {CoinWallet.Balance})");
            return;
        }

        if (CoinWallet != null && !CoinWallet.TrySpend(requiredCost)) return;

        List<GachaResult> rollResults = gachaManager.RollMulti(drawCount);
        List<GachaEntryData> entryDataList = new List<GachaEntryData>();

        if (rollResults != null)
        {
            foreach (GachaResult result in rollResults)
            {
                if (result.Entry == null) continue;

                entryDataList.Add(result.Entry);

                if (result.Entry is ToolDataSO toolData)
                {
                    if (InventoryManager_Tool.Instance != null)
                    {
                        InventoryManager_Tool.Instance.AddToolSlot(toolData);
                        Debug.Log($"<color=cyan>[GachaBridge] 도구 획득 완료:</color> {toolData.DisplayName}");
                    }
                }
                else
                {
                    Debug.LogWarning($"<color=yellow>[GachaBridge] 경고:</color> 뽑힌 데이터가 ToolDataSO가 아닙니다! ({result.Entry.DisplayName})");
                }
            }
        }

        _isOpen = false;
        if (teammateInitialWindow != null) teammateInitialWindow.SetActive(false);

        if (gachaDirector != null)
        {
            // 요청한 drawCount 대신, 실제 뽑혀 나온 entryDataList.Count를 전달하여 UI 불일치 차단!
            int actualCount = entryDataList.Count > 0 ? entryDataList.Count : drawCount;
            gachaDirector.StartGachaSequence(actualCount, entryDataList);
        }
    }
}