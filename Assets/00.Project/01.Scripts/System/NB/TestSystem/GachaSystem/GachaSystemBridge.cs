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
    [SerializeField] private ToolGachaManager gachaManager;

    [Tooltip("ICoinWallet을 구현한 코인 관리자 컴포넌트")]
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
    private const float CLICK_THRESHOLD = 0.3f; // 연타 방지 간격 살짝 보정

    private void Awake()
    {
        if (gachaDirector == null)
        {
            gachaDirector = FindAnyObjectByType<GachaDirector>(FindObjectsInactive.Include);
        }
    }

    private void OnEnable() => InitButtonListeners();
    private void Start() => InitButtonListeners();
    private void OnDisable() => RemoveButtonListeners();

    private void InitButtonListeners()
    {
        // 코드로 이벤트를 묶을 때는 인스펙터 버튼 OnClick 리스트를 비워두는 것이 안전합니다.
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
        // 중복 클릭 쿨다운 방어
        if (Time.time - _lastClickTime < CLICK_THRESHOLD) return;
        _lastClickTime = Time.time;

        if (gachaManager == null)
        {
            Debug.LogError("<color=red>[GachaBridge]</color> GachaManager 참조가 연결되지 않았습니다!");
            return;
        }

        long requiredCost = gachaManager.GetCost(drawCount);

        if (CoinWallet != null && CoinWallet.Balance < requiredCost)
        {
            Debug.LogWarning($"<color=red>[GachaBridge] 골드 부족!</color> (필요: {requiredCost} / 보유: {CoinWallet.Balance})");
            return;
        }

        if (CoinWallet != null && !CoinWallet.TrySpend(requiredCost)) return;

        // 가챠 계산 실행
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
                    // GachaManager가 '순수 확률 계산'만 하고 지급을 안 하는 구조라면 아래 주석을 해제
                    // 현재 1+1 버그가 발생한다면 GachaManager가 이미 지급을 하고 있는 상태

                    /*
                    if (InventoryManager_Tool.Instance != null)
                    {
                        InventoryManager_Tool.Instance.AddToolSlot(toolData);
                    }
                    */
                    Debug.Log($"<color=cyan>[GachaBridge] 도구 결과 확인:</color> {toolData.DisplayName}");
                }
            }
        }

        _isOpen = false;
        if (teammateInitialWindow != null) teammateInitialWindow.SetActive(false);

        if (gachaDirector != null)
        {
            int actualCount = entryDataList.Count > 0 ? entryDataList.Count : drawCount;
            gachaDirector.StartGachaSequence(actualCount, entryDataList);
        }
    }
}