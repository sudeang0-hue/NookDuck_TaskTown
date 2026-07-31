using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TaskTown.Gacha;
using TaskTown.KDH;
using Tool.Data;

/// <summary>
/// [ECHO TD Architecture]
/// GachaPool_Tool.asset 데이터 에셋을 직접 연결하여 도구 가챠를 보장하는 스크립트입니다.
/// </summary>
public class GachaSystemBridge : MonoBehaviour
{
    [Header("★ Data Asset Reference ★")]
    [SerializeField] private GachaPoolData toolGachaPoolData;

    [Header("Teammate's System References")]
    // ★ [수정] GachaManagerBase 대신 팀원이 만든 'ToolGachaManager' 전용 타입으로 직접 지정합니다!
    // 이렇게 바꾸면 인스펙터에 AnimalGachaManager를 실수로 드래그해도 유니티가 들어가지 않게 막아줍니다.
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
    private void OnDisable() => RemoveButtonListeners();

    private void InitButtonListeners()
    {
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

    /// <summary>
    /// GachaPool_Tool 에셋 기반 도구 전용 뽑기 파이프라인
    /// </summary>
    private void OnClickedDrawGacha(int drawCount)
    {
        if (Time.time - _lastClickTime < CLICK_THRESHOLD) return;
        _lastClickTime = Time.time;

        if (gachaManager == null)
        {
            Debug.LogError("<color=red>[GachaBridge]</color> GachaManagerBase 참조가 연결되지 않았습니다!");
            return;
        }

        // 1. 비용 계산 및 지갑 검수
        long requiredCost = gachaManager.GetCost(drawCount);

        if (CoinWallet != null && CoinWallet.Balance < requiredCost)
        {
            Debug.LogWarning($"<color=red>[GachaBridge] 골드 부족!</color> (필요: {requiredCost} / 보유: {CoinWallet.Balance})");
            return;
        }

        if (CoinWallet != null && !CoinWallet.TrySpend(requiredCost)) return;

        // ★ [핵심] 가챠 매니저에게 GachaPool_Tool 에셋을 주입/셋팅할 수 있는 프로퍼티가 있다면 주입
        // 만약 GachaManagerBase에 Pool 변수가 공개되어 있다면 아래 주석 해제:
        // gachaManager.SetPool(toolGachaPoolData);

        // 2. 가챠 롤 실행
        List<GachaResult> rollResults = gachaManager.RollMulti(drawCount);
        List<GachaEntryData> entryDataList = new List<GachaEntryData>();

        if (rollResults != null)
        {
            foreach (GachaResult result in rollResults)
            {
                if (result.Entry == null) continue;

                // 연출 데이터 수집
                entryDataList.Add(result.Entry);

                // 3. 도구 데이터 판별 및 인벤토리 추가
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

        // UI 은폐 및 연출 재생
        _isOpen = false;
        if (teammateInitialWindow != null) teammateInitialWindow.SetActive(false);

        if (gachaDirector != null)
        {
            gachaDirector.StartGachaSequence(drawCount, entryDataList);
        }
    }
}